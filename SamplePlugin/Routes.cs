using System;
using System.Collections.Generic;

namespace OceanFishingAutomator.Definitions
{
    public class Route
    {
        public string RouteName { get; set; }
        public string RouteShortName { get; set; }
        public uint NormalBait { get; set; }
        public uint SpectralBait { get; set; }
        public List<int> NormalFishIDs { get; set; }
        public List<int> SpectralFishIDs { get; set; }
    }

    public static class Routes
    {
        private static List<Route> _routes = new List<Route>
        {
            new Route
            {
                RouteName = "Galadion Bay",
                RouteShortName = "galadion",
                NormalBait = 29716,    // example bait ID
                SpectralBait = 2603,   // example spectral bait ID
                NormalFishIDs = new List<int> { 28937, 28938 },
                SpectralFishIDs = new List<int> { 28940, 28941 }
            },
            new Route
            {
                RouteName = "Ruby Sea",
                RouteShortName = "ruby",
                NormalBait = 29716,
                SpectralBait = 2603,
                NormalFishIDs = new List<int> { 28939 },
                SpectralFishIDs = new List<int> { 28940 }
            }
        };

        public static Route GetDefaultRoute() => _routes[0];

        public static Route GetNextRoute(Route currentRoute)
        {
            int idx = _routes.IndexOf(currentRoute);
            return (idx < 0 || idx + 1 >= _routes.Count) ? _routes[0] : _routes[idx + 1];
        }
    }
}
