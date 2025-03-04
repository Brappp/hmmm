using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using OceanFishingAutomator.UI;
using System;
using Dalamud.Game;
using Dalamud.Plugin.Services;

namespace OceanFishingAutomator
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Ocean Fishing Automator";

        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; private set; } = null!;
        [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
        [PluginService] internal static IPluginLog Log { get; private set; } = null!;
        [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;

        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem { get; init; } = new("OceanFishingAutomator");

        private MainWindow mainWindow;
        private FishingManager fishingManager;

        private const string CommandName = "/oceanfish";

        public Plugin()
        {
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Save();

            // Initialize the fishing automation logic with the SigScanner.
            fishingManager = new FishingManager(Configuration, SigScanner);

            // Initialize main window
            mainWindow = new MainWindow(this, fishingManager);
            WindowSystem.AddWindow(mainWindow);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the Ocean Fishing Automator window."
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleUI;
            PluginInterface.UiBuilder.OpenMainUi += ToggleUI;
        }

        private void OnCommand(string command, string args)
        {
            ToggleUI();
        }

        public void ToggleUI()
        {
            mainWindow.IsOpen = !mainWindow.IsOpen;
        }

        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        public void Dispose()
        {
            CommandManager.RemoveHandler(CommandName);
            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi -= ToggleUI;
            PluginInterface.UiBuilder.OpenMainUi -= ToggleUI;
            WindowSystem.RemoveAllWindows();
            fishingManager.Dispose();
        }
    }
}
