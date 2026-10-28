using MioGame;
using MioGame.Shader;
using MioModdingApi;
using MioModLoader;
using System.Runtime.CompilerServices;

namespace ColorMio
{
    public class ColorMio : Mod
    {
        public Vec_float_3 color = default;
        public Vec_float_3 outlineColor = default;
        public Vec_float_3 intensity = default;

        private readonly string[] colors = { "red", "green", "blue" };


        public override void Initialize()
        {
            Hooks();

            ConnectMenus();
        }
        
        private static ModMenuPage? modPage;
        private static Dictionary<string, NumberOption> tints = [];
        private static Dictionary<string, NumberOption> outlines = [];

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
            }
            catch (Exception ex)
            {
                LogMessage("Could not initialize options: " + ex);
            }
        }

        private unsafe void Hooks()
        {
            On.MioGame.GlobalFunctions.game.On_renderer.render_tentacle_2.Prefix += Render_tentacle_prefix;
            //On.MioGame.On_Mio_baked.from_params.Prefix += From_params_Prefix;
            //On.MioGame.GlobalFunctions.game.On_renderer.mio_hair_outline_color.Hook += Mio_hair_outline_color_Hook;
        }

        //private unsafe void From_params_Prefix(MioGame.Mio_params* @params)
        //{
        //    ref var mioParams = ref Unsafe.AsRef<MioGame.Mio_params>(@params);
        //    PrintVector(ref mioParams.halo_color);
        //}

        private unsafe void Render_tentacle_prefix(uint idx, Mio.Tentacle* t, Vec_float_3 chain_u, Vec_float_3 chain_v, 
            Array_Vec_float_3* chain, Array_Shader__Tentacle_vertex* vertices, Array_unsigned_int* indices, 
            Tentacle_consts* consts)
        {
            ref var tint = ref consts->tint;
            tint.x = (color.x / 255) / 0.9411765f * intensity.x;
            tint.y = (color.y / 255) / 0.83137256f * intensity.y;
            tint.z = (color.z / 255) / 0.5019608f * intensity.z;
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

        private void SetColorChannel(ref Vec_float_3 color, string channel, int value)
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
