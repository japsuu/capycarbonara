namespace Flipper;

public class DataFetchService
{
    private const string RUNELITE_API_ADDRESS_LATEST = "https://prices.runescape.wiki/api/v1/osrs/latest";
    private const string RUNELITE_API_ADDRESS_10_MIN = "https://prices.runescape.wiki/api/v1/osrs/10m";
    
    private readonly HttpClient httpClient;

    public DataFetchService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<DataResponse> FetchData()
    {
        HttpResponseMessage responseLatest = await httpClient.GetAsync(RUNELITE_API_ADDRESS_LATEST);
        HttpResponseMessage response10Min = await httpClient.GetAsync(RUNELITE_API_ADDRESS_10_MIN);
        responseLatest.EnsureSuccessStatusCode();
        response10Min.EnsureSuccessStatusCode();

        DataResponse response = new(responseLatest, response10Min);

        return response;
    }

    public class DataResponse
    {
        public HttpResponseMessage ResponseLatest;
        public HttpResponseMessage Response10Min;

        public DataResponse(HttpResponseMessage responseLatest, HttpResponseMessage response10Min)
        {
            ResponseLatest = responseLatest;
            Response10Min = response10Min;
        }
    }
}