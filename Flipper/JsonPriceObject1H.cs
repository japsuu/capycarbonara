using Newtonsoft.Json;

namespace Flipper;

#pragma warning disable CS8618
public class JsonPriceObject1H
{
    [JsonProperty("avgHighPrice")]
    public long? AvgHighPrice { get; set; }

    [JsonProperty("highPriceVolume")]
    public long HighPriceVolume { get; set; }

    [JsonProperty("avgLowPrice")]
    public long? AvgLowPrice { get; set; }

    [JsonProperty("lowPriceVolume")]
    public long LowPriceVolume { get; set; }
        
    public PriceObject1H? ToPriceObject(long timestamp)
    {
        if (AvgHighPrice == null || AvgLowPrice == null)
            return null;

        PriceObject1H priceObject1HPrices = new(
            (long)AvgHighPrice,
            HighPriceVolume,
            (long)AvgLowPrice,
            LowPriceVolume,
            timestamp);

        return priceObject1HPrices;
    }
}