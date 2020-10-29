namespace FollowerV2
{
    internal class NetworkActivityObject
    {
        public bool FollowersShouldWork { get; set; }
        public string LeaderName { get; set; }
        public int LeaderProximityRadius { get; set; }
        public int MinimumFpsThreshold { get; set; }
        public FollowerCommandSetting FollowerCommandSettings { get; set; }
    }
}