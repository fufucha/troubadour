[![Download Count](https://img.shields.io/github/downloads/fufucha/troubadour/total.svg)](https://github.com/fufucha/troubadour/releases) [![Build Status](https://img.shields.io/github/actions/workflow/status/fufucha/troubadour/.github/workflows/build.yml)](https://github.com/fufucha/troubadour/actions) [![Latest Release](https://img.shields.io/github/v/release/fufucha/troubadour)](https://github.com/fufucha/troubadour/releases/latest)

# Troubadour - BGM Preset Manager

Troubadour allows you to customize and manage background music (BGM) presets by replacing specific in-game tracks with alternative BGM entries.

## Default Preset

Troubadour includes a default preset that you can import as a reference.

```json
{"Name":"Custom Preset","Description":"Swaps out those overplayed battle tracks for something slightly less overplayed.","SelectedEntries":[13,33,37,38,52,115,145,150,151,152,154,161,162,173,180,181,218,247,249,269,309,321,331,351,404,406,470,751],"Replacements":[231,366,523,533,543,559,582,725,738,784,817,963,938,975,977,20073,20092,20093,20099],"IsProtected":false,"IsEnabled":true}
```

## Planned Features

The following features are planned for future updates:
- **Preset Cycling**: Loop through all enabled presets automatically.
- **User-Imported Music**: Allow the use and sharing of custom music tracks selected by the user.
- **SCD Format Player**: Add support for the SCD format.
- **Orchestrion Playlist Integration**: Automatically import playlists from the the [Orchestrion plugin](https://github.com/perchbirdd/OrchestrionPlugin) when available.

## Credits

Special thanks to the **[Orchestrion Plugin](https://github.com/perchbirdd/OrchestrionPlugin)** for providing reference structures used in the implementation of `BgmManager`.
