using Newtonsoft.Json;

namespace Flipper;

#pragma warning disable CS8618
public class JsonPriceObject
{
    [JsonProperty("high")]
    public long? High { get; set; }

    [JsonProperty("highTime")]
    public long? HighTime { get; set; }

    [JsonProperty("low")]
    public long? Low { get; set; }

    [JsonProperty("lowTime")]
    public long? LowTime { get; set; }

    public PriceObject? ToPriceObject()
    {
        if (High == null || HighTime == null || Low == null || LowTime == null)
            return null;

        PriceObject priceObjectPrices = new(
            (long)High,
            (long)HighTime,
            (long)Low,
            (long)LowTime);

        return priceObjectPrices;
    }
}