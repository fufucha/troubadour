
using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Troubadour.BGM;
using Troubadour.Windows;

namespace Troubadour;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;

    public string Name => "Troubadour";
    private const string CommandName = "/troubadour";
    public readonly WindowSystem WindowSystem = new("Troubadour");

    private MainWindow mainWindow;
    public Configuration Config { get; private set; }
    private List<BgmData> bgmList = new();

    public BgmManager BgmManager { get; private set; }
    private ushort currentBgmId;

    public PresetManager PresetManager { get; private set; }

    public List<BgmData> BgmList { get; private set; }
    public Dictionary<ushort, bool> BgmStates { get; private set; }

    public Plugin()
    {
        // load configuration
        Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // initialize the Preset Manager
        PresetManager = new PresetManager(Config, this);

        // initialize BGM data
        BgmList = BgmRepository.BgmData.ToList();
        BgmStates = new Dictionary<ushort, bool>();

        // initialize the main window
        mainWindow = new MainWindow(this, Config);
        WindowSystem.AddWindow(mainWindow);

        // register UI events
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi += () =>
        {
            mainWindow.IsOpen = true;
            mainWindow.ActiveTab = MainWindow.Tab.Main;
        };
        PluginInterface.UiBuilder.OpenConfigUi += () =>
        {
            mainWindow.IsOpen = true;
            mainWindow.ActiveTab = MainWindow.Tab.Settings;
        };

        // add command handler
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Customize and manage BGM presets with user-defined alternatives"
        });

        // subscribe to framework updates
        Framework.Update += OnFrameworkUpdate;

        try
        {
            // add or check the default preset
            PresetManager.Initialize();

            // initialize BGM Manager
            BgmManager = new BgmManager(SigScanner);
            BgmManager.Initialize();

            // set the initial BGM
            currentBgmId = BgmManager.GetCurrentBgmId();
        }
        catch (Exception ex)
        {
            PrintError($"An error occurred during plugin initialization: {ex.Message}.");
        }
    }

    /// <summary>
    /// Plays the specified BGM ID with the given priority.
    /// </summary>
    /// <param name="bgmId">The ID of the BGM to play.</param>
    /// <param name="priority">The priority level for the BGM.</param>

    public unsafe void PlayBackgroundMusic(ushort bgmId, ushort priority = BgmManager.HIGHEST_PRIORITY)
    {
        try
        {
            BgmManager.Play(priority, bgmId, bgmId, BgmManager.SceneFlags.Resume | BgmManager.SceneFlags.EnableDisableRestart, 0);
            Print(@$"BGM ""{bgmId}"" started for Scene ""{priority}"" successfully.");
        }
        catch (Exception ex)
        {
            PrintError($"An error occurred while attempting to play the music: {ex.Message}.");
        }
    }

    /// <summary>
    /// Stops the currently playing BGM at the specified priority.
    /// </summary>
    /// <param name="priority">The priority level to stop.</param>
    public unsafe void StopBackgroundMusic(ushort priority = BgmManager.HIGHEST_PRIORITY)
    {
        try
        {
            BgmManager.Stop(priority);
            Print(@$"BGM for Scene ""{priority}"" stopped successfully.");
        }
        catch (Exception ex)
        {
            PrintError($"An error occurred while attempting to stop the music: {ex.Message}.");
        }
    }

    /// <summary>
    /// Handles logic for BGM changes by iterating through all enabled presets.
    /// If a detected BGM ID matches an entry in any enabled preset, it replaces the BGM with a randomly selected replacement from that specific preset.
    /// If no match is found and a replacement BGM is currently playing, stops the currently playing music.
    /// </summary>
    /// <param name="detectedBgmId">The ID of the newly detected BGM.</param>
    private void OnBgmChanged(ushort detectedBgmId)
    {
        // iterate through all enabled presets
        foreach (var preset in PresetManager.GetEnabledPresets())
        {
            // check if the detected BGM ID is in the preset's checked list
            if (preset.SelectedEntries.Contains(detectedBgmId))
            {
                // select a random replacement BGM
                var random = new Random();
                var newBgm = preset.Replacements.ElementAt(random.Next(preset.Replacements.Count));

                // attempt to set the new BGM
                if (!BgmManager.SetBgmToScene(BgmManager.HIGHEST_PRIORITY, newBgm))
                {
                    PrintError(@$"Failed to set BGM ""{newBgm}"".");
                    return;
                }

                Print(@$"Replacing BGM ""{detectedBgmId}"" with ""{newBgm}"" from preset ""{preset.Name}"".");
                return;
            }
        }

        // if no preset has a replacement for the detected BGM ID, stop the current music
        var currentSceneBgmId = BgmManager.CurrentSceneBgmId(BgmManager.HIGHEST_PRIORITY);
        if (currentSceneBgmId != 0)
        {
            Print(@$"BGM ID ""{detectedBgmId}"" not found in any enabled preset. Revert to game.");
            StopBackgroundMusic();
        }
    }

    /// <summary>
    /// Automatically triggers BGM replacement logic when the current BGM changes.
    /// </summary>
    private void OnFrameworkUpdate(IFramework framework)
    {
        var newBgmId = BgmManager.GetCurrentBgmId();
        if (newBgmId != currentBgmId)
        {
            currentBgmId = newBgmId;
            OnBgmChanged(newBgmId);
        }
    }

    /// <summary>
    /// Toggles the visibility of the main window when the command is invoked.
    /// </summary>
    private void OnCommand(string command, string args)
    {
        mainWindow.IsOpen = !mainWindow.IsOpen;
    }

    /// <summary>
    /// Displays an informational message in the chat.
    /// </summary>
    /// <param name="message">The message to display.</param>
    public void Print(string message)
    {
        if (Config.Debug)
        {
            ChatGui.Print($"[Troubadour] {message}");
        }
    }

    /// <summary>
    /// Displays an error message in the chat.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    public void PrintError(string message)
    {
        ChatGui.PrintError($"[Troubadour] {message}");
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler(CommandName);
        Framework.Update -= OnFrameworkUpdate;

        BgmList.Clear();
        BgmStates.Clear();
        BgmManager?.Dispose();
    }
}
