using System;
using System.Collections.Generic;
using System.Linq;

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
        public uint RouteId { get; set; }
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
                SpectralFishIDs = new List<int> { 28940, 28941 },
                RouteId = 1
            },
            new Route
            {
                RouteName = "Southern Merlthor",
                RouteShortName = "southernmerlthor",
                NormalBait = 29716,
                SpectralBait = 2613,
                NormalFishIDs = new List<int> { 29722, 29739 },
                SpectralFishIDs = new List<int> { 29757, 29758 },
                RouteId = 2
            },
            new Route
            {
                RouteName = "Northern Merlthor",
                RouteShortName = "northernmerlthor",
                NormalBait = 29716,
                SpectralBait = 2619,
                NormalFishIDs = new List<int> { 29736, 29737 },
                SpectralFishIDs = new List<int> { 29748, 29776 },
                RouteId = 3
            },
            new Route
            {
                RouteName = "Rhotano Sea",
                RouteShortName = "rhotano",
                NormalBait = 29716,
                SpectralBait = 2591,
                NormalFishIDs = new List<int> { 29728, 29729 },
                SpectralFishIDs = new List<int> { 29775, 29767 },
                RouteId = 4
            },
            new Route
            {
                RouteName = "Cieldalaes Margin",
                RouteShortName = "cieldalaes",
                NormalBait = 29716,
                SpectralBait = 27590,
                NormalFishIDs = new List<int> { 32055, 32056 },
                SpectralFishIDs = new List<int> { 32065, 32066 },
                RouteId = 5
            },
            new Route
            {
                RouteName = "Bloodbrine Sea",
                RouteShortName = "bloodbrine",
                NormalBait = 29716,
                SpectralBait = 2587,
                NormalFishIDs = new List<int> { 32075, 32076 },
                SpectralFishIDs = new List<int> { 32085, 32086 },
                RouteId = 6
            },
            new Route
            {
                RouteName = "Rothlyt Sound",
                RouteShortName = "rothlyt",
                NormalBait = 29716,
                SpectralBait = 2603,
                NormalFishIDs = new List<int> { 32095, 32096 },
                SpectralFishIDs = new List<int> { 32105, 32106 },
                RouteId = 7
            },
            new Route
            {
                RouteName = "Sirensong Sea",
                RouteShortName = "sirensong",
                NormalBait = 29716,
                SpectralBait = 36593,
                NormalFishIDs = new List<int> { 40524, 40522 },
                SpectralFishIDs = new List<int> { 40531, 40532 },
                RouteId = 8
            },
            new Route
            {
                RouteName = "Kugane Coast",
                RouteShortName = "kugane",
                NormalBait = 29716,
                SpectralBait = 40551,
                NormalFishIDs = new List<int> { 40541, 40542 },
                SpectralFishIDs = new List<int> { 40551, 40552 },
                RouteId = 9
            },
            new Route
            {
                RouteName = "Ruby Sea",
                RouteShortName = "ruby",
                NormalBait = 29716,
                SpectralBait = 27590,
                NormalFishIDs = new List<int> { 40561, 40562 },
                SpectralFishIDs = new List<int> { 40571, 40572 },
                RouteId = 10
            },
            new Route
            {
                RouteName = "One River",
                RouteShortName = "oneriver",
                NormalBait = 29716,
                SpectralBait = 12704,
                NormalFishIDs = new List<int> { 40581, 40582 },
                SpectralFishIDs = new List<int> { 40591, 40592 },
                RouteId = 11
            }
        };

        public static Route GetDefaultRoute() => _routes[0];

        public static Route GetNextRoute(Route currentRoute)
        {
            int idx = _routes.IndexOf(currentRoute);
            return (idx < 0 || idx + 1 >= _routes.Count) ? _routes[0] : _routes[idx + 1];
        }

        /// <summary>
        /// Gets a route by its index (IKDRoute ID).
        /// </summary>
        /// <param name="routeId">The route ID to look up.</param>
        /// <returns>The route corresponding to the ID or the default route if not found.</returns>
        public static Route GetRouteByIndex(uint routeId)
        {
            var route = _routes.FirstOrDefault(r => r.RouteId == routeId);
            return route ?? GetDefaultRoute();
        }
    }
}
