# Colored Mio
A mod for MIO: Memories in Orbit, changing the color of mio's tentacles.

## Setup
### Prerequisites: 
- Dotnet 8
- [the mod loader](https://github.com/MIO-Modding/mod-loader)
- [latest release](https://github.com/TheOmnipresence/color-mio/releases/latest/download/ColorMio.zip)

### Installing:
- Install the mod loader, then run the game and close it.
- Unzip the release folder into the mods folder (should go mods/ColorMio/ColorMio.dll)

## Using
- Start the game and open the settings.
- In the mods section, choose "Color Mio".
- Change around the rgb values to your desired values.
- Close the menu.

### Gradients

You can have Mio's tentacles change colour over time by changing the "Gradient" option to the gradient of your choice.
"rainbow" is available by default, and you can change how fast it changes colours with the "Rainbow duration" setting.

You can add custom gradients by adding them to `./modconfig/io.github.mio-modding.color-mio/gradients.json`, which gets created when you load the game with this mod installed.
A custom gradient is defined with an array of objects with `r`, `g` and `b` properties to define a colour, and a `t` property to determine what time (measured in in-game ticks, 1/60th of a second) the colour will be reached.

As an example:
```json
{
    "blue-green": [
        {"r": 0, "g": 0, "b": 255, "t": 0},
        {"r": 0, "g": 255, "b": 0, "t": 120},
        {"r": 0, "g": 0, "b": 255, "t": 240}
    ]
}

```
This gradient starts blue, slowly becomes green over 2 seconds, then turns back to blue over then next 2 seconds.

Gradients override the 'tint' setting, but still get mutltiplied by the 'intensity' setting.