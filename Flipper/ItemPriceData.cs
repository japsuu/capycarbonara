namespace Flipper;

public class ItemPriceData
{
    private Queue<TimeseriesLatest.TimeserieEntryLatest> priceHistory;

    public ItemPriceData()
    {
        priceHistory = new Queue<TimeseriesLatest.TimeserieEntryLatest>(5);
    }
}