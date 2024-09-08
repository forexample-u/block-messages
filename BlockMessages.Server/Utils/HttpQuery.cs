namespace Server.Utils;

public static class HttpQuery
{
    private static readonly HttpClient _client = new HttpClient();

    public static async Task<string> GetAsync(string url)
    {
        try
        {
            return await _client.GetStringAsync(url);
        }
        catch
        {
            return null;
        }
    }
}
