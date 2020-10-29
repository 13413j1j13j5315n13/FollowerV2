using System.IO;
using Newtonsoft.Json;

namespace FollowerV2
{
    internal class FileCommandProtocol : ICommandProtocol
    {
        private readonly FollowerV2Settings _followerSettings;

        public FileCommandProtocol(FollowerV2Settings followerSettings)
        {
            _followerSettings = followerSettings;
        }

        public void Start()
        {
            // Nothing needed
        }

        public void Restart()
        {
            // Nothing needed
        }

        public void Stop()
        {
            // Nothing needed
        }

        public void Work(NetworkActivityObject obj)
        {
            // This will overwrite the existing file
            File.WriteAllText(GetFileName(), JsonConvert.SerializeObject(obj));
        }

        private string GetFileName()
        {
            return _followerSettings.LeaderModeSettings.LeaderModeFileSettings.FilePath.Value;
        }
    }
}