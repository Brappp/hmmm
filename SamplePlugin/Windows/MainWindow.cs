using Dalamud.Interface.Windowing;
using ImGuiNET;
using OceanFishingAutomator;
using OceanFishingAutomator.Definitions;
using System.Numerics;

namespace OceanFishingAutomator.UI
{
    public class MainWindow : Window, System.IDisposable
    {
        private readonly Plugin plugin;
        private readonly FishingManager fishingManager;
        private readonly Configuration config;
        private bool showConfigSection = false;

        public MainWindow(Plugin plugin, FishingManager fishingManager)
            : base("Ocean Fishing Automator")
        {
            this.plugin = plugin;
            this.fishingManager = fishingManager;
            this.config = plugin.Configuration;
            Size = new Vector2(420, 350);
            SizeCondition = ImGuiCond.Always;
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

            // Control buttons
            if (fishingManager.IsAutomationRunning)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1.0f));
                if (ImGui.Button("Stop Fishing", new Vector2(ImGui.GetContentRegionAvail().X, 30)))
                {
                    fishingManager.StopAutomation();
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

        private void DrawSettingsTab()
        {
            ImGui.Text("Enable Automation:");
            bool enable = config.EnableAutomation;
            if (ImGui.Checkbox("##EnableAutomation", ref enable))
            {
                config.EnableAutomation = enable;
                config.Save();
            }

            ImGui.TextDisabled("Toggle automation on/off globally");
            ImGui.Spacing();

            ImGui.Text("Use Auto-Mooch:");
            bool autoMooch = config.UseAutoMooch;
            if (ImGui.Checkbox("##UseAutoMooch", ref autoMooch))
            {
                config.UseAutoMooch = autoMooch;
                config.Save();
            }

            ImGui.TextDisabled("Automatically mooch when a suitable fish is caught");
            ImGui.Spacing();

            ImGui.Text("Auto-Chum on Full GP:");
            bool autoChum = config.UseAutoChumOnFullGP;
            if (ImGui.Checkbox("##UseAutoChum", ref autoChum))
            {
                config.UseAutoChumOnFullGP = autoChum;
                config.Save();
            }

            ImGui.TextDisabled("Automatically use Chum when GP is near full");
            ImGui.Spacing();

            ImGui.Text("GP Full Threshold:");
            int gpThreshold = config.GPFullThreshold;
            if (ImGui.SliderInt("##GPFullThreshold", ref gpThreshold, 0, 800, "%d GP"))
            {
                config.GPFullThreshold = gpThreshold < 0 ? 0 : gpThreshold;
                config.Save();
            }

            ImGui.TextDisabled("GP threshold to consider as 'full' for auto-chum");
            ImGui.Spacing();

            // Save current route for persistence
            if (ImGui.Button("Save Current Route as Default", new Vector2(250, 24)))
            {
                config.LastUsedRoute = fishingManager.CurrentRoute.RouteShortName;
                config.Save();
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
        }

        public void Dispose()
        {
            // Dispose resources if needed.
        }
    }
}
