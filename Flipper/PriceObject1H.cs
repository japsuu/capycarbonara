namespace Flipper;

#pragma warning disable CS8618
public class PriceObject1H
{
    public long Timestamp;
        
    public long AvgHighPrice;

    public long HighPriceVolume;

    public long AvgLowPrice;

    public long LowPriceVolume;

    public long TotalVolume => HighPriceVolume + LowPriceVolume;

    public PriceObject1H(long avgHighPrice, long highPriceVolume, long avgLowPrice, long lowPriceVolume, long timestamp)
    {
        Timestamp = timestamp;
        AvgHighPrice = avgHighPrice;
        HighPriceVolume = highPriceVolume;
        AvgLowPrice = avgLowPrice;
        LowPriceVolume = lowPriceVolume;
    }
}