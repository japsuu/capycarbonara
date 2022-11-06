using Newtonsoft.Json;

namespace Flipper;

#pragma warning disable CS8618
public class PriceMapping
{
    [JsonProperty("data")]
    public Dictionary<string, JsonPriceObject?> Prices { get; set; }
}