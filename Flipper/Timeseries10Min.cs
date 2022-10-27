#nullable disable
using System.ComponentModel;
using Newtonsoft.Json;

namespace Flipper;

public class Timeseries10Min
{
    /// <summary>
    /// Key = ID, Value = SerieEntry.
    /// </summary>
    [JsonProperty("data")]
    public Dictionary<string, TimeserieEntry10Min> Dataset { get; set; }

    public static Timeseries10Min FromJson(string json) => JsonConvert.DeserializeObject<Timeseries10Min>(json, Converter.Settings);
    
    public class TimeserieEntry10Min
    {
        [DefaultValue(0)]
        [JsonProperty("avgHighPrice", NullValueHandling = NullValueHandling.Ignore)]
        public long AvgHighPrice { get; set; }

        [DefaultValue(0)]
        [JsonProperty("highPriceVolume", NullValueHandling = NullValueHandling.Ignore)]
        public long HighPriceVolume { get; set; }

        [DefaultValue(0)]
        [JsonProperty("avgLowPrice", NullValueHandling = NullValueHandling.Ignore)]
        public long AvgLowPrice { get; set; }

        [DefaultValue(0)]
        [JsonProperty("lowPriceVolume", NullValueHandling = NullValueHandling.Ignore)]
        public long LowPriceVolume { get; set; }
    }
}