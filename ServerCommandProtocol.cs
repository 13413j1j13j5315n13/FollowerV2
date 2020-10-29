using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ExileCore;
using Newtonsoft.Json;

namespace FollowerV2
{
    class ServerCommandProtocol: ICommandProtocol
    {
        private readonly FollowerV2Settings _followerSettings;
        private bool _serverIsListening = false;
        private HttpListener _httpListener = new HttpListener();

        public ServerCommandProtocol(FollowerV2Settings followerSettings)
        {
            _followerSettings = followerSettings;

            _httpListener.TimeoutManager.IdleConnection = TimeSpan.FromSeconds(5);
            _httpListener.TimeoutManager.EntityBody = TimeSpan.FromSeconds(5);

            AddCurrentPrefix();
        }

        public void Work(NetworkActivityObject obj)
        {
            LogMsg("Server.Work called");

            if (_serverIsListening)
            {
                LogMsg("Server._serverIsListening is true, skipping");
                return;
            }

            if (!_httpListener.IsListening)
            {
                LogMsg("HttpListener is not listening, skipping");
                return;
            }

            LogMsg("Server._serverIsListening is false, starting server listening");

            _serverIsListening = true;

            try
            {
                // SYNC WORKING SERVER
                HttpListenerContext context = _httpListener.GetContext();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                byte[] buffer = new byte[] { };
                response.ContentLength64 = 0;
                response.StatusCode = (int)HttpStatusCode.OK;

                if (request.HttpMethod == "GET")
                {
                    string responseString = JsonConvert.SerializeObject(obj);

                    buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                }

                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
                // SYNC WORKING SERVER ENDS
            }
            finally
            {
                _serverIsListening = false;
            }
        }

        public void Start()
        {
            LogMsg("Server.StartServer() is called");

            if (_httpListener.IsListening)
            {
                LogMsg("Server is already listening");
                return;
            }

            _httpListener.Start();
        }

        public void Restart()
        {
            LogMsg("Server.RestartServer() is called");

            if (_httpListener.IsListening) Stop();

            _httpListener = new HttpListener();

            if (!_httpListener.Prefixes.Any())
            {
                AddCurrentPrefix();
            }

            LogMsg("Server has been recreated");

            _httpListener.TimeoutManager.IdleConnection = TimeSpan.FromSeconds(5);
            _httpListener.TimeoutManager.EntityBody = TimeSpan.FromSeconds(5);

            Start();

            _serverIsListening = false;
        }

        public void Stop()
        {
            LogMsg("Server.KillServer() is called, killing the server");

            //if (_httpListener.Prefixes.Any()) _httpListener.Prefixes.Clear();
            _httpListener.Abort();
        }

        private void AddCurrentPrefix()
        {
            string url = GetServerUrl();
            _httpListener.Prefixes.Add(url);
        }

        private string GetServerUrl()
        {
            return $"http://{_followerSettings.LeaderModeSettings.LeaderModeNetworkSettings.ServerHostname.Value}:{_followerSettings.LeaderModeSettings.LeaderModeNetworkSettings.ServerPort.Value}/";
        }

        private void LogMsg(string msg)
        {
            if (_followerSettings.Debug.Value && _followerSettings.VerboseDebug.Value)
            {
                DebugWindow.LogMsg(msg, 1);
            }
        }
    }
}
