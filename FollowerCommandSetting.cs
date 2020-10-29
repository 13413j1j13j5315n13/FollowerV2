using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FollowerV2
{
    public class FollowerCommandSetting
    {
        // Must be public for JSON's "SerializeObject" to work
        public List<FollowerCommandsDataClass> FollowerCommandsDataSet = new List<FollowerCommandsDataClass>();

        public void AddNewFollower(string followerName)
        {
            if (string.IsNullOrEmpty(followerName)) return;

            followerName = followerName.Trim();

            var contains = FollowerCommandsDataSet.Select(a => a.FollowerName == followerName).FirstOrDefault();
            if (contains) return;

            var follower = new FollowerCommandsDataClass(followerName);
            FollowerCommandsDataSet.Add(follower);
        }

        public void RemoveFollower(string followerName)
        {
            if (string.IsNullOrEmpty(followerName)) return;

            var follower = FollowerCommandsDataSet.FirstOrDefault(a => a.FollowerName == followerName);
            if (follower != null) FollowerCommandsDataSet.Remove(follower);
        }

        public void SetFollowersToEnterHideout(string hideoutCharacterName)
        {
            FollowerCommandsDataSet.ForEach(f => { f.SetEnterHideout(hideoutCharacterName); });
        }
    }

    public class FollowerCommandsDataClass
    {
        [JsonIgnore] private readonly DateTime _emptyDateTime = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [JsonIgnore] private readonly int _taskDelayMs = 2000;

        public bool Aggressive = true;

        public List<FollowerSkill> FollowerSkills = new List<FollowerSkill>();

        public int NormalItemId;

        public bool ShouldLevelUpGems = false;

        public FollowerCommandsDataClass()
        {
        }

        public FollowerCommandsDataClass(string followerName)
        {
            FollowerName = followerName;
        }

        public string FollowerName { get; set; }

        public string HideoutCharacterName { get; set; }

        public DateTime LastTimeEntranceUsedDateTime { get; set; } = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public DateTime LastTimePortalUsedDateTime { get; set; } = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public DateTime LastTimeQuestItemPickupDateTime { get; set; } =
            new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public DateTime LastTimeNormalItemPickupDateTime { get; set; } =
            new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public DateTime LastTimeEnterHideoutUsedDateTime { get; set; } =
            new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public void SetToUseEntrance()
        {
            if (LastTimeEntranceUsedDateTime != _emptyDateTime) return;

            LastTimeEntranceUsedDateTime = DateTime.UtcNow;
            Task.Delay(_taskDelayMs).ContinueWith(t => LastTimeEntranceUsedDateTime = _emptyDateTime);
        }

        public void SetToUsePortal()
        {
            if (LastTimePortalUsedDateTime != _emptyDateTime) return;

            LastTimePortalUsedDateTime = DateTime.UtcNow;
            Task.Delay(_taskDelayMs).ContinueWith(t => LastTimePortalUsedDateTime = _emptyDateTime);
        }

        public void SetPickupQuestItem()
        {
            if (LastTimeQuestItemPickupDateTime != _emptyDateTime) return;

            LastTimeQuestItemPickupDateTime = DateTime.UtcNow;
            Task.Delay(_taskDelayMs).ContinueWith(t => LastTimeQuestItemPickupDateTime = _emptyDateTime);
        }

        public void SetEnterHideout(string hideoutCharacterName)
        {
            if (LastTimeEnterHideoutUsedDateTime != _emptyDateTime ||
                string.IsNullOrEmpty(hideoutCharacterName)) return;

            LastTimeEnterHideoutUsedDateTime = DateTime.UtcNow;
            HideoutCharacterName = hideoutCharacterName;

            Task.Delay(_taskDelayMs).ContinueWith(t => LastTimeEnterHideoutUsedDateTime = _emptyDateTime);
        }

        public void SetPickupNormalItem(int itemId)
        {
            if (LastTimeNormalItemPickupDateTime != _emptyDateTime) return;

            LastTimeNormalItemPickupDateTime = DateTime.UtcNow;
            NormalItemId = itemId;
            Task.Delay(_taskDelayMs).ContinueWith(t =>
            {
                LastTimeNormalItemPickupDateTime = _emptyDateTime;
                NormalItemId = itemId;
            });
        }

        public void AddNewEmptySkill()
        {
            var id = new Random().Next(0, 100);
            FollowerSkills.Add(new FollowerSkill(id));
        }

        public void RemoveSkill(int skillId)
        {
            FollowerSkills.RemoveAll(s => s.Id == skillId);
        }
    }
}