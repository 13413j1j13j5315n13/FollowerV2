using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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

            bool contains = FollowerCommandsDataSet.Select(a => a.FollowerName == followerName).FirstOrDefault();
            if (contains) return;

            FollowerCommandsDataClass follower = new FollowerCommandsDataClass(followerName);
            FollowerCommandsDataSet.Add(follower);
        }

        public void RemoveFollower(string followerName)
        {
            if (string.IsNullOrEmpty(followerName)) return;

            FollowerCommandsDataClass follower = FollowerCommandsDataSet.FirstOrDefault(a => a.FollowerName == followerName);
            if (follower != null) FollowerCommandsDataSet.Remove(follower);
        }
    }

    public class FollowerCommandsDataClass
    {
        [JsonIgnore] private int _taskDelayMs = 2000;

        [JsonIgnore]
        private readonly DateTime _emptyDateTime = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public string FollowerName { get; set; }

        public DateTime LastTimeEntranceUsedDateTime { get; set; } = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public DateTime LastTimePortalUsedDateTime { get; set; } = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public DateTime LastTimeQuestItemPickupDateTime { get; set; } = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public DateTime LastTimeNormalItemPickupDateTime { get; set; } = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public int NormalItemId = 0;

        public List<FollowerSkill> FollowerSkills = new List<FollowerSkill>();

        public bool ShouldLevelUpGems = false;

        public bool Aggressive = true;

        public FollowerCommandsDataClass() { }

        public FollowerCommandsDataClass(string followerName)
        {
            this.FollowerName = followerName;
        }

        public void SetToUseEntrance()
        {
            if (this.LastTimeEntranceUsedDateTime != _emptyDateTime) return;

            this.LastTimeEntranceUsedDateTime = DateTime.UtcNow;
            Task.Delay(_taskDelayMs).ContinueWith(t => LastTimeEntranceUsedDateTime = _emptyDateTime);
        }

        public void SetToUsePortal()
        {
            if (this.LastTimePortalUsedDateTime != _emptyDateTime) return;

            this.LastTimePortalUsedDateTime = DateTime.UtcNow;
            Task.Delay(_taskDelayMs).ContinueWith(t => LastTimePortalUsedDateTime = _emptyDateTime);
        }

        public void SetPickupQuestItem()
        {
            if (this.LastTimeQuestItemPickupDateTime != _emptyDateTime) return;

            this.LastTimeQuestItemPickupDateTime = DateTime.UtcNow;
            Task.Delay(_taskDelayMs).ContinueWith(t => LastTimeQuestItemPickupDateTime = _emptyDateTime);
        }

        public void SetPickupNormalItem(int itemId)
        {
            if (this.LastTimeNormalItemPickupDateTime != _emptyDateTime) return;

            this.LastTimeNormalItemPickupDateTime = DateTime.UtcNow;
            this.NormalItemId = itemId;
            Task.Delay(_taskDelayMs).ContinueWith(t =>
            {
                LastTimeNormalItemPickupDateTime = _emptyDateTime;
                NormalItemId = itemId;
            });
        }

        public void AddNewEmptySkill()
        {
            int id = new Random().Next(0, 100);
            FollowerSkills.Add(new FollowerSkill(id));
        }

        public void RemoveSkill(int skillId)
        {
            FollowerSkills.RemoveAll(s => s.Id == skillId);
        }
    }
}
