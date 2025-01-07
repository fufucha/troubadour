using System.Linq;
using System.Numerics;
using Dalamud.Interface.Colors;
using ImGuiNET;
using Troubadour.BGM;
using Troubadour.Services;

namespace Troubadour.Windows.UI.Tabs;

public class Main : TabBase
{
    private readonly UIService ui;
    private readonly uint[] icons;
    private readonly string[] categories;
    private int selectedPanel;
    private string searchQuery;
    private bool showCheckedOnly;

    public Main(Plugin plugin, Configuration config, UIService ui, uint[] icons, string[] categories) : base(plugin, config)
    {
        this.ui = ui;
        this.icons = icons;
        this.categories = categories;
        searchQuery = string.Empty;

        // synchronize checkboxes
        SynchronizeBgmStates();
    }

    /// <summary>
    /// Renders the content of the "Main" tab.
    /// </summary>
    public override void Draw()
    {
        if (ImGui.BeginTabItem("Main"))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 4));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4, 2));
            ImGui.Columns(2, "MainColumns", false);

            DrawIconsPanel();
            DrawContentPanel();

            ImGui.Columns(1);
            ImGui.PopStyleVar(2);
            ImGui.EndTabItem();
        }
    }

    /// <summary>
    /// Renders the left-side panel containing category icons, allowing users to navigate between different BGM categories.
    /// </summary>
    private void DrawIconsPanel()
    {
        ImGui.SetColumnWidth(0, 56);
        ImGui.BeginChild("IconsPanel", new Vector2(0, 0), false);
        DisplayIcons();
        ImGui.EndChild();
    }

    /// <summary>
    /// Renders the right-side panel containing the preset dropdown, search bar, and filtered BGM list for the selected category.
    /// </summary>
    private void DrawContentPanel()
    {
        ImGui.NextColumn();

        DrawPresetDropdown();
        DrawSearchBar();
        DrawPanelContent();
    }

    /// <summary>
    /// Renders the category icons as buttons, allowing users to switch panels.
    /// </summary>
    private void DisplayIcons()
    {
        for (var i = 0; i < icons.Length; i++)
        {
            var iconID = icons[i];
            var icon = ui.GetIcon(iconID);
            if (icon == null)
            {
                ImGui.Text($"Icon {iconID} not found.");
                ImGui.Spacing();
                continue;
            }

            using var textureWrap = icon.GetWrapOrEmpty();
            if (textureWrap == null || textureWrap.ImGuiHandle == nint.Zero)
            {
                ImGui.Text($"Icon {iconID} not found.");
                ImGui.Spacing();
                continue;
            }

            ImGui.PushStyleColor(ImGuiCol.Button, selectedPanel == i ? ImGuiColors.DalamudGrey : new Vector4(0, 0, 0, 0));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered]);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);

            if (ImGui.ImageButton(textureWrap.ImGuiHandle, new Vector2(40, 40), Vector2.Zero, Vector2.One))
            {
                selectedPanel = i;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(categories[i]);
                ImGui.EndTooltip();
            }

            ImGui.PopStyleColor(3);
            ImGui.Spacing();
        }
    }

    /// <summary>
    /// Renders the dropdown menu for selecting a preset.
    /// </summary>
    private void DrawPresetDropdown()
    {
        if (ImGui.BeginCombo("##PresetDropdown", Plugin.PresetManager.SelectedPreset?.Name ?? "Select a Preset"))
        {
            foreach (var preset in Plugin.Config.Presets)
            {
                var name = preset.Name + (preset.IsEnabled ? "" : " (disabled)");
                var isSelected = Plugin.PresetManager.SelectedPreset?.Name == preset.Name;
                if (ImGui.Selectable(name, isSelected))
                {
                    Plugin.PresetManager.SetSelectedPreset(preset);
                    SynchronizeBgmStates();
                }

                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }
            ImGui.EndCombo();
        }
    }

    /// <summary>
    /// Renders the search bar with a text input and checkbox for filtering the BGM list.
    /// </summary>
    private void DrawSearchBar()
    {
        ImGui.InputTextWithHint("##SearchBar", "Search BGM...", ref searchQuery, 100);
        ImGui.SameLine();
        ImGui.Checkbox("Show Checked Only", ref showCheckedOnly);
        searchQuery = searchQuery.ToLowerInvariant();
    }

    /// <summary>
    /// Renders the list of filtered BGM entries for the selected category.
    /// </summary>
    private void DrawPanelContent()
    {
        if (selectedPanel < categories.Length)
        {
            var category = categories[selectedPanel];
            var filteredBgm = ui.GetFilteredBgm(searchQuery, categories[selectedPanel], showCheckedOnly);

            ImGui.BeginChild($"Panel_{selectedPanel}_Content", new Vector2(0, 0), true);

            if (!filteredBgm.Any())
            {
                ImGui.Text("No results found for this category.");
                ImGui.EndChild();
                return;
            }

            foreach (var bgm in filteredBgm)
            {
                DrawBgmComponent(bgm);
            }

            ImGui.EndChild();
        }
    }

    /// <summary>
    /// Renders an individual BGM component, including a checkbox, play button, and stop button.
    /// </summary>
    /// <param name="entry">The BGM entry to display.</param>
    private void DrawBgmComponent(BgmData entry)
    {
        var title = $"{entry.Id} - {entry.English}";
        var isChecked = ui.GetBgmState(entry.Id);

        if (ImGui.Checkbox($"{title}##{entry.Id}", ref isChecked))
        {
            ui.SetBgmState(entry.Id, isChecked);

            if (Plugin.PresetManager.SelectedPreset != null)
            {
                if (isChecked)
                {
                    Plugin.PresetManager.SelectedPreset.SelectedEntries.Add(entry.Id);
                }
                else
                {
                    Plugin.PresetManager.SelectedPreset.SelectedEntries.Remove(entry.Id);
                }
            }

            Plugin.Config.Save();
        }

        ImGui.SameLine();
        if (ImGui.Button($"▶##Play_{entry.Id}"))
        {
            Plugin.PlayBackgroundMusic(entry.Id);
        }

        ImGui.SameLine();
        if (ImGui.Button($"■##Stop_{entry.Id}"))
        {
            Plugin.StopBackgroundMusic();
        }
    }

    /// <summary>
    /// Updates the checkbox states to synchronize with the selected preset's BGM entries.
    /// </summary>
    private void SynchronizeBgmStates()
    {
        if (Plugin.PresetManager.SelectedPreset?.SelectedEntries != null)
        {
            foreach (var bgm in ui.GetFilteredBgm(string.Empty, string.Empty, false))
            {
                var isSelected = Plugin.PresetManager.SelectedPreset.SelectedEntries.Contains(bgm.Id);
                ui.SetBgmState(bgm.Id, isSelected);
            }
        }
    }
}
