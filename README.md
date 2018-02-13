# Subnautica Mod System

I built this system to prevent having to manually re-patch the Assembly-CSharp.dll file that Subnautica uses for its core code. As patching gets more complex to maintain, each release would create a mountain of effort for even simple mods.

With this system, you can create your own mods that you can patch into Subnautica's code, and you will be able to incorporate replacement code at runtime when it is loaded when the game is launched. 

## Getting Started

You'll need to have some understanding of C#, IL, and Reflection in order to create new mods. Subnautica currently doesn't officially support modding, so we patch the game's files in order to launch our mods and override the game's own code.

### Prerequisites

In order to go from nothing to patching, you'll need:

* Git
* Visual Studio 2017 (Community Edition is fine)
* dnSpy or another C# decompiler
* A decent amount of patience

### Installing

Start by cloning the repository

```
git clone --recurse-submodules git://github.com/HexiDave/AssemblyPatcher.git
```

Open the folder you cloned into and run the **BuildHelper.exe**. This will search out and set your Subnautica folder for the build system to reference. If you're wary of running it, you can copy the **ManagedPath.targets.template** to **ManagedPath.targets** and change it as follows:

```
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <SubnauticaPath>**SET THIS: [Steam library]\steamapps\common\Subnautica\Subnautica_Data\Managed**</SubnauticaPath>
  </PropertyGroup>
</Project>
```

Open the **AssemblyPatcher.sln** file and build it all. If you wish to ignore the **ExampleMod** patches, disable the project from building. 

Once built, the solution will copy everything needed to your Subnautica folder. You can either go there and run the **AssemblyPatcher.exe**, or you can run it right from Visual Studio.

If you encounter permission issues, make sure you don't have the game, dnSpy or something with the Assembly-CSharp.dll file loaded. It's also possible that Windows will complain about modifying files in Program Files - you can run manually copy the project files there manually or run Visual Studio in Admin mode. 

## Built With

* [Visual Studio 2017 Community Edition](https://www.visualstudio.com/downloads/) - IDE and compiler
* [dnSpy](https://github.com/0xd4d/dnSpy) - Decompiler and investigation tool
* [dnlib](https://github.com/0xd4d/dnlib) - Library used by dnSpy to do the heavy lifting

## Contributing

TODO, but I will accept PRs. 

## Versioning

TODO

## Authors

* **[HexiDave](https://github.com/HexiDave)** - *Initial work*

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

## Acknowledgments

* **[Subnautica Nitrox](https://github.com/SubnauticaNitrox/Nitrox)** - For inspiration and build system ideas
* **[0xd4d](https://github.com/0xd4d)** - For powering the core of this project
