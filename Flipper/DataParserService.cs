namespace Flipper;

public class DataParserService
{
    public class TimeSeriesPair
    {
        public TimeseriesLatest SeriesLatest;
        public Timeseries10Min Series10Min;

        public TimeSeriesPair(TimeseriesLatest seriesLatest, Timeseries10Min series10Min)
        {
            SeriesLatest = seriesLatest;
            Series10Min = series10Min;
        }
    }
    public async Task<TimeSeriesPair> ParseDataResponse(DataFetchService.DataResponse response)
    {
        string jsonStringLatest = await response.ResponseLatest.Content.ReadAsStringAsync();
        string jsonString10Min = await response.Response10Min.Content.ReadAsStringAsync();

        if (string.IsNullOrEmpty(jsonStringLatest) || string.IsNullOrEmpty(jsonString10Min))
        {
            throw new Exception("JsonString was null!");
        }

        return new TimeSeriesPair(TimeseriesLatest.FromJson(jsonStringLatest), Timeseries10Min.FromJson(jsonString10Min));
    }
}