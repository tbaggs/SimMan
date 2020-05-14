using Newtonsoft.Json;
using System.Collections.Generic;

namespace SimManager.Models
{
    public class ConfigSettings
    {
        public ApplicationSettings ApplicationSettings { get; set; }
        public IoTHubSettings IoTHubSettings { get; set; }

    }

    public class ApplicationSettings
    {
        [JsonProperty("sims")]
        public int Sims { get; set; }

        [JsonProperty("vehicles")]
        public int Vehicles { get; set; }

        [JsonProperty("simulateButtonPushes")]
        public bool SimulateButtonPushes { get; set; }

        [JsonProperty("simulateOnlyButtonPushes")]
        public bool SimulateOnlyButtonPushes { get; set; }

        [JsonProperty("gridColumns")]
        public int GridColumns { get; set; }

        [JsonProperty("gridRows")]
        public int GridRows { get; set; }

        [JsonProperty("delay")]
        public int Delay { get; set; }

        [JsonProperty("movementsToSimulate")]
        public int MovementsToSimulate { get; set; }

        [JsonProperty("zoneSettings")]
        public List<ZoneSetting> Zones { get; set; }

    }

    public class IoTHubSettings
    {
        [JsonProperty("hostName")]
        public string HostName { get; set; }

        [JsonProperty("sharedAccessKey")]
        public string SharedAccessKey { get; set; }

        [JsonProperty("sharedAccessKeyName")]
        public string SharedAccessKeyName { get; set; }
    }

    public class ZoneSetting
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("gridBlocks")]
        public int GridBlocks { get; set; }
    }
}
