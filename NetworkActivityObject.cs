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
        public bool FollowersShouldWork { get; set; }
        public string LeaderName { get; set; }
        public int LeaderProximityRadius { get; set; }
        public FollowerCommandSetting FollowerCommandSettings { get; set; }
    }
}
