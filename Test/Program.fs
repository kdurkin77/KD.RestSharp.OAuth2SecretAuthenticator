open System
open System.Collections.Generic

open KD.RestSharp.OAuth2SecretAuthenticator
open RestSharp


type Message = {
    MyMessage: string
    }

type Response = {
    Status: string
    }


[<EntryPoint>]
let main _ =
    async {
    try
        let baseUrl = ""
        let authUri = new Uri ""
        let clientId = ""
        let clientSecret = ""
        let scope = ""
        let resource = ""

        let restClientOptions = new RestClientOptions(
            baseUrl,
            Authenticator =
                OAuth2SecretAuthenticator(authUri, clientId, clientSecret)
                .UseScope(scope)
                .ShouldUseRequestBody())

        use restClient = new RestClient(restClientOptions)

        let message = {
            MyMessage = "TEST"
            }

        let headers =
            [
                new KeyValuePair<string, string>("timestamp", DateTime.UtcNow.ToString())
            ] |> ResizeArray

        let request =
            RestRequest(resource, Method.Post)
                .AddJsonBody(message)
                .AddHeaders(headers)

        let! response = restClient.ExecuteAsync<Response>(request) |> Async.AwaitTask
        if not response.IsSuccessStatusCode then
            Console.WriteLine($"Call failed: {response.StatusCode} - Content: {response.Content}")
        else
            Console.WriteLine($"{response.Data}")

        let! response2 = restClient.ExecuteAsync<Response>(request) |> Async.AwaitTask
        if not response2.IsSuccessStatusCode then
            Console.WriteLine($"Call failed: {response2.StatusCode} - Content: {response2.Content}")
        else
            Console.WriteLine($"{response2.Data}")

        return 0
    with ex ->

        Console.WriteLine ex
        return Unchecked.defaultof<_>

    } |> Async.RunSynchronously
