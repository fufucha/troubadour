using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Troubadour.BGM;
using Troubadour.Services;
using Troubadour.Windows.UI.Tabs;

namespace Troubadour.Windows;

public class MainWindow : Window, IDisposable
{
    private static readonly uint[] Icons = { 61831, 61875, 61876, 61877, 61878, 61879, 61880 };
    private static readonly string[] Categories = {
        "Event",
        "A Realm Reborn",
        "Heavensward",
        "Stormblood",
        "Shadowbringers",
        "Endwalker",
        "Dawntrail"
    };

    private readonly Main mainTab;
    private readonly Settings settingsTab;
    private readonly Debug debugTab;

    private List<BgmData> bgmList;
    private Dictionary<ushort, bool> bgmStates;
    private Dictionary<string, string> searches;

    public MainWindow(Plugin plugin, Configuration config) : base("Troubadour", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        bgmList = BgmRepository.BgmData.ToList();
        bgmStates = new Dictionary<ushort, bool>();
        searches = new Dictionary<string, string>();
        var ui = new UIService(Plugin.TextureProvider, bgmList, bgmStates);

        mainTab = new Main(plugin, config, ui, Icons, Categories);
        settingsTab = new Settings(plugin, config, searches, ui);
        debugTab = new Debug(plugin, config);
    }

    /// <summary>
    /// Renders the content of the main window.
    /// </summary>
    public override void Draw()
    {
        var isOpen = IsOpen;
        ImGui.SetNextWindowSize(new Vector2(800, 600), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(new Vector2(800, 600), new Vector2(float.MaxValue, float.MaxValue));

        ImGui.Begin("Troubadour", ref isOpen, ImGuiWindowFlags.None);

        if (ImGui.BeginTabBar("Tabs"))
        {
            mainTab.Draw();
            settingsTab.Draw();
            debugTab.Draw();
            ImGui.EndTabBar();
        }

        ImGui.End();
    }

    public void Dispose()
    {
        bgmList.Clear();
        bgmStates.Clear();
        searches.Clear();
    }
}
