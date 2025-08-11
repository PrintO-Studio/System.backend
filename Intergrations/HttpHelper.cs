using Dumpify;
using System.Text;
using Zorro.Middlewares;

namespace PrintO.Intergrations;

public static class HttpHelper
{
    public static async Task<string> POST(this HttpClient client, string url, string jsonData)
    {
        var requestContent = new StringContent(jsonData, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, requestContent);
        string responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode is false)
        {
            throw new QueryException(url + ": " + responseContent, (int)response.StatusCode);
        }
        return responseContent;
    }

    public static async Task<string> GET(this HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        string responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode is false)
        {
            throw new QueryException(url + ": " + responseContent, (int)response.StatusCode);
        }
        return responseContent;
    }
}