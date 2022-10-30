namespace Flipper;

public class CalculatorService
{
    public List<Anomaly> GetAnomalies(DataParserService.TimeSeriesPair timeseriesPair)
    {
        List<Anomaly> anomalies = new();

        //if (timeseriesPair.SeriesLatest.Dataset.Count != timeseriesPair.Series10Min.Dataset.Count)
        //{
        //    throw new Exception($"Dataset sizes do not match: Latest:{timeseriesPair.SeriesLatest.Dataset.Count} 10Min:{timeseriesPair.Series10Min.Dataset.Count}!");
        //}

        for (int i = 0; i < timeseriesPair.SeriesLatest.Dataset.Values.Count; i++)
        {
            TimeseriesLatest.TimeserieEntryLatest entryLatest = timeseriesPair.SeriesLatest.Dataset.Values.ElementAt(i);
            if (!timeseriesPair.Series10Min.Dataset.TryGetValue(timeseriesPair.SeriesLatest.Dataset.Keys.ElementAt(i), out Timeseries10Min.TimeserieEntry10Min? entry10Min))
            {
                continue;
            }
            
            if(entry10Min == null) continue;
            if(entryLatest == null) continue;
            if(entry10Min.AvgHighPrice == 0 ||
               entry10Min.AvgLowPrice == 0 ||
               entryLatest.High == 0 ||
               entryLatest.Low == 0) continue;
            
            if(entryLatest.Low < 5000 || (entry10Min.LowPriceVolume > 20000 || entry10Min.HighPriceVolume > 20000)) continue;
            
            //BUG: Compare to potential profit (margin * limit)

            int itemId = int.Parse(timeseriesPair.SeriesLatest.Dataset.Keys.ElementAt(i));

            (double percentage, bool isDrop) = GetDeviationPercentage(entryLatest, entry10Min);

            if (percentage > 20)
            {
                anomalies.Add(isDrop
                    ? new Anomaly(Anomaly.Type.Drop, itemId)
                    : new Anomaly(Anomaly.Type.Spike, itemId));
            }
        }

        return anomalies;
    }

    private static (double percentage, bool isDrop) GetDeviationPercentage(TimeseriesLatest.TimeserieEntryLatest latestEntry, Timeseries10Min.TimeserieEntry10Min averageEntry)
    {
        bool drop = latestEntry.Low < averageEntry.AvgLowPrice;
        double avgVal = drop ? averageEntry.AvgLowPrice : averageEntry.AvgHighPrice;
        double curVal = drop ? latestEntry.Low : latestEntry.High;

        // double inversePercentage = -(1.0 - curVal / avgVal);
        // double percentage = 1.0 - inversePercentage;
        // double absolutePercentage = Math.Abs(percentage);

        return (Math.Round(Math.Abs((1.0 + (1.0 - curVal / avgVal) - 1.0) * 100.0)), drop);

        //return Math.Round(absolutePercentage);
    }

    

    public class Anomaly
    {
        public enum Type
        {
            Spike,
            Drop
        }
        public Type AnomalyType;

        public int ItemID;

        public Anomaly(Type anomalyType, int itemId)
        {
            AnomalyType = anomalyType;
            ItemID = itemId;
        }
    }
}