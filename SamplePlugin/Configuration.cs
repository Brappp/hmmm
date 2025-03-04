using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace OceanFishingAutomator
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        // Toggle the overall automation.
        public bool EnableAutomation { get; set; } = true;
        // Automatically try to mooch when possible.
        public bool UseAutoMooch { get; set; } = true;
        // When GP is near full, automatically use a GP-dump action.
        public bool UseAutoChumOnFullGP { get; set; } = true;
        // Threshold (in GP points) to consider GP as full.
        public int GPFullThreshold { get; set; } = 100;
        // Last used route (for persistence, if desired).
        public string LastUsedRoute { get; set; } = string.Empty;

        // Spectral current settings
        // Automatically switch to spectral bait when a spectral current starts.
        public bool AutoSwitchToSpectralBait { get; set; } = true;
        // Automatically switch back to normal bait when a spectral current ends.
        public bool AutoSwitchToNormalBait { get; set; } = true;
        // Play a sound alert when a spectral current starts.
        public bool PlaySpectralAlert { get; set; } = true;
        // Format string for chat alert when spectral current starts.
        public string SpectralAlertFormat { get; set; } = "[Ocean Fishing] Spectral current active! {0} seconds remaining.";

        public void Save()
        {
            Plugin.PluginInterface.SavePluginConfig(this);
        }
    }
}
