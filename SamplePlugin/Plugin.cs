using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using OceanFishingAutomator.UI;
using OceanFishingAutomator.SeFunctions;
using Dalamud.Game;

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
        [PluginService] internal static IFramework Framework { get; private set; } = null!;

        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem { get; init; }

        private readonly MainWindow mainWindow;
        private readonly FishingManager fishingManager;
        private readonly RouteTracker routeTracker;
        private readonly TugReader tugReader;
        private readonly FishingSkillsManager fishingSkillsManager;

        private const string CommandName = "/oceanfish";

        /// <summary>
        /// Initializes the Ocean Fishing Automator Plugin explicitly, registering all necessary callbacks.
        /// </summary>
        public Plugin()
        {
            // Load or create default configuration
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Save();

            // Initialize the window system explicitly
            WindowSystem = new WindowSystem("OceanFishingAutomator");

            // Initialize dependencies explicitly
            routeTracker = new RouteTracker(Framework);
            tugReader = new TugReader(SigScanner);
            fishingManager = new FishingManager(Configuration, SigScanner);

            fishingSkillsManager = new FishingSkillsManager(
                Configuration,
                fishingManager,
                tugReader,
                routeTracker,
                CommandManager,
                Log
            );

            // Initialize the main plugin UI window explicitly
            mainWindow = new MainWindow(this, fishingManager);
            WindowSystem.AddWindow(mainWindow);

            // Explicitly register the command handler for the plugin
            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the Ocean Fishing Automator window."
            });

            // Explicitly hook up Dalamud framework events
            Framework.Update += OnFrameworkUpdate;
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleUI;

            // Explicitly register main UI callback to fix the UI registration warning
            PluginInterface.UiBuilder.OpenMainUi += ToggleUI;

            Log.Information("Ocean Fishing Automator plugin initialized successfully with main UI callback registered.");
        }

        /// <summary>
        /// Explicitly handles the command "/oceanfish" to toggle the main plugin UI window.
        /// </summary>
        private void OnCommand(string command, string args)
        {
            ToggleUI();
        }

        /// <summary>
        /// Explicitly toggles the main plugin UI visibility.
        /// </summary>
        public void ToggleUI()
        {
            mainWindow.IsOpen = !mainWindow.IsOpen;
        }

        /// <summary>
        /// Explicitly defines the main UI draw loop for the window system.
        /// </summary>
        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        /// <summary>
        /// Explicitly defines the framework update method, used to update fishing automation logic.
        /// </summary>
        private void OnFrameworkUpdate(IFramework framework)
        {
            fishingSkillsManager.ManageSkills();
        }

        /// <summary>
        /// Explicitly disposes of all resources and unregisters all callbacks to ensure a clean unload of the plugin.
        /// </summary>
        public void Dispose()
        {
            Framework.Update -= OnFrameworkUpdate;
            CommandManager.RemoveHandler(CommandName);
            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi -= ToggleUI;

            // Explicitly unregister the main UI callback
            PluginInterface.UiBuilder.OpenMainUi -= ToggleUI;

            fishingSkillsManager.Dispose();
            fishingManager.Dispose();
            routeTracker.Dispose();

            WindowSystem.RemoveAllWindows();

            Log.Information("Ocean Fishing Automator plugin disposed cleanly, all callbacks unregistered.");
        }
    }
}
