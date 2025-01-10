using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Troubadour.BGM;

public class PresetManager
{
    public BgmPreset SelectedPreset { get; private set; }

    private readonly Configuration config;
    private readonly Plugin plugin;

    // JSON serializer settings for Import and Export
    private JsonSerializerSettings JsonSerializerSettings => new JsonSerializerSettings
    {
        MissingMemberHandling = MissingMemberHandling.Error,
        NullValueHandling = NullValueHandling.Include,
        DefaultValueHandling = DefaultValueHandling.Populate
    };

    public PresetManager(Configuration config, Plugin plugin)
    {
        this.config = config;
        this.plugin = plugin;

        Initialize();
    }

    /// <summary>
    /// Ensures there is always a default preset available and restores or sets the selected preset.
    /// </summary>
    public void Initialize()
    {
        SelectedPreset = GetPreset(config.SelectedPresetName) ?? GetDefaultPreset();
        SetSelectedPreset(SelectedPreset);
    }

    /// <summary>
    /// Retrieves the default protected preset or creates it if it doesn't exist.
    /// </summary>
    /// <returns>The default protected preset.</returns>
    public BgmPreset GetDefaultPreset()
    {
        var protectedPreset = config.Presets.FirstOrDefault(p => p.IsProtected);
        if (protectedPreset == null)
        {
            protectedPreset = CreateDefaultPreset();
            config.Presets.Add(protectedPreset);
            SavePresets();
        }
        return protectedPreset;
    }

    /// <summary>
    /// Retrieves a preset by its name.
    /// </summary>
    /// <param name="name">The name of the preset.</param>
    /// <returns>The matching preset, or null if not found.</returns>
    public BgmPreset? GetPreset(string name)
    {
        return config.Presets.FirstOrDefault(p => p.Name == name);
    }

    /// <summary>
    /// Retrieves all presets that are currently enabled.
    /// </summary>
    /// <returns>A collection of enabled presets.</returns>
    public IEnumerable<BgmPreset> GetEnabledPresets()
    {
        return config.Presets.Where(p => p.IsEnabled);
    }

    /// <summary>
    /// Creates a new default preset.
    /// </summary>
    /// <returns>The newly created default preset.</returns>
    public BgmPreset CreateDefaultPreset()
    {
        var preset = new BgmPreset { IsProtected = true };
        preset.ResetToDefault();
        return preset;
    }

    /// <summary>
    /// Generates a unique name for a new preset to avoid name conflicts.
    /// </summary>
    /// <param name="name">The base name for the preset.</param>
    /// <returns>A unique preset name.</returns>
    public string GetUniquePresetName(string name)
    {
        var counter = 1;
        while (config.Presets.Any(p => p.Name == name))
        {
            name = $"{name} ({counter++})";
        }

        return name;
    }

    /// <summary>
    /// Sets the currently selected preset and updates the configuration.
    /// </summary>
    /// <param name="preset">The preset to set as the selected one.</param>
    public void SetSelectedPreset(BgmPreset preset)
    {
        SelectedPreset = preset;
        config.SelectedPresetName = preset.Name;
        SavePresets();
    }

    /// <summary>
    /// Adds a new preset with the specified name.
    /// </summary>
    /// <param name="name">The name of the new preset.</param>
    public void AddPreset(string name)
    {
        var newPreset = new BgmPreset { Name = name };
        config.Presets.Add(newPreset);
        SavePresets();
    }

    /// <summary>
    /// Renames an existing preset.
    /// </summary>
    /// <param name="preset">The preset to rename.</param>
    /// <param name="name">The new name for the preset.</param>
    public void RenamePreset(BgmPreset preset, string name)
    {
        preset.Name = name;
        SavePresets();
    }

    /// <summary>
    /// Deletes the specified preset. If the deleted preset is the selected preset, it updates the selection.
    /// </summary>
    /// <param name="preset">The preset to delete.</param>
    public void DeletePreset(BgmPreset preset)
    {
        config.Presets.Remove(preset);
        if (SelectedPreset == preset)
        {
            SetSelectedPreset(config.Presets.FirstOrDefault() ?? GetDefaultPreset());
        }
        SavePresets();
    }

    /// <summary>
    /// Imports a preset from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string containing the preset data.</param>
    /// <returns>True if the preset was successfully imported; otherwise, false.</returns>
    public bool ImportPreset(string json)
    {
        try
        {
            var preset = JsonConvert.DeserializeObject<BgmPreset>(json, JsonSerializerSettings);
            if (preset == null)
            {
                return false;
            }

            preset.Name = GetUniquePresetName(preset.Name.Substring(0, Math.Min(preset.Name.Length, 32)));
            preset.Description = preset.Description.Substring(0, Math.Min(preset.Description.Length, 255));
            preset.IsProtected = false;
            config.Presets.Add(preset);
            config.Save();
        }
        catch (JsonSerializationException)
        {
            throw new Exception("Failed to import preset. Invalid JSON format.");
        }
        catch (Exception ex)
        {
            throw new Exception($"An unexpected error occurred during import: {ex.Message}");
        }

        return true;
    }

    /// <summary>
    /// Exports a preset to a JSON string.
    /// </summary>
    /// <param name="preset">The preset to export.</param>
    /// <returns>A JSON string representing the preset.</returns>
    public string ExportPreset(BgmPreset preset)
    {
        return JsonConvert.SerializeObject(preset, Formatting.None, JsonSerializerSettings);
    }

    /// <summary>
    /// Checks if a preset exists in the configuration.
    /// </summary>
    /// <param name="preset">The preset to check.</param>
    /// <returns>True if the preset exists; otherwise, false.</returns>
    public bool PresetExists(BgmPreset preset)
    {
        return PresetExists(preset.Name);
    }

    /// <summary>
    /// Checks if a preset with the specified name exists in the configuration.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>True if a preset with the name exists; otherwise, false.</returns>
    public bool PresetExists(string name)
    {
        return config.Presets.Any(p => p.Name == name);
    }

    /// <summary>
    /// Saves all presets to the configuration file.
    /// </summary>
    public void SavePresets()
    {
        config.Save();
    }
}
