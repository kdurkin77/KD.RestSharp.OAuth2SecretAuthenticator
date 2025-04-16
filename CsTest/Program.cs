using CsTest;
using KD.RestSharp.OAuth2SecretAuthenticator;
using RestSharp;

try
{
    var baseUrl = "";
    var authUri = new Uri("");
    var clientId = "";
    var clientSecret = "";
    var scope = "";
    var resource = "";

    var restClientOptions = new RestClientOptions(baseUrl)
    {
        Authenticator =
            new OAuth2SecretAuthenticator(authUri, clientId, clientSecret)
            .UseScope(scope)
            .ShouldUseRequestBody()
    };

    using var restClient = new RestClient(restClientOptions);
    var message = new Message() { MyMessage = "TEST" };

    var headers =
        new List<KeyValuePair<string, string>>()
        {
            new("timestamp", DateTime.UtcNow.ToString()),
        };

    var request =
        new RestRequest(resource, Method.Post)
            .AddJsonBody(message)
            .AddHeaders(headers);

    var response = await restClient.ExecuteAsync<Response>(request);
    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"Call failed: {response.StatusCode} - Content: {response.Content}");
    }
    else
    {
        Console.WriteLine($"{response?.Data?.Status}");
    }

    var response2 = await restClient.ExecuteAsync<Response>(request);
    if (!response2.IsSuccessStatusCode)
    {
        Console.WriteLine($"Call failed: {response2.StatusCode} - Content: {response2.Content}");
    }
    else
    {
        Console.WriteLine($"{response2?.Data?.Status}");
    }


}
catch (Exception ex)
{
    Console.WriteLine(ex);
}

namespace CsTest
{
    public class Message
    {
        public string? MyMessage { get; set; }
    }

    public class Response
    {
        public string? Status { get; set; }
    }
}
