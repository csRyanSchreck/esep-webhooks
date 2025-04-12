using System.Text;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

// for converting the incoming JSON payload into .NET objects
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        public async Task<object> FunctionHandler(object input, ILambdaContext context)
        {
            //debugging logging info
            context.Logger.LogInformation($"FunctionHandler received: {input?.ToString()}");
            //converts input to dynamic obj
            dynamic json = JsonConvert.DeserializeObject<dynamic>(input.ToString());
            //checks if new object has a body
            if (json?.body != null)
            {
                //put payload into json
                json = JsonConvert.DeserializeObject<dynamic>(json.body.ToString());
            }
            if (json?.issue?.html_url == null)
            {
                context.Logger.LogInformation("Payload missing 'issue.html_url'.");
                return new
                {
                    statusCode = 400,
                    headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
                    body = "Invalid payload"
                };
            }
            //gets GitHub issue URL from the payload.
            string issueUrl = json.issue.html_url;
            context.Logger.LogInformation("Issue URL: " + issueUrl);
            //reads slack url in from lambda config
            string slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
            //null check for slack url
            if (string.IsNullOrEmpty(slackUrl))
            {
                context.Logger.LogInformation("SLACK_URL env variable is not set.");
                return new
                {
                    statusCode = 500,
                    headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
                    body = "SLACK_URL is not set"
                };
            }
            string jsonPayload = JsonConvert.SerializeObject(new { text = $"Issue Created: {issueUrl}" });
            
            //send request to slack
            var client = new HttpClient();
            var webRequest = new HttpRequestMessage(HttpMethod.Post, slackUrl)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            HttpResponseMessage response = await client.SendAsync(webRequest);
            
            //read slack response
            using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());
            string slackResponse = await reader.ReadToEndAsync();
            var proxyResponse = new
            {
                statusCode = 200,
                headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
                body = slackResponse
            };
            return proxyResponse;
        }
    }
}