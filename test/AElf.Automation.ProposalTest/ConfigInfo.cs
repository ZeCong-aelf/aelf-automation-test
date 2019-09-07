using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AElf.Automation.ProposalTest
{
    public class EnvironmentInfo
    {
        [JsonProperty("Environment")] public string Environment { get; set; }
        [JsonProperty("InitAccount")] public string InitAccount { get; set; }
        [JsonProperty("Url")] public string Url { get; set; }
        [JsonProperty("Password")] public string Password { get; set; }
    }

    public class ConfigInfo
    {
        [JsonProperty("TestEnvironment")] public string TestEnvironment { get; set; }
        [JsonProperty("EnvironmentInfo")] public List<EnvironmentInfo> EnvironmentInfos { get; set; }
        [JsonProperty("UserCount")] public int UserCount { get; set; }
    }

    public static class ConfigHelper
    {
        private static ConfigInfo _instance;
        private static string _jsonContent;
        private static readonly object LockObj = new object();

        public static ConfigInfo Config => GetConfigInfo();

        private static ConfigInfo GetConfigInfo()
        {
            lock (LockObj)
            {
                if (_instance != null) return _instance;

                var configFile = Path.Combine(Directory.GetCurrentDirectory(), "proposal-config.json");
                _jsonContent = File.ReadAllText(configFile);
                _instance = JsonConvert.DeserializeObject<ConfigInfo>(_jsonContent);
            }

            return _instance;
        }
    }
}