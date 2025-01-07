namespace Troubadour.Windows.UI.Tabs;

public abstract class TabBase
{
    protected Plugin Plugin { get; }
    protected Configuration Config { get; }

    protected TabBase(Plugin plugin, Configuration config)
    {
        Plugin = plugin;
        Config = config;
    }

    public abstract void Draw();
}
