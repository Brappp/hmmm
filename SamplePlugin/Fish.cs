using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace OceanFishingAutomator.Definitions
{
    public class Fish
    {
        public uint RouteID { get; set; }
        public string RouteShortName { get; set; }
        public int FishID { get; set; }
        public string FishName { get; set; }
        // Additional properties from OceanTrip’s fish definitions:
        public bool CausesSpectral { get; set; }
        public bool SpectralFish { get; set; }
        public float BiteStart { get; set; }
        public float BiteEnd { get; set; }
        public int Points { get; set; }
    }

    public static class FishDataCache
    {
        private static List<Fish> _cachedFishList;

        public static List<Fish> GetFish()
        {
            if (_cachedFishList == null)
            {
                _cachedFishList = LoadFishData();
            }
            return _cachedFishList;
        }

        public static void InvalidateCache() => _cachedFishList = null;

        private static List<Fish> LoadFishData()
        {
            try
            {
                // Assume fishList.json is in the "Resources" folder alongside the plugin DLL.
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "fishList.json");
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("Fish list file not found", filePath);
                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<List<Fish>>(json);
            }
            catch (Exception)
            {
                return new List<Fish>();
            }
        }
    }
}
