using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace FollowerV2
{
    internal class NetworkHelper
    {
        public static bool IsUrlAlive(string url, int timeoutMs)
        {
            try
            {
                var request = WebRequest.Create(url);
                request.Method = "HEAD";
                request.Timeout = timeoutMs;
                var response = (HttpWebResponse) request.GetResponse();
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
            }

            return false;
        }

        public static string GetNetworkResponse(string url, int timeoutMs)
        {
            var returnVal = "";

            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = timeoutMs;
            request.ReadWriteTimeout = timeoutMs;
            
            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                var stream = response.GetResponseStream();
                var reader = new StreamReader(stream);
                returnVal = reader.ReadToEnd();
            }
            catch {}

            return returnVal;
        }
    }
}