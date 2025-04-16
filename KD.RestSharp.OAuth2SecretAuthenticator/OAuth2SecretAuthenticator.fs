namespace KD.RestSharp.OAuth2SecretAuthenticator

open System
open System.Text.Json.Serialization
open System.Threading
open System.Threading.Tasks

open RestSharp
open RestSharp.Authenticators


type TokenResponse() =
    [<JsonPropertyName("token_type")>]
    member val TokenType   : string = null with get, set
    [<JsonPropertyName("access_token")>]
    member val AccessToken : string = null with get, set
    [<JsonPropertyName("expires_in")>]
    member val ExpiresIn : Nullable<int64> = Nullable<int64>() with get, set


[<Sealed>]
type OAuth2SecretAuthenticator(authUri: Uri, clientId: string, clientSecret: string) =
    inherit AuthenticatorBase(String.Empty)

    do if String.IsNullOrWhiteSpace clientId     then invalidArg (nameof clientId)     "Client ID cannot be null or whitespace"
    do if String.IsNullOrWhiteSpace clientSecret then invalidArg (nameof clientSecret) "Client Secret cannot be null or whitespace"

    let mutable expiration': DateTimeOffset Option = None
    let sync = new SemaphoreSlim(1, 1)

    let tokenNeedsRefreshed token expiration' expirationLimit =
        if String.IsNullOrWhiteSpace token then
            true
        else
            match expiration' with
            | None            -> true
            | Some expiration -> DateTimeOffset.Now.Add(expirationLimit) >= expiration

    let getToken (authUri: Uri) (clientId: string) (clientSecret: string) (scope: string Option) (useRequestBody: bool) = task {
        use client =
            if useRequestBody then
                new RestClient(RestClientOptions(Authenticator = HttpBasicAuthenticator(clientId, clientSecret)))
            else
                new RestClient()

        let request =
            let baseRequest =
                RestRequest(authUri)
                    .AddParameter("grant_type", "client_credentials")
            match scope, useRequestBody with
            | Some scope, true ->
                baseRequest
                    .AddParameter("scope", scope)
                    .AddParameter("client_id", clientId)
                    .AddParameter("client_secret", clientSecret)
            | Some scope, false ->
                baseRequest
                    .AddParameter("scope", scope)
            | None, true ->
                baseRequest
                    .AddParameter("client_id", clientId)
                    .AddParameter("client_secret", clientSecret)
            | None, false ->
                baseRequest

        let! response = client.PostAsync<TokenResponse>(request)
        if response = Unchecked.defaultof<_> || String.IsNullOrWhiteSpace response.AccessToken then
            failwith "No token received"
            return Unchecked.defaultof<_>
        else if not response.ExpiresIn.HasValue then
            return $"{response.TokenType} {response.AccessToken}", None
        else
            let expiration' = Some (DateTimeOffset.Now.AddSeconds(float response.ExpiresIn.Value))
            return ($"{response.TokenType} {response.AccessToken}", expiration')
        }

    member private _.Token
        with get()      = base.Token
        and  set(value) = base.Token <- value

    member val private Scope          : string   Option = None                     with get, set
    member val private UseRequestBody : bool            = false                    with get, set
    member val private ExpirationLimit: TimeSpan        = TimeSpan.FromMinutes 30. with get, set

    member this.UseScope(scope) =
        if isNull scope then
            nullArg (nameof scope)
        else
            this.Scope <- Some scope
            this

    member this.ShouldUseRequestBody() =
        this.UseRequestBody <- true
        this

    member this.SetExpirationLimit(expirationLimit) =
        this.ExpirationLimit <- expirationLimit
        this

    override this.GetAuthenticationParameter(_) =
        task {
            let! token, expiration = task {
                if not (tokenNeedsRefreshed (this.Token) expiration' this.ExpirationLimit) then
                    return this.Token, expiration'
                else
                    do! sync.WaitAsync()
                    try
                        if not (tokenNeedsRefreshed (this.Token) expiration' this.ExpirationLimit) then
                            return this.Token, expiration'
                        else
                            return! getToken authUri clientId clientSecret this.Scope this.UseRequestBody
                    finally
                        sync.Release() |> ignore
                }

            this.Token  <- token
            expiration' <- expiration
            return HeaderParameter(KnownHeaders.Authorization, this.Token) :> Parameter
        } |> ValueTask<Parameter>
