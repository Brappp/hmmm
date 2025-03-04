using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using OceanFishingAutomator.Definitions;

namespace OceanFishingAutomator.SeFunctions
{
    /// <summary>
    /// Tracks the current ocean fishing route and spectral current status.
    /// </summary>
    public unsafe class RouteTracker : IDisposable
    {
        private readonly IFramework framework;

        /// <summary>
        /// The current route ID (corresponds to IKDRoute sheet)
        /// </summary>
        public uint CurrentRoute { get; private set; }

        /// <summary>
        /// The current zone index (0, 1, or 2).
        /// </summary>
        public byte CurrentZone { get; private set; }

        /// <summary>
        /// Whether a spectral current is currently active.
        /// </summary>
        public bool IsSpectralActive { get; private set; }

        /// <summary>
        /// Remaining time in the current spectral current (in seconds).
        /// </summary>
        public uint SpectralTimeRemaining { get; private set; } = 120; // Default spectral time is 2 minutes

        /// <summary>
        /// Time remaining in the current zone (in seconds).
        /// </summary>
        public float TimeLeftInZone { get; private set; }

        /// <summary>
        /// Event triggered when spectral current status changes.
        /// </summary>
        public event Action<bool> OnSpectralStatusChanged;

        /// <summary>
        /// Event triggered when the route or zone changes.
        /// </summary>
        public event Action<uint, byte> OnRouteOrZoneChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="RouteTracker"/> class.
        /// </summary>
        /// <param name="framework">The Dalamud Framework service.</param>
        public RouteTracker(IFramework framework)
        {
            this.framework = framework;
            this.framework.Update += this.OnFrameworkUpdate;
        }

        /// <summary>
        /// Disposes of the RouteTracker instance.
        /// </summary>
        public void Dispose()
        {
            this.framework.Update -= this.OnFrameworkUpdate;
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            var oceanFishing = EventFramework.Instance()->GetInstanceContentOceanFishing();
            if (oceanFishing == null) return;

            // Update route and zone
            var oldRoute = this.CurrentRoute;
            var oldZone = this.CurrentZone;
            this.CurrentRoute = oceanFishing->CurrentRoute;
            this.CurrentZone = (byte)oceanFishing->CurrentZone;

            if (oldRoute != this.CurrentRoute || oldZone != this.CurrentZone)
            {
                Plugin.Log.Debug($"Route/Zone changed: Route={this.CurrentRoute}, Zone={this.CurrentZone}");
                this.OnRouteOrZoneChanged?.Invoke(this.CurrentRoute, this.CurrentZone);
            }

            // Update spectral status
            var oldSpectralActive = this.IsSpectralActive;
            this.IsSpectralActive = oceanFishing->SpectralCurrentActive;

            // Handle spectral status changes
            if (oldSpectralActive != this.IsSpectralActive)
            {
                Plugin.Log.Debug($"Spectral status changed: {this.IsSpectralActive}");
                this.OnSpectralStatusChanged?.Invoke(this.IsSpectralActive);

                if (this.IsSpectralActive)
                {
                    // Reset timer when spectral starts
                    this.SpectralTimeRemaining = 120;
                }
            }

            // Update time left in zone
            this.TimeLeftInZone = oceanFishing->InstanceContentDirector.ContentDirector.ContentTimeLeft -
                                 oceanFishing->TimeOffset;

            // Update spectral timer
            if (this.IsSpectralActive)
            {
                // Don't let spectral timer go below 0
                if (this.SpectralTimeRemaining > 0)
                {
                    this.SpectralTimeRemaining -= 1;
                }

                // Handle spectral current ending at 30s before zone change
                if (this.TimeLeftInZone <= 30 && this.SpectralTimeRemaining > this.TimeLeftInZone - 30)
                {
                    this.SpectralTimeRemaining = (uint)Math.Max(0, this.TimeLeftInZone - 30);
                }
            }
        }

        /// <summary>
        /// Gets a formatted string of the spectral time remaining.
        /// </summary>
        /// <returns>A formatted string in the format "M:SS".</returns>
        public string GetFormattedSpectralTime()
        {
            var minutes = this.SpectralTimeRemaining / 60;
            var seconds = this.SpectralTimeRemaining % 60;
            return $"{minutes}:{seconds:D2}";
        }

        /// <summary>
        /// Gets a formatted string of the time left in the zone.
        /// </summary>
        /// <returns>A formatted string in the format "M:SS".</returns>
        public string GetFormattedZoneTime()
        {
            var minutes = (int)this.TimeLeftInZone / 60;
            var seconds = (int)this.TimeLeftInZone % 60;
            return $"{minutes}:{seconds:D2}";
        }

        /// <summary>
        /// Gets the current route object.
        /// </summary>
        /// <returns>The current Route object or null if not found.</returns>
        public Route GetCurrentRoute()
        {
            return Routes.GetRouteByIndex(this.CurrentRoute);
        }
    }
}
