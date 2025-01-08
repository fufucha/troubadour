using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Colors;
using ImGuiNET;
using Troubadour.BGM;
using Troubadour.Services;

namespace Troubadour.Windows.UI.Tabs;

public class Settings : TabBase
{
    private readonly UIService ui;
    private Dictionary<string, string> searches;

    public Settings(Plugin plugin, Configuration config, Dictionary<string, string> searches, UIService ui) : base(plugin, config)
    {
        this.ui = ui;
        this.searches = searches;
    }

    /// <summary>
    /// Renders the content of the "Settings" tab.
    /// </summary>
    public override void Draw()
    {
        DrawSectionHeader("Preset Management");
        ImGui.BeginChild("SettingsScrollArea", new Vector2(0, 0), true);

        DrawGlobalPresetButtons();
        ImGui.Separator();

        foreach (var preset in Plugin.Config.Presets.ToList())
        {
            DrawPreset(preset);
        }

        ImGui.EndChild();
    }

    /// <summary>
    /// Draws the buttons for global preset management.
    /// </summary>
    private void DrawGlobalPresetButtons()
    {
        if (ImGui.Button("Add New Preset"))
        {
            var newPresetName = Plugin.PresetManager.GetUniquePresetName("New Preset");
            Plugin.PresetManager.AddPreset(newPresetName);
        }

        ImGui.SameLine();
        if (ImGui.Button("Import Preset"))
        {
            try
            {
                var clipboardText = ImGui.GetClipboardText();
                if (!Plugin.PresetManager.ImportPreset(clipboardText))
                {
                    Plugin.PrintError("Failed to import preset.");
                    return;
                }

                Plugin.Print("Preset imported successfully.");
            }
            catch (Exception ex)
            {
                Plugin.PrintError($"Failed to import preset: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Renders the UI components for a specific preset.
    /// </summary>
    /// <param name="preset">The preset to be rendered.</param>
    private void DrawPreset(BgmPreset preset)
    {
        if (!searches.ContainsKey(preset.Name))
        {
            searches[preset.Name] = string.Empty;
        }

        if (ImGui.CollapsingHeader($"{preset.Name}###PresetHeader_{preset.Name}", ImGuiTreeNodeFlags.CollapsingHeader))
        {
            var newPresetName = preset.Name;

            ImGui.Text("Name");
            if (ImGui.InputText($"##RenamePreset_{preset.Name}", ref newPresetName, 32, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if (string.IsNullOrWhiteSpace(newPresetName))
                {
                    Plugin.PrintError("Preset name cannot be empty.");
                    return;
                }

                if (Plugin.PresetManager.PresetExists(newPresetName))
                {
                    Plugin.PrintError("This preset name already exists.");
                    return;
                }

                // update the search keys to reflect the new name
                searches[newPresetName] = searches[preset.Name];
                searches.Remove(preset.Name);

                Plugin.PresetManager.RenamePreset(preset, newPresetName.Substring(0, Math.Min(newPresetName.Length, 32)));
            }

            ImGui.SameLine();
            bool isEnabled = preset.IsEnabled;
            if (ImGui.Checkbox($"Enabled##{preset.Name}", ref isEnabled))
            {
                preset.IsEnabled = isEnabled;
                Plugin.Config.Save();
            }

            ImGui.Separator();
            ImGui.Text("Description");
            var description = preset.Description;
            if (ImGui.InputTextMultiline($"##Description_{preset.Name}", ref description, 255, new Vector2(-1, 50)))
            {
                preset.Description = description.Substring(0, Math.Min(description.Length, 255));
                Plugin.Config.Save();
            }

            ImGui.Separator();
            DrawPresetReplacementsSection(preset);
            ImGui.Separator();

            if (preset.IsProtected)
            {
                if (ImGui.Button($"Restore {preset.Name} to Default"))
                {
                    preset.ResetToDefault();
                    Plugin.Config.Save();
                }
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.DPSRed);
                if (ImGui.Button($"Delete Preset##{preset.Name}"))
                {
                    if (ImGui.GetIO().KeyCtrl)
                    {
                        Plugin.PresetManager.DeletePreset(preset);
                        searches.Remove(preset.Name);
                        ImGui.PopStyleColor();
                        return;
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Hold CTRL and click to delete this preset.");
                }
                ImGui.PopStyleColor();
            }

            ImGui.SameLine();
            if (ImGui.Button($"Copy Preset to Clipboard##{preset.Name}"))
            {
                var json = Plugin.PresetManager.ExportPreset(preset);
                ImGui.SetClipboardText(json);
            }
        }
    }

    /// <summary>
    /// Draws the section for managing replacements within a preset.
    /// </summary>
    /// <param name="preset">The preset for which the replacements are managed.</param>
    private void DrawPresetReplacementsSection(BgmPreset preset)
    {
        ImGui.Text("Add Replacements");
        var replacementSearch = searches[preset.Name];
        if (ImGui.InputTextWithHint($"##ReplacementSearch_{preset.Name}", "Search BGM...", ref replacementSearch, 100))
        {
            searches[preset.Name] = replacementSearch.ToLowerInvariant();
        }

        // retrieve filtered BGMs
        var filteredBgm = ui.GetFilteredBgm(replacementSearch, string.Empty, false);
        if (!filteredBgm.Any())
        {
            ImGui.Text("No results found.");
        }

        // display the list of filtered BGMs
        ImGui.BeginChild($"ReplacementList_{preset.Name}", new Vector2(0, ImGui.GetContentRegionAvail().Y / 2), true);

        foreach (var bgm in filteredBgm)
        {
            var bgmTitle = $"{bgm.Id} - {bgm.English}";

            ImGui.Text(bgmTitle);

            ImGui.SameLine();
            if (ImGui.Button($"▶##Play_{bgm.Id}_{preset.Name}"))
            {
                Plugin.PlayBackgroundMusic(bgm.Id);
            }

            ImGui.SameLine();
            if (ImGui.Button($"■##Stop_{bgm.Id}_{preset.Name}"))
            {
                Plugin.StopBackgroundMusic();
            }

            ImGui.SameLine();
            if (ImGui.Button($"Add##{bgm.Id}_{preset.Name}"))
            {
                if (!preset.Replacements.Contains(bgm.Id))
                {
                    preset.Replacements.Add(bgm.Id);
                    Plugin.Config.Save();
                }
            }
        }
        ImGui.EndChild();

        ImGui.Separator();
        ImGui.Text("Current Replacements");
        foreach (var replacement in preset.Replacements.ToList())
        {
            var replacementBgm = ui.GetFilteredBgm(string.Empty, string.Empty, false)
                .FirstOrDefault(bgm => bgm.Id == replacement);

            var replacementTitle = replacementBgm?.English != null
                ? $"{replacement} - {replacementBgm.English}"
                : $"{replacement} - Unknown BGM";

            ImGui.Text($" • {replacementTitle}");
            ImGui.SameLine();
            if (ImGui.Button($"Remove##{replacement}_{preset.Name}"))
            {
                preset.Replacements.Remove(replacement);
                Plugin.Config.Save();
            }
        }
    }

    /// <summary>
    /// Draws a styled section header with the given title.
    /// </summary>
    /// <param name="title">The title of the section header.</param>
    private void DrawSectionHeader(string title)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DPSRed);
        ImGui.SetWindowFontScale(1.2f);
        ImGui.Text(title);
        ImGui.SetWindowFontScale(1.0f);
        ImGui.PopStyleColor();
        ImGui.Separator();
    }
}
