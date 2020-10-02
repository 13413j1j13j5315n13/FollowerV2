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
        public HashSet<FollowerCommandsDataClass> FollowerCommandsDataSet = new HashSet<FollowerCommandsDataClass>();

        public void AddNewFollower(string followerName)
        {
            if (string.IsNullOrEmpty(followerName)) return;

            bool contains = FollowerCommandsDataSet.Select(a => a.FollowerName == followerName).FirstOrDefault();
            if (contains) return;

            FollowerCommandsDataClass follower = new FollowerCommandsDataClass(followerName);
            FollowerCommandsDataSet.Add(follower);
        }

        public void RemoveFollower(string followerName)
        {
            if (string.IsNullOrEmpty(followerName)) return;

            FollowerCommandsDataClass follower = FollowerCommandsDataSet.First(a => a.FollowerName == followerName);
            FollowerCommandsDataSet.Remove(follower);
        }

        public List<string> GetNames()
        {
            return FollowerCommandsDataSet.Select(a => a.FollowerName).ToList();
        }

        private FollowerCommandsDataClass GetFollowerByName(string followerName)
        {
            return FollowerCommandsDataSet.First(a => a.FollowerName == followerName);
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
    }
}
