using MioGame;
using MioGame.Shader;
using MioModdingApi;
using MioModLoader;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ColorMio
{
    

    public class ColorMio : Mod
    {
        public Vec_float_3 color = default;
        public Vec_float_3 outlineColor = default;
        public Vec_float_3 intensity = default;
        public float currentTick = 0f;
        public float maxTick = 300f;

        private readonly string[] colors = { "red", "green", "blue" };


        public override void Initialize()
        {
            Hooks();

            LoadGradients();
            ConnectMenus();
        }
        
        private static ModMenuPage? modPage;
        private static Dictionary<string, NumberOption> tints = [];
        private static Dictionary<string, NumberOption> outlines = [];
        private static NumberOption? rainbowDuration;
        private static ChoiceOption? gradientOption;
        private Dictionary<string, GradientPoint[]> gradientConfig = [];
        private List<string> gradientChoices = [
            "none",
            "rainbow",
        ];

        public void ConnectMenus()
        {
            modPage = ModMenu.RegisterMod(this, "Color Mio", "Color Mio");
            //MioGame.Shader.Mio_params
            
            try
            {
                RegisterRGB("tint", "Tint", 1, "The {0} value of the tentacle color.", tints,
                    (int value, string c) => SetColorChannel(ref color, c, value));
                RegisterRGB("int", "Intensity", 1, "The {0} intensity value of the tentacle color.", tints,
                    (int value, string c) => SetColorChannel(ref intensity, c, value));
                //RegisterRGB("outline", "Outline", 1, "The {0} value of the tentacle outline color.", tints,
                //    (int value, string c) => SetColorChannel(ref outlineColor, c, value));

                gradientOption = modPage.RegisterChoice("gradient_choice", "Gradient", gradientChoices, 0, "Choose a gradient to cycle through");
                rainbowDuration = modPage.RegisterNumber("rainbow_duration", "Rainbow duration", 1, 20, 1, 5, "How long to cycle through all the colours when rainbow gradient is selected", "{0} seconds");

                void updateMaxTick()
                {
                    if (gradientOption.Value == "rainbow")
                    {
                        maxTick = rainbowDuration.Value * 60;
                    }
                    else if (gradientOption.Value != "none")
                    {
                        maxTick = gradientConfig[gradientOption.Value][^1].t;
                    }
                }

                gradientOption.Changed += index => updateMaxTick();
                rainbowDuration.Changed += duration => updateMaxTick();
                updateMaxTick();
            }
            catch (Exception ex)
            {
                LogMessage("Could not initialize options: " + ex);
            }
        }

        private unsafe void Hooks()
        {
            On.MioGame.GlobalFunctions.game.On_renderer.render_tentacle_2.Prefix += Render_tentacle_prefix;
            On.MioGame.On_Game.fixed_update.Prefix += Fixed_update_Prefix;

            //On.MioGame.On_Mio_baked.from_params.Prefix += From_params_Prefix;
            //On.MioGame.GlobalFunctions.game.On_renderer.mio_hair_outline_color.Hook += Mio_hair_outline_color_Hook;
        }

        //private unsafe void From_params_Prefix(MioGame.Mio_params* @params)
        //{
        //    ref var mioParams = ref Unsafe.AsRef<MioGame.Mio_params>(@params);
        //    PrintVector(ref mioParams.halo_color);
        //}

        private void LoadGradients()
        {
            string path = Path.Combine(this.GetModConfigPath(), "gradients.json");
            if (!System.IO.File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                Dictionary<string, GradientPoint[]> defaultGradients = [];
                System.IO.File.WriteAllText(path, JsonSerializer.Serialize(defaultGradients, new JsonSerializerOptions { WriteIndented = true }));
            }

            try
            {
                Dictionary<string, GradientPoint[]>? loadedGradients = JsonSerializer.Deserialize<Dictionary<string, GradientPoint[]>>(System.IO.File.ReadAllText(path));
                if (loadedGradients is not null)
                {
                    gradientConfig = loadedGradients;
                    gradientChoices.AddRange(gradientConfig.Keys);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"could not read {path}, using defaults: {ex.Message}");
                try { System.IO.File.Copy(path, path + ".broken", overwrite: true); } catch { /* best effort */ }
                return;
            }
        }

        private static Vec_float_3 Lerp_Vectors(Vec_float_3 a, Vec_float_3 b, float r)
        {
            return new Vec_float_3() { Base = new _vec_storage_float_3() { x = (a.x * (1 - r)) + (b.x * r), y = (a.y * (1 - r)) + (b.y * r), z = (a.z * (1 - r)) + (b.z * r) } };
        }

        private static Vec_float_3 Gradient_Point_as_Vec(GradientPoint gradient)
        {
            return new Vec_float_3() { Base = new _vec_storage_float_3() { x = gradient.r, y = gradient.g, z = gradient.b} };
        }

        private static Vec_float_3 Lerp_Gradient(GradientPoint[] gradient, float t)
        {
            int index = -1;
            for (int ii = 0; ii < (gradient.Length - 1); ii++)
            {
                if (gradient[ii].t <= t && t < gradient[ii + 1].t )
                {
                    index = ii;
                    break;
                }
            }
            if (index == -1)
            {
                return Gradient_Point_as_Vec(gradient[0]);
            }
            GradientPoint a = gradient[index];
            GradientPoint b = gradient[index + 1];
            float r = (t - a.t) / (b.t - a.t);
            return Lerp_Vectors(Gradient_Point_as_Vec(a), Gradient_Point_as_Vec(b), r);

        }


        private unsafe void Fixed_update_Prefix(MioGame.Game* __this)
        {
            // increment the tick counter on update, not during the render_tentacle_2 hook so the colour changing isn't frame rate dependent
            currentTick = (currentTick + 1) % maxTick;
        }

        private static Vec_float_3 HueToRGB(float hue)
        {
            // magic incantation to convert hsv to rgb from wikipedia
            static float k_func(float n, float h) => (n + (h / 60f)) % 6f;
            static float f_func(float n, float h) => 1f - Math.Max(0f, Math.Min(k_func(n, h), Math.Min(4f - k_func(n, h), 1f)));

            return new Vec_float_3() { Base = new _vec_storage_float_3() { x = f_func(5f, hue), y = f_func(3f, hue), z = f_func(1f, hue) } };
        }

        private unsafe void Render_tentacle_prefix(uint idx, Mio.Tentacle* t, Vec_float_3 chain_u, Vec_float_3 chain_v, 
            Array_Vec_float_3* chain, Array_Shader__Tentacle_vertex* vertices, Array_unsigned_int* indices, 
            Tentacle_consts* consts)
        {
            ref var tint = ref consts->tint;

            string selectedGradient = gradientOption?.Value ?? "none";
            if (selectedGradient == "none")
            {
                tint.x = (color.x / 255) / 0.9411765f * intensity.x;
                tint.y = (color.y / 255) / 0.83137256f * intensity.y;
                tint.z = (color.z / 255) / 0.5019608f * intensity.z;
            }
            else if (selectedGradient == "rainbow")
            {
                // the rainbow colour is just a HSV value with S and V set to 1 and hue increasing from 0 to 360
                Vec_float_3 next_colour = HueToRGB(currentTick / maxTick * 360);
                tint.x = next_colour.x / 0.9411765f * intensity.x;
                tint.y = next_colour.y / 0.83137256f * intensity.y;
                tint.z = next_colour.z / 0.5019608f * intensity.z;
            }
            else
            {
                Vec_float_3 next_colour = Lerp_Gradient(gradientConfig[selectedGradient], currentTick);
                tint.x = next_colour.x / 255 / 0.9411765f * intensity.x;
                tint.y = next_colour.y / 255 / 0.83137256f * intensity.y;
                tint.z = next_colour.z / 255 / 0.5019608f * intensity.z;
            }
        }

        private unsafe void Mio_hair_outline_color_Hook(On.MioGame.GlobalFunctions.game.On_renderer.orig_mio_hair_outline_color orig, Vec_float_3* retBuffer)
        {
            ref var vector = ref Unsafe.AsRef<Vec_float_3>(retBuffer);
            vector.x = outlineColor.x;
            vector.y = outlineColor.y;
            vector.z = outlineColor.z;
        }

        private void PrintVector(ref Vec_float_3 vec)
        {
            LogMessage("x: " + vec.x.ToString());
            LogMessage("y: " + vec.y.ToString());
            LogMessage("z: " + vec.z.ToString());
        }

        private readonly Vec_float_3 zeroVect = new Vec_float_3() { Base = new _vec_storage_float_3() { x = 0.0f, y = 0.0f, z = 0.0f } };

        private static void SetColorChannel(ref Vec_float_3 color, string channel, int value)
        {
            switch (channel)
            {
                case "red":
                    color.x = value;
                    break;
                case "green":
                    color.y = value;
                    break;
                case "blue":
                    color.z = value;
                    break;
            }
        }
        

        private void RegisterRGB(string id, string title, int initialValue, string description, 
            Dictionary<string, NumberOption> collection, Action<int, string> changed)
        {
            if (modPage == null) return;
            foreach (string c in colors)
            {
                tints[c] = modPage.RegisterNumber(
                    c + "_" + id, title + $" ({c})",
                    0, 255, 1, initialValue,
                    System.String.Format(description, c)
                    );
                tints[c].Changed += (int value) => changed(value, c);
                changed(tints[c].Value, c);
            }
        }
    }
}

internal record GradientPoint(float r, float g, float b, float t);