using ImGuiNET;
using Troubadour.BGM;

namespace Troubadour.Windows.UI.Tabs;

public class Debug : TabBase
{
    public Debug(Plugin plugin, Configuration config) : base(plugin, config) { }

    /// <summary>
    /// Renders the content of the "Debug" tab.
    /// </summary>
    public override void Draw()
    {
        if (ImGui.BeginTabItem("Debug"))
        {
            bool debugChatLogs = Config.Debug;
            if (ImGui.Checkbox("Display chat logs", ref debugChatLogs))
            {
                Config.Debug = debugChatLogs;
                Config.Save();
            }

            ImGui.Separator();
            if (ImGui.Button("Play Debug BGM"))
            {
                Plugin.PlayBackgroundMusic(671);
            }

            ImGui.SameLine();
            if (ImGui.Button("Stop Debug BGM"))
            {
                Plugin.StopBackgroundMusic();
            }

            ImGui.Separator();
            if (ImGui.Button("Log All Scenes"))
            {
                PrintScenes();
            }

            ImGui.EndTabItem();
        }
    }

    /// <summary>
    /// Prints the details of all BGM scenes to the chat.
    /// </summary>
    private unsafe void PrintScenes()
    {
        var scenes = Plugin.BgmManager.GetScenes();
        if (scenes == null)
        {
            Plugin.PrintError("Failed to retrieve BGM scenes.");
            return;
        }

        for (ushort i = BgmManager.HIGHEST_PRIORITY; i <= BgmManager.LOWEST_PRIORITY; i++)
        {
            var scene = scenes[i];
            Plugin.Print($"Scene {i}: BgmId={scene.BgmId}, BgmReference={scene.BgmReference}, Flags={scene.Flags}, Timer={scene.Timer:F2}");
        }
    }
}
