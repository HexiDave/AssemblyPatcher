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

Extract to your Subnautica folder - overwrite if it asks (it's only adding the **Mods** folder to **Subnautica\Subnautica_Data\Managed**). Then run **AssemblyPatcher.exe** to apply changes - you will need to do this if the game updates or you add new mods. In the future, there will be a UI to handle installation of mods.

### Building

Start by cloning the repository

```
git clone --recurse-submodules git://github.com/HexiDave/AssemblyPatcher.git
```

Open the folder you cloned into and run the **BuildHelper.exe**. Clicking **Initialize** will search out and set your Subnautica folder for the build system to reference. If you're wary of running it, you can copy the **SubnauticaPath.targets.template** to **SubnauticaPath.targets** and change it as follows:

```
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <SubnauticaPath>**SET THIS: [Steam library]\steamapps\common\Subnautica**</SubnauticaPath>
  </PropertyGroup>
</Project>
```

Open the **AssemblyPatcher.sln** file and build it all. 

Once built, the solution will copy everything needed to your Subnautica folder. You can either go there and run the **AssemblyPatcher.exe**, or you can run it right from Visual Studio.

If you encounter permission issues, make sure you don't have the game, dnSpy or something with the Assembly-CSharp.dll file loaded. It's also possible that Windows will complain about modifying files in Program Files - you can run manually copy the project files there manually or run Visual Studio in Admin mode. 

## Modding

To create your own Visual Studio Solution for creating mods, run **BuildHelper** and select _File -> New Mod_. Fill in the requested details and click **Create**. Your _Username_ is just a way to group your mods into one folder/solution file. Once it's created, you will find the solution in:

```
<AssemblyPatcherFolder>\Mods\<Username>\
```

Building the mods should automatically put them into the Subnautica mods directory, and you can then run the **AssemblyPatcher** in Subnautica's folder to include them to the game.

## My Mods

If you'd like to see some already existing mods, I've started a repository which you can use to experiment with. 

[My mod repository](https://github.com/HexiDave/SubnauticaMods.git)

To add them to the build system, do the following in the **AssemblyPatcher\Mods** folder

```
git clone https://github.com/HexiDave/SubnauticaMods.git HexiDave
```

This will put them into a named subdirectory to separate from your own. 

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

