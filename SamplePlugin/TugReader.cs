using Dalamud.Game;
using Dalamud.Plugin.Services;
using OceanFishingAutomator;
using System;
using System.Runtime.InteropServices;

namespace OceanFishingAutomator.SeFunctions
{
    /// <summary>
    /// Represents the different tug strengths as read from memory.
    /// </summary>
    public enum TugType : byte
    {
        Unknown = 0,
        Light = 1,
        Strong = 2,
        Legendary = 3
    }

    /// <summary>
    /// Reads the current tug value from game memory using a signature scan.
    /// </summary>
    public unsafe class TugReader
    {
        private readonly ISigScanner sigScanner;
        private IntPtr tugAddress;

        /// <summary>
        /// Initializes a new instance of the <see cref="TugReader"/> class.
        /// </summary>
        /// <param name="sigScanner">The Dalamud SigScanner service.</param>
        public TugReader(ISigScanner sigScanner)
        {
            this.sigScanner = sigScanner;
            Initialize();
        }

        /// <summary>
        /// Uses a signature to locate the tug value in memory.
        /// Replace the signature string with the actual signature from CombatReborn/AutoHook.
        /// </summary>
        private void Initialize()
        {
            // Example signature – REPLACE with the correct signature.
            tugAddress = sigScanner.ScanText("E8 ?? ?? ?? ?? 80 7B 3E 00 48 8D 3D");
            if (tugAddress == IntPtr.Zero)
            {
                Plugin.Log.Error("TugReader: Failed to find tug address using signature scan.");
            }
            else
            {
                Plugin.Log.Debug($"TugReader: Found tug address at 0x{tugAddress.ToString("X")}");
            }
        }

        /// <summary>
        /// Reads the current tug value from memory.
        /// </summary>
        /// <returns>The current tug as a <see cref="TugType"/>.</returns>
        public TugType GetCurrentTug()
        {
            if (tugAddress == IntPtr.Zero)
                return TugType.Unknown;
            try
            {
                byte tugValue = Marshal.ReadByte(tugAddress);
                return tugValue switch
                {
                    1 => TugType.Light,
                    2 => TugType.Strong,
                    3 => TugType.Legendary,
                    _ => TugType.Unknown,
                };
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"TugReader: Exception while reading tug value: {ex.Message}");
                return TugType.Unknown;
            }
        }
    }
}
