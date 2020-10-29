using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FollowerV2
{
    class FileCommandProtocol : ICommandProtocol
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
