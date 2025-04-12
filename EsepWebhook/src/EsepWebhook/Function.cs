using System.Text;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

// for converting the incoming JSON payload into .NET objects
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        public async Task<string> FunctionHandler(object input, ILambdaContext context)
        {
            context.Logger.LogLine("Input: " + input?.ToString());

            dynamic payload = JsonConvert.DeserializeObject<dynamic>(input.ToString());
            
            if (payload?.issue?.html_url == null)
            {
                context.Logger.LogLine("Payload missing 'issue.html_url'.");
                return "Invalid payload";
            }
            
            string issueUrl = payload.issue.html_url;
            context.Logger.LogLine("Issue URL: " + issueUrl);

            string slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
            if (string.IsNullOrEmpty(slackUrl))
            {
                context.Logger.LogLine("SLACK_URL env variable is not set.");
                return "SLACK_URL is not set";
            }

            var slackPayload = new { text = $"Issue Created: {issueUrl}" };
            string jsonPayload = JsonConvert.SerializeObject(slackPayload);

            using (var client = new HttpClient())
            {
                HttpResponseMessage response = await client.PostAsync(slackUrl,
                    new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    context.Logger.LogLine("Slack msg sent successfully.");
                    return "Success";
                }
                else
                {
                    context.Logger.LogLine("Failed to send Slack msg with status: " + response.StatusCode);
                    return $"Failed: {response.StatusCode}";
                }
            }
        }
    }
}
