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
            Settings.Profiles.OnValueSelected += ProfileChanged;
            Settings.FollowerModeSettings.NearbyPlayers.OnValueSelected += NearbyPlayerAsLeaderSelected;

            _nearbyPlayersUpdateCoroutine = new Coroutine(UpdateNearbyPlayersWork(), this, "Update nearby players");

            // Fire all coroutines
            Core.ParallelRunner.Run(_nearbyPlayersUpdateCoroutine);

            return true;
        }

        public override void DrawSettings()
        {
            Settings.DrawSettings();
        }

        private void ProfileChanged(string profile)
        {
            LogMsgWithDebug($"Profile changed to: {profile}");

            if (profile == ProfilesEnum.Follower)
            {

            } else if (profile == ProfilesEnum.Leader)
            {

            } else if (profile == ProfilesEnum.Disable)
            {

            }
            else
            {
                LogError($"Profile changed to unsupported value: {profile}. This should not have happened...");
            }
        }

        private void NearbyPlayerAsLeaderSelected(string name)
        {
            if (name != "") Settings.FollowerModeSettings.LeaderName.Value = name;

            Settings.FollowerModeSettings.NearbyPlayers.Value = "";
        }

        private void LogMsgWithDebug(string message)
        {
            if (!Settings.Debug.Value) return;
            LogMessage(message);
        }

        private IEnumerator UpdateNearbyPlayersWork()
        {
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

    }
}
