﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Exceptions.API;
using Newtonsoft.Json.Serialization;

namespace TwitchLib.Internal
{
    internal class Requests
    {
        public enum API
        {
            v3, v4, v5
        }

        #region POST
        public static T Post<T>(string url, Models.API.RequestModel model, API api = API.v5)
        {
            var test = new JsonSerializerSettings();
            if (model != null)
                return JsonConvert.DeserializeObject<T>(Post(url, LowercaseJsonSerializer.SerializeObject(model), api));
            else
                return JsonConvert.DeserializeObject<T>(Post(url, "", api));
        }

        public static void Post(string url, Models.API.RequestModel model, API api = API.v5)
        {
            Post(url, LowercaseJsonSerializer.SerializeObject(model), api);
        }

        public static string Post(string url, string payload, API api = API.v5)
        {
            checkForCredentials();
            url = appendClientId(url);

            var request = WebRequest.CreateHttp(url);
            request.Method = "POST";
            request.ContentType = "application/json";

            if (!string.IsNullOrEmpty(TwitchAPI.Shared.AccessToken))
                request.Headers["Authorization"] = $"OAuth {TwitchAPI.Shared.AccessToken}";
            request.Accept = $"application/vnd.twitchtv.v{getVersion(api)}+json";

            using (var writer = new StreamWriter(request.GetRequestStream()))
                writer.Write(payload);

            try
            {
                var response = request.GetResponse();
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    string data = reader.ReadToEnd();
                    return data;
                }
            }
            catch (WebException ex) { handleWebException(ex); }

            return null;
        }
        #endregion

        #region GET
        public static string Get(string url, API api = API.v5)
        {
            checkForCredentials();
            url = appendClientId(url);

            var request = WebRequest.CreateHttp(url);
            request.Method = "GET";
            request.ContentType = "application/json";
            request.Accept = $"application/vnd.twitchtv.v{getVersion(api)}+json";

            if (!string.IsNullOrEmpty(TwitchAPI.Shared.AccessToken))
                request.Headers["Authorization"] = $"OAuth {TwitchAPI.Shared.AccessToken}";

            try
            {
                var response = request.GetResponse();
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    string data = reader.ReadToEnd();
                    return data;
                }

            }
            catch (WebException ex) { handleWebException(ex); }

            return null;
        }

        public static T Get<T>(string url, API api = API.v5)
        {
            return JsonConvert.DeserializeObject<T>(Get(url, api));
        }
        #endregion

        #region DELETE
        public static string Delete(string url, API api = API.v5)
        {
            return genericRequest(url, "DELETE", null, api);
        }

        public static T Delete<T>(string url, API api = API.v5)
        {
            return JsonConvert.DeserializeObject<T>(genericRequest(url, "DELETE", null, api));
        }
        #endregion

        #region PUT
        public static T Put<T>(string url, string payload, API api = API.v5)
        {
            return JsonConvert.DeserializeObject<T>(genericRequest(url, "PUT", payload, api));
        }

        public static string Put(string url, string payload, API api = API.v5)
        {
            return genericRequest(url, "PUT", payload, api);
        }
        #endregion

        private static string genericRequest(string url, string method, string payload = null, API api = API.v5)
        {
            checkForCredentials();
            url = appendClientId(url);

            var request = WebRequest.CreateHttp(url);
            request.Method = method;
            request.ContentType = "application/json";
            request.Accept = $"application/vnd.twitchtv.v{getVersion(api)}+json";

            if (!string.IsNullOrEmpty(TwitchAPI.Shared.AccessToken))
                request.Headers["Authorization"] = $"OAuth {TwitchAPI.Shared.AccessToken}";

            if(payload != null)
                using (var writer = new StreamWriter(request.GetRequestStream()))
                    writer.Write(payload);

            try
            {
                var response = request.GetResponse();
                using (var reader = new StreamReader(response.GetResponseStream()))
                    return reader.ReadToEnd();
            }
            catch (WebException ex) { handleWebException(ex); }

            return null;
        }

        private static int getVersion(API api)
        {
            switch (api)
            {
                case API.v3:
                    return 3;
                case API.v4:
                    return 4;
                case API.v5:
                default:
                    return 5;
            }
        }

        private static string appendClientId(string url)
        {
            return url.Contains("?")
                ? $"{url}&client_id={TwitchAPI.Shared.ClientId}"
                : $"{url}?client_id={TwitchAPI.Shared.ClientId}";
        }

        private static void checkForCredentials()
        {
            if (string.IsNullOrEmpty(TwitchAPI.Shared.ClientId) && string.IsNullOrWhiteSpace(TwitchAPI.Shared.AccessToken))
                throw new InvalidCredentialException("All API calls require Client-Id or OAuth token. Set Client-Id by using SetClientId(\"client_id_here\")");
        }

        private static void handleWebException(WebException e)
        {
            HttpWebResponse errorResp = e.Response as HttpWebResponse;
            switch (errorResp.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    throw new BadScopeException("Your request was blocked due to bad credentials (do you have the right scope for your access token?).");
                case HttpStatusCode.NotFound:
                    throw new BadResourceException("The resource you tried to access was not valid.");
                case (HttpStatusCode)422:
                    throw new NotPartneredException("The resource you requested is only available to channels that have been partnered by Twitch.");
                default:
                    throw e;
            }
        }

        // Contract resolver to force keys to lowercase
        // Credit: http://stackoverflow.com/questions/6288660/net-ensuring-json-keys-are-lowercase
        public class LowercaseJsonSerializer
        {
            private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
            {
                ContractResolver = new LowercaseContractResolver()
            };

            public static string SerializeObject(object o)
            {
                return JsonConvert.SerializeObject(o, Formatting.Indented, Settings);
            }

            public class LowercaseContractResolver : DefaultContractResolver
            {
                protected override string ResolvePropertyName(string propertyName)
                {
                    return propertyName.ToLower();
                }
            }
        }
    }
}
