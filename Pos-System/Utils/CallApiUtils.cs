using Microsoft.AspNetCore.Http;
using MoreLinq;
using Newtonsoft.Json;
using Pos_System.API.Enums;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Pos_System.API.Utils
{
    public class CallApiUtils
    { 
        public static async Task<HttpResponseMessage> CallApiEndpoint(string url, object data)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(url, content);
            return response;
        }
        public static async Task<Object> GenerateObjectFromResponse(HttpResponseMessage response)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<Object>(responseString);
            return responseObject;
        }
    }
}
