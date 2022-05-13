using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Awaken.Scripts.Dividends.Helpers;

public class HttpClientHelper
{
    public static string GetResponse(string url, out string statusCode)
    {
        if (url.StartsWith("https"))
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
        }

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        HttpResponseMessage response = httpClient.GetAsync(url).Result;
        statusCode = response.StatusCode.ToString();
        return response.IsSuccessStatusCode ? response.Content.ReadAsStringAsync().Result : null;
    }

    public static T GetResponse<T>(string url) where T : class, new()
    {
        if (url.StartsWith("https"))
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
        }

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var response = httpClient.GetAsync(url).Result;
        var result = default(T);
        if (response.IsSuccessStatusCode)
        {
            Task<string> t = response.Content.ReadAsStringAsync();
            string s = t.Result;
            result = JsonConvert.DeserializeObject<T>(s);
        }

        return result;
    }

    public static string PostResponse(string url, string postData, out string statusCode)
    {
        if (url.StartsWith("https"))
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
        }

        var httpContent = new StringContent(postData);
        httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        httpContent.Headers.ContentType.CharSet = "utf-8";
        var httpClient = new HttpClient();
        var response = httpClient.PostAsync(url, httpContent).Result;
        statusCode = response.StatusCode.ToString();
        if (response.IsSuccessStatusCode)
        {
            return response.Content.ReadAsStringAsync().Result;
        }

        return null;
    }

    public static T PostResponse<T>(string url, string postData) where T : class, new()
    {
        if (url.StartsWith("https"))
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
        }

        var httpContent = new StringContent(postData);
        httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var httpClient = new HttpClient();

        T result = default(T);

        var response = httpClient.PostAsync(url, httpContent).Result;
        if (response.IsSuccessStatusCode)
        {
            var t = response.Content.ReadAsStringAsync();
            var s = t.Result;
            result = JsonConvert.DeserializeObject<T>(s);
        }

        return result;
    }

    public static string PostResponse(string url, string postData, string token, string appId, string serviceURL,
        out string statusCode)
    {
        if (url.StartsWith("https"))
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
        }

        var httpContent = new StringContent(postData);
        httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        httpContent.Headers.ContentType.CharSet = "utf-8";

        // custom
        httpContent.Headers.Add("token", token);
        httpContent.Headers.Add("appId", appId);
        httpContent.Headers.Add("sericeUrl", serviceURL);

        var httpClient = new HttpClient();

        var response = httpClient.PostAsync(url, httpContent).Result;
        statusCode = response.StatusCode.ToString();
        if (response.IsSuccessStatusCode)
        {
            return response.Content.ReadAsStringAsync().Result;
        }

        return null;
    }

    public static string PatchResponse(string url, string postData)
    {
        var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
        httpWebRequest.ContentType = "application/x-www-form-urlencoded";
        httpWebRequest.Method = "PATCH";

        var btBodies = Encoding.UTF8.GetBytes(postData);
        httpWebRequest.ContentLength = btBodies.Length;
        httpWebRequest.GetRequestStream().Write(btBodies, 0, btBodies.Length);

        HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
        var streamReader = new StreamReader(httpWebResponse.GetResponseStream());
        string responseContent = streamReader.ReadToEnd();

        httpWebResponse.Close();
        streamReader.Close();
        httpWebRequest.Abort();
        httpWebResponse.Close();

        return responseContent;
    }

    public static string AddResponse(string url, string postData)
    {
        if (url.StartsWith("https"))
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
        }

        HttpContent httpContent = new StringContent(postData);
        httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded")
            { CharSet = "utf-8" };
        var httpClient = new HttpClient();
        HttpResponseMessage response = httpClient.PostAsync(url, httpContent).Result;
        if (response.IsSuccessStatusCode)
        {
            string result = response.Content.ReadAsStringAsync().Result;
            return result;
        }

        return null;
    }

    public static bool DeleteResponse(string url)
    {
        if (url.StartsWith("https"))
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
        }

        var httpClient = new HttpClient();
        HttpResponseMessage response = httpClient.DeleteAsync(url).Result;
        return response.IsSuccessStatusCode;
    }

    public static string PutResponse(string url, string postData)
    {
        if (url.StartsWith("https"))
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
        }

        HttpContent httpContent = new StringContent(postData);
        httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded")
            { CharSet = "utf-8" };

        var httpClient = new HttpClient();
        HttpResponseMessage response = httpClient.PutAsync(url, httpContent).Result;
        if (response.IsSuccessStatusCode)
        {
            string result = response.Content.ReadAsStringAsync().Result;
            return result;
        }

        return null;
    }

    public static string SearchResponse(string url)
    {
        if (url.StartsWith("https"))
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
        }

        var httpClient = new HttpClient();
        var response = httpClient.GetAsync(url).Result;
        if (response.IsSuccessStatusCode)
        {
            var result = response.Content.ReadAsStringAsync().Result;
            return result;
        }

        return null;
    }
}