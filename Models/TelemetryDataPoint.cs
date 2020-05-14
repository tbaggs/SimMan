using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimManager.Models
{
    public class TelemetryDataPoint
    {
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("grid_x")]
        public int Grid_x { get; set; }

        [JsonProperty("grid_y")]
        public int Grid_y { get; set; }

        [JsonProperty("inZone")]
        public bool InZone { get; set; }

        [JsonProperty("operation")]
        public string Operation { get; set; }
    }

    public class ZoneDataPoint : TelemetryDataPoint
    {
        [JsonProperty("zoneId")]
        public Guid? ZoneId { get; set; }

        [JsonProperty("zoneName")]
        public string ZoneName { get; set; }

        [JsonProperty("zoneGrid_x")]
        public int ZoneGrid_x { get; set; }

        [JsonProperty("zoneGgrid_y")]
        public int ZoneGrid_y { get; set; }
    }
}
