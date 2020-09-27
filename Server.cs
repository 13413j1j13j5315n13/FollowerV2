using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ExileCore;
using Newtonsoft.Json;

namespace FollowerV2
{
    class Server
    {
        private readonly FollowerV2Settings _followerSettings;
        private bool _serverIsListening = false;
        readonly HttpListener _httpListener = new HttpListener();

        public Server(FollowerV2Settings followerSettings)
        {
            _followerSettings = followerSettings;

            AddCurrentPrefix();
        }

        public void Listen()
        {
            LogMsg("Server.Listen called");

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
                IAsyncResult result = _httpListener.BeginGetContext(new AsyncCallback((IAsyncResult res) =>
                {
                    HttpListener list = (HttpListener)res.AsyncState;
                    HttpListenerContext context = list.EndGetContext(res);

                    HttpListenerRequest req = context.Request;
                    HttpListenerResponse response = context.Response;

                    string responseString = JsonConvert.SerializeObject(CreateNetworkActivityObject());

                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;

                    System.IO.Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();

                }), _httpListener);
            }
            finally
            {
                _serverIsListening = false;
            }
        }

        public void StartServer()
        {
            if (_httpListener.IsListening) return;

            if (!_httpListener.Prefixes.Any())
            {
                AddCurrentPrefix();
            }

            _httpListener.Start();
        }

        public void KillServer()
        {
            _httpListener.Close();
            _httpListener.Prefixes.Clear();
        }

        private void AddCurrentPrefix()
        {
            string url = GetServerUrl();
            _httpListener.Prefixes.Add(url);
        }

        private string GetServerUrl()
        {
            return $"http://localhost:{_followerSettings.LeaderModeSettings.ServerPort.Value}/";
        }

        private NetworkActivityObject CreateNetworkActivityObject()
        {
            return new NetworkActivityObject
            {
                FollowersShouldWork = _followerSettings.LeaderModeSettings.PropagateWorkingOfFollowers.Value,
                LeaderName = _followerSettings.LeaderModeSettings.LeaderNameToPropagate.Value,
                LeaderProximityRadius = _followerSettings.LeaderModeSettings.LeaderProximityRadiusToPropagate.Value
            };
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
