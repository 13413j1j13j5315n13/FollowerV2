using System;
using System.Collections.Generic;
using System.Linq;

namespace FollowerV2
{
    public class FollowerCommandSettings
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

        public void UseWaypoint(string followerName)
        {
            var follower = GetFollowerByName(followerName);
            if (follower != null)
            {
                follower.LastTimeWaypointUsedDateTime = DateTime.UtcNow;
            }
        }

        public void UseEntrance(string followerName)
        {
            var follower = GetFollowerByName(followerName);
            if (follower != null)
            {
                follower.LastTimeEntranceUsedDateTime = DateTime.UtcNow;
            }
        }

        private FollowerCommandsDataClass GetFollowerByName(string followerName)
        {
            return FollowerCommandsDataSet.First(a => a.FollowerName == followerName);
        }
    }

    public class FollowerCommandsDataClass
    {
        public string FollowerName { get; set; }

        public DateTime LastTimeWaypointUsedDateTime { get; set; } = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public DateTime LastTimeEntranceUsedDateTime { get; set; } = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public FollowerCommandsDataClass() { }

        public FollowerCommandsDataClass(string followerName)
        {
            FollowerName = followerName;
        }
    }
}
