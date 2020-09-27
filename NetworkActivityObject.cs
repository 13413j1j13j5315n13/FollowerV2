using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FollowerV2
{
    class NetworkActivityObject
    {
        [JsonProperty("working")] public bool FollowersAreWorking { get; set; }
        [JsonProperty("leader_name")] public string LeaderName { get; set; }
        [JsonProperty("leader_proximity_radius")] public int LeaderProximityRadius { get; set; }
    }
}
