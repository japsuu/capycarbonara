using Newtonsoft.Json;

namespace Flipper;

#pragma warning disable CS8618
public class PriceMapping1H
{
    [JsonProperty("data")]
    public Dictionary<string, JsonPriceObject1H> Prices { get; set; }

    [JsonProperty("timestamp")]
    public long Timestamp { get; set; }
}