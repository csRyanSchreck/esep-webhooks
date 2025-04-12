using System.Text;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

// for converting the incoming JSON payload into .NET objects
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    public async Task<string> FunctionHandler(object input, ILambdaContext context)
    {
        context.Logger.LogInformation($"FunctionHandler received: {input}");
        
        dynamic json = JsonConvert.DeserializeObject<dynamic>(input.ToString());
        
        if (json?.body != null)
        {
            json = JsonConvert.DeserializeObject<dynamic>(json.body.ToString());
        }
        
        string payload = $"{{'text':'Issue Created: {json.issue.html_url}'}}";
        
        string slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
        if (string.IsNullOrEmpty(slackUrl))
        {
            context.Logger.LogInformation("SLACK_URL env variable is not set.");
            return JsonConvert.SerializeObject(new
            {
                statusCode = 500,
                headers = new { "Content-Type" = "application/json" },
                body = "SLACK_URL not set"
            });
        }
        
        var client = new HttpClient();
        var webRequest = new HttpRequestMessage(HttpMethod.Post, slackUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        
        HttpResponseMessage response = await client.SendAsync(webRequest);
        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());
        string slackResponse = await reader.ReadToEndAsync();
        
        var proxyResponse = new
        {
            statusCode = 200,
            headers = new { "Content-Type" = "application/json" },
            body = slackResponse
        };
        
        return JsonConvert.SerializeObject(proxyResponse);
    }
}
