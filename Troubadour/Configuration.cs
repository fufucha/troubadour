using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Troubadour.BGM;

namespace Troubadour;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public bool Debug { get; set; } = false;
    public string SelectedPresetName { get; set; } = string.Empty;
    public List<BgmPreset> Presets { get; set; } = new List<BgmPreset>();

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
