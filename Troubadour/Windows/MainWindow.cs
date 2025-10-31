using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Troubadour.Windows.UI.Tabs;

namespace Troubadour.Windows;

public class MainWindow : Window, IDisposable
{
    public enum Tab
    {
        None,
        Main,
        Settings,
        Debug
    }
    public Tab ActiveTab { get; set; } = Tab.None;
    public Plugin Plugin { get; }

    private readonly Main mainTab;
    private readonly Settings settingsTab;
    private readonly Debug debugTab;

    public MainWindow(Plugin plugin, Configuration config) : base("Troubadour", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        mainTab = new Main(plugin, config);
        settingsTab = new Settings(plugin, config);
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

        if (!ImGui.Begin("Troubadour", ref isOpen, ImGuiWindowFlags.None))
        {
            ImGui.End();
            return;
        }

        bool tabOpen = true;
        if (ImGui.BeginTabBar("Tabs"))
        {
            ImGuiTabItemFlags mainTabFlags = ActiveTab == Tab.Main ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
            if (ImGui.BeginTabItem("Main", ref tabOpen, mainTabFlags))
            {
                mainTab.Draw();
                ImGui.EndTabItem();
            }

            ImGuiTabItemFlags settingsTabFlags = ActiveTab == Tab.Settings ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
            if (ImGui.BeginTabItem("Settings", ref tabOpen, settingsTabFlags))
            {
                settingsTab.Draw();
                ImGui.EndTabItem();
            }

            ImGuiTabItemFlags debugTabFlags = ActiveTab == Tab.Debug ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
            if (ImGui.BeginTabItem("Debug", ref tabOpen, debugTabFlags))
            {
                debugTab.Draw();
                ImGui.EndTabItem();
            }

            ActiveTab = Tab.None;
            ImGui.EndTabBar();
        }

        ImGui.End();
    }

    public void Dispose()
    {
    }
}
