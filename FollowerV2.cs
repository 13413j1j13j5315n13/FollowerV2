using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using SharpDX;

namespace FollowerV2
{
    public class Follower : BaseSettingsPlugin<FollowerV2Settings>
    {

        private Coroutine _nearbyPlayersUpdateCoroutine;

        public override bool Initialise()
        {
            Settings.Profiles.OnValueSelected += OnProfileChange;
            Settings.FollowerModeSettings.FollowerModes.OnValueSelected += OnFollowerModeChange; // local or network
            Settings.FollowerModeSettings.NearbyPlayers.OnValueSelected += OnNearbyPlayerAsLeaderSelect;
            Settings.LeaderModeSettings.SetMyselfAsLeader.OnPressed += OnSetMyselfAsLeaderToPropagateChanged;

            _nearbyPlayersUpdateCoroutine = new Coroutine(UpdateNearbyPlayersWork(), this, "Update nearby players");

            // Fire all coroutines
            Core.ParallelRunner.Run(_nearbyPlayersUpdateCoroutine);

            return true;
        }

        public override void DrawSettings()
        {
            Settings.DrawSettings();
        }

        private void OnProfileChange(string profile)
        {
            LogMsgWithVerboseDebug("OnProfileChange called");
            LogMsgWithDebug($"Profile changed to: {profile}");

            if (profile == ProfilesEnum.Follower)
            {

            }
            else if (profile == ProfilesEnum.Leader)
            {

            }
            else if (profile == ProfilesEnum.Disable)
            {

            }
            else
            {
                LogError($"Profile changed to unsupported value: {profile}. This should not have happened...");
            }
        }

        private void OnFollowerModeChange(string newFollowerMode)
        {
            LogMsgWithVerboseDebug("OnFollowerModeChange called");

            if (newFollowerMode == FollowerNetworkActivityModeEnum.Local)
            {

            }
            else if (newFollowerMode == FollowerNetworkActivityModeEnum.Network)
            {

            }
        }

        private void OnSetMyselfAsLeaderToPropagateChanged()
        {
            LogMsgWithVerboseDebug("OnSetMyselfAsLeaderToPropagateChanged called");

            string name = GameController.Player.GetComponent<Player>().PlayerName;
            Settings.LeaderModeSettings.LeaderNameToPropagate.Value = name;
        }

        private void OnNearbyPlayerAsLeaderSelect(string name)
        {
            LogMsgWithVerboseDebug("OnNearbyPlayerAsLeaderSelect called");

            if (name != "") Settings.FollowerModeSettings.LeaderName.Value = name;

            Settings.FollowerModeSettings.NearbyPlayers.Value = "";
        }

        private IEnumerator UpdateNearbyPlayersWork()
        {
            LogMsgWithVerboseDebug("Starting UpdateNearbyPlayersWork function");
            while (true)
            {
                List<string> playerNames = GameController.EntityListWrapper
                    .Entities.Where(e => e.Type == EntityType.Player)
                    .Where(e =>
                    {
                        string playerName = e.GetComponent<Player>().PlayerName;
                        if (playerName == "") return false;
                        return playerName != GameController.Player.GetComponent<Player>().PlayerName;

                    })
                    .Select(e => e.GetComponent<Player>().PlayerName)
                    .ToList();

                Settings.FollowerModeSettings.NearbyPlayers.Values = playerNames;

                yield return new WaitTime(1000);
            }
        }

        private void LogMsgWithDebug(string message)
        {
            if (!Settings.Debug.Value) return;
            LogMessage(message);
        }

        private void LogMsgWithVerboseDebug(string message)
        {
            if (Settings.Debug.Value && Settings.VerboseDebug.Value)
                LogMessage(message);
        }

        private long GetDeltaInMilliseconds(DateTime lastTime)
        {
            long currentMs = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
            long lastTimeMs = ((DateTimeOffset)lastTime).ToUnixTimeMilliseconds();
            return currentMs - lastTimeMs;
        }

    }
}
