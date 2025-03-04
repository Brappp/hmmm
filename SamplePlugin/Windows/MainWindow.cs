using Dalamud.Interface.Windowing;
using ImGuiNET;
using OceanFishingAutomator;
using OceanFishingAutomator.Definitions;
using System.Numerics;
using System.Collections.Generic;
using System;
using System.IO;

namespace OceanFishingAutomator.UI
{
    public class MainWindow : Window, System.IDisposable
    {
        private readonly Plugin plugin;
        private readonly FishingManager fishingManager;
        private readonly Configuration config;
        private bool showConfigSection = false;

        // Logging system
        private const int MAX_LOG_ENTRIES = 100;
        private List<LogEntry> logEntries = new List<LogEntry>();
        private bool autoScroll = true;

        // Struct to store log entries with timestamp
        private struct LogEntry
        {
            public DateTime Timestamp;
            public string Message;
            public LogLevel Level;

            public LogEntry(string message, LogLevel level = LogLevel.Info)
            {
                Timestamp = DateTime.Now;
                Message = message;
                Level = level;
            }

            public string GetFormattedTime()
            {
                return Timestamp.ToString("HH:mm:ss");
            }
        }

        // Make LogLevel public so it can be accessed from AddLogEntry
        public enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error
        }

        public MainWindow(Plugin plugin, FishingManager fishingManager)
            : base("Ocean Fishing Automator")
        {
            this.plugin = plugin;
            this.fishingManager = fishingManager;
            this.config = plugin.Configuration;
            Size = new Vector2(500, 500); // Increased window size to accommodate logs
            SizeCondition = ImGuiCond.Always;

            // Subscribe to fishing events
            this.fishingManager.OnStatusChanged += OnFishingStatusChanged;
            this.fishingManager.OnFishCaught += OnFishCaught;
            this.fishingManager.OnBaitChanged += OnBaitChanged;
            this.fishingManager.OnTugDetected += OnTugDetected;

            // Add initial log entry
            AddLogEntry("Ocean Fishing Automator started");
        }

        public override void Draw()
        {
            // Status section
            if (ImGui.BeginTabBar("##Tabs"))
            {
                if (ImGui.BeginTabItem("Status"))
                {
                    DrawStatusTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Logs"))
                {
                    DrawLogsTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Settings"))
                {
                    DrawSettingsTab();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        private void DrawStatusTab()
        {
            ImGui.Text($"Status: {fishingManager.CurrentActionStatus}");
            if (fishingManager.IsInValidRoute)
            {
                ImGui.TextColored(new Vector4(0, 1, 0, 1), "✓ In valid ocean fishing zone");

                // Zone time remaining
                ImGui.SameLine(ImGui.GetWindowWidth() - 100);
                ImGui.Text($"Time: {fishingManager.GetZoneTimeRemaining()}");
            }
            else
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), "✗ Not in a valid ocean fishing zone");
            }

            ImGui.Separator();

            // Route information
            ImGui.Text($"Current Route: {fishingManager.CurrentRoute.RouteName}");

            // Spectral current status
            if (fishingManager.IsSpectralActive)
            {
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.4f, 0.6f, 1.0f, 1.0f), $"[SPECTRAL ACTIVE - {fishingManager.GetSpectralTimeRemaining()}]");
            }

            // Bait information
            if (fishingManager.IsSpectralActive)
            {
                ImGui.Text($"Current Bait: Spectral Bait ({fishingManager.CurrentRoute.SpectralBait})");
            }
            else
            {
                ImGui.Text($"Normal Bait: {fishingManager.CurrentRoute.NormalBait}");
                ImGui.Text($"Spectral Bait: {fishingManager.CurrentRoute.SpectralBait}");
            }

            ImGui.Separator();

            // Fishing information
            ImGui.Text($"Last Tug Strength: {fishingManager.LastTugStrength}");
            if (fishingManager.LastCaughtFish != null)
                ImGui.Text($"Last Caught Fish: {fishingManager.LastCaughtFish.FishName}");
            else
                ImGui.Text("No fish caught yet.");

            ImGui.Separator();

            // Recent activity logs (mini view)
            ImGui.Text("Recent Activity:");
            float footerHeight = ImGui.GetStyle().ItemSpacing.Y + ImGui.GetFrameHeightWithSpacing();
            if (ImGui.BeginChild("MiniLogView", new Vector2(-1, 80), true))
            {
                // Display last 5 log entries in the mini view
                int startIdx = Math.Max(0, logEntries.Count - 5);
                for (int i = startIdx; i < logEntries.Count; i++)
                {
                    var entry = logEntries[i];
                    ImGui.TextColored(GetLogLevelColor(entry.Level), $"[{entry.GetFormattedTime()}] {entry.Message}");
                }

                // Auto-scroll
                if (autoScroll && logEntries.Count > 0)
                    ImGui.SetScrollHereY(1.0f);
            }
            ImGui.EndChild();

            ImGui.Separator();

            // Control buttons
            if (fishingManager.IsAutomationRunning)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1.0f));
                if (ImGui.Button("Stop Fishing", new Vector2(ImGui.GetContentRegionAvail().X, 30)))
                {
                    fishingManager.StopAutomation();
                    AddLogEntry("Fishing automation stopped", LogLevel.Info);
                }
                ImGui.PopStyleColor();
            }
            else
            {
                bool canStart = fishingManager.IsInValidRoute && config.EnableAutomation;

                if (!canStart)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.8f, 0.2f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.9f, 0.3f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.1f, 0.7f, 0.1f, 1.0f));
                }

                if (ImGui.Button("Start Fishing", new Vector2(ImGui.GetContentRegionAvail().X, 30)) && canStart)
                {
                    fishingManager.StartAutomation();
                    AddLogEntry("Fishing automation started", LogLevel.Info);
                }

                ImGui.PopStyleColor(3);

                if (!fishingManager.IsInValidRoute)
                {
                    ImGui.TextColored(new Vector4(1, 0.5f, 0, 1), "Cannot start: Not in an ocean fishing zone");
                }
                else if (!config.EnableAutomation)
                {
                    ImGui.TextColored(new Vector4(1, 0.5f, 0, 1), "Cannot start: Automation is disabled in settings");
                }
            }
        }

        private void DrawLogsTab()
        {
            // Log controls
            ImGui.Checkbox("Auto-scroll", ref autoScroll);
            ImGui.SameLine();
            if (ImGui.Button("Clear Logs"))
            {
                logEntries.Clear();
                AddLogEntry("Logs cleared", LogLevel.Info);
            }

            ImGui.Separator();

            // Full log view
            if (ImGui.BeginChild("LogView", new Vector2(-1, -1), true))
            {
                foreach (var entry in logEntries)
                {
                    ImGui.TextColored(GetLogLevelColor(entry.Level), $"[{entry.GetFormattedTime()}] {entry.Message}");
                }

                // Auto-scroll
                if (autoScroll && logEntries.Count > 0)
                    ImGui.SetScrollHereY(1.0f);
            }
            ImGui.EndChild();
        }

        private void DrawSettingsTab()
        {
            ImGui.Text("Enable Automation:");
            bool enable = config.EnableAutomation;
            if (ImGui.Checkbox("##EnableAutomation", ref enable))
            {
                config.EnableAutomation = enable;
                config.Save();
                AddLogEntry($"Automation {(enable ? "enabled" : "disabled")}", LogLevel.Info);
            }

            ImGui.TextDisabled("Toggle automation on/off globally");
            ImGui.Spacing();

            ImGui.Text("Use Auto-Mooch:");
            bool autoMooch = config.UseAutoMooch;
            if (ImGui.Checkbox("##UseAutoMooch", ref autoMooch))
            {
                config.UseAutoMooch = autoMooch;
                config.Save();
                AddLogEntry($"Auto-Mooch {(autoMooch ? "enabled" : "disabled")}", LogLevel.Info);
            }

            ImGui.TextDisabled("Automatically mooch when a suitable fish is caught");
            ImGui.Spacing();

            ImGui.Text("Auto-Chum on Full GP:");
            bool autoChum = config.UseAutoChumOnFullGP;
            if (ImGui.Checkbox("##UseAutoChum", ref autoChum))
            {
                config.UseAutoChumOnFullGP = autoChum;
                config.Save();
                AddLogEntry($"Auto-Chum {(autoChum ? "enabled" : "disabled")}", LogLevel.Info);
            }

            ImGui.TextDisabled("Automatically use Chum when GP is near full");
            ImGui.Spacing();

            ImGui.Text("GP Full Threshold:");
            int gpThreshold = config.GPFullThreshold;
            if (ImGui.SliderInt("##GPFullThreshold", ref gpThreshold, 0, 800, "%d GP"))
            {
                config.GPFullThreshold = gpThreshold < 0 ? 0 : gpThreshold;
                config.Save();
                AddLogEntry($"GP threshold set to {gpThreshold}", LogLevel.Info);
            }

            ImGui.TextDisabled("GP threshold to consider as 'full' for auto-chum");
            ImGui.Spacing();

            // Save current route for persistence
            if (ImGui.Button("Save Current Route as Default", new Vector2(250, 24)))
            {
                config.LastUsedRoute = fishingManager.CurrentRoute.RouteShortName;
                config.Save();
                AddLogEntry($"Saved '{fishingManager.CurrentRoute.RouteName}' as default route", LogLevel.Info);
                ImGui.OpenPopup("RouteSaved");
            }

            if (ImGui.BeginPopup("RouteSaved"))
            {
                ImGui.Text("Route saved as default!");
                ImGui.EndPopup();
            }

            ImGui.SameLine();
            ImGui.TextDisabled("?");
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("Saves the current route as default for the next time you start the plugin");
                ImGui.EndTooltip();
            }

            // Log settings section
            ImGui.Separator();
            ImGui.Text("Log Settings:");

            if (ImGui.Button("Export Logs"))
            {
                ExportLogs();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("?");
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("Export logs to a text file");
                ImGui.EndTooltip();
            }
        }

        // Log entry color based on level
        private Vector4 GetLogLevelColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    return new Vector4(0.7f, 0.7f, 0.7f, 1.0f); // Gray
                case LogLevel.Info:
                    return new Vector4(1.0f, 1.0f, 1.0f, 1.0f); // White
                case LogLevel.Warning:
                    return new Vector4(1.0f, 0.8f, 0.0f, 1.0f); // Yellow
                case LogLevel.Error:
                    return new Vector4(1.0f, 0.3f, 0.3f, 1.0f); // Red
                default:
                    return new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            }
        }

        // Add a log entry
        public void AddLogEntry(string message, LogLevel level = LogLevel.Info)
        {
            logEntries.Add(new LogEntry(message, level));

            // Limit log size
            if (logEntries.Count > MAX_LOG_ENTRIES)
            {
                logEntries.RemoveAt(0);
            }

            // Also log to Dalamud log system based on level
            switch (level)
            {
                case LogLevel.Debug:
                    Plugin.Log.Debug(message);
                    break;
                case LogLevel.Info:
                    Plugin.Log.Information(message);
                    break;
                case LogLevel.Warning:
                    Plugin.Log.Warning(message);
                    break;
                case LogLevel.Error:
                    Plugin.Log.Error(message);
                    break;
            }
        }

        // Export logs to file
        private void ExportLogs()
        {
            try
            {
                string filename = $"OceanFishingLogs_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                // Fix: Use Plugin.PluginInterface instead of plugin.PluginInterface
                string path = Path.Combine(Plugin.PluginInterface.GetPluginConfigDirectory(), filename);

                using (StreamWriter writer = new StreamWriter(path))
                {
                    writer.WriteLine($"Ocean Fishing Automator - Log Export - {DateTime.Now}");
                    writer.WriteLine(new string('-', 50));

                    foreach (var entry in logEntries)
                    {
                        writer.WriteLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] {entry.Message}");
                    }
                }

                AddLogEntry($"Logs exported to {filename}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                AddLogEntry($"Failed to export logs: {ex.Message}", LogLevel.Error);
            }
        }

        // Event handlers for fishing events
        private void OnFishingStatusChanged(string status)
        {
            AddLogEntry(status);
        }

        private void OnFishCaught(Fish fish)
        {
            if (fish != null)
            {
                AddLogEntry($"Caught: {fish.FishName} ({fish.Points} points)", LogLevel.Info);
            }
        }

        private void OnBaitChanged(uint baitId, string baitName)
        {
            AddLogEntry($"Changed bait to {baitName} (ID: {baitId})", LogLevel.Info);
        }

        private void OnTugDetected(string tugType)
        {
            AddLogEntry($"Tug detected: {tugType}", LogLevel.Info);
        }

        public void Dispose()
        {
            // Unsubscribe from events
            if (fishingManager != null)
            {
                fishingManager.OnStatusChanged -= OnFishingStatusChanged;
                fishingManager.OnFishCaught -= OnFishCaught;
                fishingManager.OnBaitChanged -= OnBaitChanged;
                fishingManager.OnTugDetected -= OnTugDetected;
            }
        }
    }
}
