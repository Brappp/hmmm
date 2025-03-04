using System;
using System.Timers;
using OceanFishingAutomator.Definitions;
using OceanFishingAutomator.SeFunctions;
// Alias to ensure we use the TugType from SeFunctions.
using TugType = OceanFishingAutomator.SeFunctions.TugType;
using Dalamud.Game;
using Dalamud.Plugin.Services;

namespace OceanFishingAutomator
{
    /// <summary>
    /// Represents the various states of the fishing automation cycle.
    /// </summary>
    public enum FishingState
    {
        Idle,
        Casting,
        WaitingForBite,
        Reeling,
        Mooching,
        Completed
    }

    /// <summary>
    /// Manages the fishing automation cycle.
    /// Integrates tug reading from memory via TugReader.
    /// </summary>
    public class FishingManager : IDisposable
    {
        private readonly Timer updateTimer;
        private readonly Configuration config;
        public FishingState State { get; private set; } = FishingState.Idle;

        public Route CurrentRoute { get; private set; }
        public Fish LastCaughtFish { get; private set; }
        public string CurrentActionStatus { get; private set; } = "Idle";
        public uint CurrentBaitId { get; private set; }
        public string LastTugStrength { get; private set; } = "None";

        // Flag to indicate if automation is currently running
        public bool IsAutomationRunning { get; private set; } = false;

        // Flag to indicate if player is in a valid fishing route
        public bool IsInValidRoute { get; private set; } = false;

        // Flag to indicate if a spectral current is active
        public bool IsSpectralActive => routeTracker?.IsSpectralActive ?? false;

        // Route and spectral tracking
        private readonly RouteTracker routeTracker;

        // Instance to read tug from memory.
        private readonly TugReader tugReader;
        private readonly IClientState clientState;

        // Events for logging system
        public event Action<string> OnStatusChanged;
        public event Action<Fish> OnFishCaught;
        public event Action<uint, string> OnBaitChanged;
        public event Action<string> OnTugDetected;

        /// <summary>
        /// Initializes a new instance of the FishingManager.
        /// </summary>
        /// <param name="config">The plugin configuration.</param>
        /// <param name="sigScanner">The Dalamud SigScanner service.</param>
        public FishingManager(Configuration config, ISigScanner sigScanner)
        {
            this.config = config;
            CurrentRoute = Routes.GetDefaultRoute();
            tugReader = new TugReader(sigScanner);
            clientState = Plugin.ClientState;
            routeTracker = new RouteTracker(Plugin.Framework);

            // Subscribe to route tracker events
            routeTracker.OnRouteOrZoneChanged += OnRouteOrZoneChanged;
            routeTracker.OnSpectralStatusChanged += OnSpectralStatusChanged;

            updateTimer = new Timer(1000); // 1-second interval
            updateTimer.Elapsed += OnUpdate;
            updateTimer.AutoReset = true;
            updateTimer.Start();
        }

        /// <summary>
        /// Starts the fishing automation.
        /// </summary>
        public void StartAutomation()
        {
            if (!config.EnableAutomation)
            {
                UpdateStatus("Automation is disabled in settings.");
                return;
            }

            if (!IsInValidRoute)
            {
                UpdateStatus("Not in a valid ocean fishing route.");
                return;
            }

            IsAutomationRunning = true;
            State = FishingState.Idle;
            UpdateStatus("Automation started. Preparing to cast...");
            Plugin.Log.Debug("Fishing automation started.");
        }

        /// <summary>
        /// Stops the fishing automation.
        /// </summary>
        public void StopAutomation()
        {
            IsAutomationRunning = false;
            State = FishingState.Idle;
            UpdateStatus("Automation stopped.");
            Plugin.Log.Debug("Fishing automation stopped.");
        }

        /// <summary>
        /// Handler for route or zone changes
        /// </summary>
        private void OnRouteOrZoneChanged(uint routeId, byte zoneIndex)
        {
            // Update the current route based on the route ID
            CurrentRoute = Routes.GetRouteByIndex(routeId);
            Plugin.Log.Debug($"Route changed to {CurrentRoute.RouteName}, zone {zoneIndex + 1}");

            // Update UI to show the new route information
            UpdateStatus($"Now at {CurrentRoute.RouteName}, zone {zoneIndex + 1}");
        }

        /// <summary>
        /// Handler for spectral current status changes
        /// </summary>
        private void OnSpectralStatusChanged(bool isSpectralActive)
        {
            if (isSpectralActive)
            {
                Plugin.Log.Debug("Spectral current started");
                UpdateStatus("Spectral current active! Switching to spectral bait...");

                // Could auto-switch bait here
                CurrentBaitId = CurrentRoute.SpectralBait;
                OnBaitChanged?.Invoke(CurrentBaitId, $"Spectral Bait ({CurrentBaitId})");
            }
            else
            {
                Plugin.Log.Debug("Spectral current ended");
                UpdateStatus("Spectral current ended. Reverting to normal bait...");

                // Could auto-switch bait here
                CurrentBaitId = CurrentRoute.NormalBait;
                OnBaitChanged?.Invoke(CurrentBaitId, $"Normal Bait ({CurrentBaitId})");
            }
        }

        /// <summary>
        /// Checks if the player is currently in a valid fishing route.
        /// </summary>
        public void CheckCurrentRoute()
        {
            // Check if we have valid ocean fishing information
            var isValid = routeTracker.CurrentRoute > 0;

            // Only update if the state has changed
            if (isValid != IsInValidRoute)
            {
                IsInValidRoute = isValid;

                if (IsInValidRoute)
                {
                    CurrentRoute = Routes.GetRouteByIndex(routeTracker.CurrentRoute);
                    UpdateStatus($"Entered {CurrentRoute.RouteName}, zone {routeTracker.CurrentZone + 1}");
                    Plugin.Log.Debug($"Entered valid ocean fishing route: {CurrentRoute.RouteName}");
                }
                else
                {
                    UpdateStatus("Not in ocean fishing.");
                    Plugin.Log.Debug("Exited ocean fishing route.");

                    if (IsAutomationRunning)
                    {
                        StopAutomation();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the formatted spectral time remaining.
        /// </summary>
        public string GetSpectralTimeRemaining()
        {
            return routeTracker.GetFormattedSpectralTime();
        }

        /// <summary>
        /// Gets the formatted time left in zone.
        /// </summary>
        public string GetZoneTimeRemaining()
        {
            return routeTracker.GetFormattedZoneTime();
        }

        /// <summary>
        /// Updates the current status and triggers the status changed event
        /// </summary>
        public void UpdateStatus(string status)
        {
            CurrentActionStatus = status;
            OnStatusChanged?.Invoke(status);
        }

        /// <summary>
        /// Updates the fishing state machine.
        /// </summary>
        private void OnUpdate(object sender, ElapsedEventArgs e)
        {
            // Check if we're in a valid fishing route
            CheckCurrentRoute();

            // Don't process fishing logic if automation isn't running
            if (!IsAutomationRunning)
            {
                return;
            }

            // Don't continue if we're not in a valid route anymore
            if (!IsInValidRoute)
            {
                UpdateStatus("Not in a valid ocean fishing route. Stopping automation.");
                StopAutomation();
                return;
            }

            switch (State)
            {
                case FishingState.Idle:
                    UpdateStatus("Casting...");
                    State = FishingState.Casting;
                    break;
                case FishingState.Casting:
                    UpdateStatus("Waiting for bite...");
                    State = FishingState.WaitingForBite;
                    break;
                case FishingState.WaitingForBite:
                    TugType tug = tugReader.GetCurrentTug();
                    switch (tug)
                    {
                        case TugType.Light:
                            LastTugStrength = "Light";
                            break;
                        case TugType.Strong:
                            LastTugStrength = "Strong";
                            break;
                        case TugType.Legendary:
                            LastTugStrength = "Legendary";
                            break;
                        default:
                            LastTugStrength = "Unknown";
                            break;
                    }
                    UpdateStatus($"Bite detected: {LastTugStrength} tug. Reeling in...");
                    OnTugDetected?.Invoke(LastTugStrength);
                    State = FishingState.Reeling;
                    break;
                case FishingState.Reeling:
                    UpdateStatus((LastTugStrength == "Light") ? "Using Precision Hookset" : "Using Powerful Hookset");
                    System.Threading.Thread.Sleep(500);
                    var fishList = FishDataCache.GetFish();
                    if (fishList.Count > 0)
                    {
                        LastCaughtFish = fishList[0];
                        UpdateStatus($"Caught: {LastCaughtFish.FishName}");
                        OnFishCaught?.Invoke(LastCaughtFish);
                    }
                    else
                    {
                        UpdateStatus("Caught: Unknown Fish");
                        OnFishCaught?.Invoke(null);
                    }
                    State = FishingState.Mooching;
                    break;
                case FishingState.Mooching:
                    if (config.UseAutoMooch)
                    {
                        UpdateStatus("Attempting Mooch...");
                        State = FishingState.Reeling;
                    }
                    else
                    {
                        State = FishingState.Completed;
                    }
                    break;
                case FishingState.Completed:
                    UpdateStatus("Cycle complete. Moving to next stop...");
                    CurrentRoute = Routes.GetNextRoute(CurrentRoute);
                    State = FishingState.Idle;
                    break;
            }
        }

        public void Dispose()
        {
            updateTimer?.Stop();
            updateTimer?.Dispose();
            routeTracker?.Dispose();
        }
    }
}
