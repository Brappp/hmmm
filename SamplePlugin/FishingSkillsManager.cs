using System;
using Dalamud.Plugin.Services;
using OceanFishingAutomator.SeFunctions;

namespace OceanFishingAutomator
{
    public class FishingSkillsManager : IDisposable
    {
        private readonly Configuration _config;
        private readonly FishingManager _fishingManager;
        private readonly TugReader _tugReader;
        private readonly RouteTracker _routeTracker;
        private readonly ICommandManager _commandManager;
        private readonly IPluginLog _log;

        public FishingSkillsManager(Configuration config, FishingManager fishingManager, TugReader tugReader, RouteTracker routeTracker, ICommandManager commandManager, IPluginLog log)
        {
            _config = config;
            _fishingManager = fishingManager;
            _tugReader = tugReader;
            _routeTracker = routeTracker;
            _commandManager = commandManager;
            _log = log;
        }

        public void ManageSkills()
        {
            // Verify automation conditions
            if (!_fishingManager.IsAutomationRunning || _fishingManager.State != FishingState.WaitingForBite)
                return;

            TugType currentTug = _tugReader.GetCurrentTug();

            if (_routeTracker.IsSpectralActive)
                HandleSpectralCurrentSkills(currentTug); // fixed method name
            else
                HandleNormalSkills(currentTug: currentTug);
        }

        private void HandleSpectralCurrentSkills(TugType tug)
        {
            if (tug == TugType.Strong || tug == TugType.Legendary)
                ExecuteSkill("Double Hook");
        }

        private void HandleNormalSkills(TugType currentTug)
        {
            // Auto Mooch when configured and conditions met
            if (_config.UseAutoMooch && currentTug != TugType.Light && CanUseAction("Mooch"))
            {
                ExecuteSkill("Mooch");
                return;
            }

            // GP check for Chum usage
            if (_config.UseAutoChumOnFullGP &&
                GetCurrentGp() >= (GetMaxGp() - _config.GPFullThreshold))
            {
                ExecuteSkill("Chum");
            }

            // Use Patience if not already active
            if (CanUseAction("Patience") && !HasStatus("Patience"))
                ExecuteSkill("Patience");

            // Use Prize Catch if available
            if (CanUseAction("Prize Catch"))
                ExecuteSkill("Prize Catch");

            // Use Surface Slap if available
            if (CanUseAction("Surface Slap"))
                ExecuteSkill("Surface Slap");
        }

        private void ExecuteSkill(string skillName)
        {
            string command = $"/ac \"{skillName}\"";
            _commandManager.ProcessCommand(command);
            _log.Debug($"Executed skill: {skillName}");
            _fishingManager.UpdateStatus($"Used skill: {skillName}");
        }

        private bool CanUseAction(string actionName)
        {
            // Placeholder logic—extend with proper cooldown checks as needed
            return true;
        }

        private bool HasStatus(string statusName)
        {
            // Placeholder for checking player status effect; implement as needed
            return false;
        }

        private int GetCurrentGp()
        {
            // Explicit casting to int to match property types
            return (int)(Plugin.ClientState.LocalPlayer?.CurrentGp ?? 0);
        }

        private int GetMaxGp()
        {
            // Explicit casting to int to match property types
            return (int)(Plugin.ClientState.LocalPlayer?.MaxGp ?? 0);
        }

        public void Dispose()
        {
            // Currently no unmanaged resources; dispose pattern ready if needed
        }
    }
}
