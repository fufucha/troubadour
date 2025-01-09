using Troubadour.Services;

namespace Troubadour.Windows.UI.Tabs;

public abstract class TabBase
{
    protected Plugin Plugin { get; }
    protected Configuration Config { get; }
    protected UIService ui { get; }

    protected TabBase(Plugin plugin, Configuration config)
    {
        Plugin = plugin;
        Config = config;
        this.ui = new UIService(Plugin.TextureProvider, Plugin.BgmList, Plugin.BgmStates);
    }

    public abstract void Draw();

    /// <summary>
    /// Updates the checkbox states to synchronize with the selected preset's BGM entries.
    /// </summary>
    public void SynchronizeBgmStates()
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
