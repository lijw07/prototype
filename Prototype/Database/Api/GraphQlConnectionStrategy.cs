using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;

namespace Prototype.Database.Api;

public class GraphQlConnectionStrategy(
    HttpClient httpClient,
    PasswordEncryptionService encryptionService,
    ILogger<GraphQlConnectionStrategy> logger)
    : IApiConnectionStrategy
{
    public DataSourceTypeEnum ConnectionType => DataSourceTypeEnum.GraphQL;

    public Dictionary<AuthenticationTypeEnum, bool> GetSupportedAuthTypes()
    {
        return new Dictionary<AuthenticationTypeEnum, bool>
        {
            { AuthenticationTypeEnum.NoAuth, true },
            { AuthenticationTypeEnum.ApiKey, true },
            { AuthenticationTypeEnum.BearerToken, true },
            { AuthenticationTypeEnum.BasicAuth, true },
            { AuthenticationTypeEnum.OAuth2, true },
            { AuthenticationTypeEnum.JwtToken, true },
            { AuthenticationTypeEnum.Custom, true }
        };
    }

    public async Task<object> ExecuteRequestAsync(ConnectionSourceRequestDto sourceRequest)
    {
        var request = CreateGraphQLRequest(sourceRequest);
        var response = await httpClient.SendAsync(request);
        
        var content = await response.Content.ReadAsStringAsync();
        
        return new
        {
            StatusCode = (int)response.StatusCode,
            Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value)),
            Content = content,
            IsSuccess = response.IsSuccessStatusCode,
            Data = ParseGraphQLResponse(content)
        };
    }

    public async Task<object> ExecuteRequestAsync(ApplicationConnectionModel source)
    {
        var dto = MapToDto(source);
        return await ExecuteRequestAsync(dto);
    }

    public async Task<bool> TestConnectionAsync(ConnectionSourceRequestDto sourceRequest)
    {
        try
        {
            // Use introspection query to test GraphQL endpoint
            var introspectionQuery = new
            {
                query = @"
                query IntrospectionQuery {
                    __schema {
                        queryType { name }
                    }
                }"
            };

            var testSource = new ConnectionSourceRequestDto
            {
                Host = sourceRequest.Host,
                Port = sourceRequest.Port,
                Url = sourceRequest.Url,
                ApiEndpoint = sourceRequest.ApiEndpoint,
                AuthenticationType = sourceRequest.AuthenticationType,
                Username = sourceRequest.Username,
                Password = sourceRequest.Password,
                ApiKey = sourceRequest.ApiKey,
                BearerToken = sourceRequest.BearerToken,
                Headers = sourceRequest.Headers,
                RequestBody = JsonSerializer.Serialize(introspectionQuery),
                HttpMethod = "POST"
            };

            var request = CreateGraphQLRequest(testSource);
            var response = await httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
                return false;

            var content = await response.Content.ReadAsStringAsync();
            var result = ParseGraphQLResponse(content);
            
            // Check if it's a valid GraphQL response
            return result?.ContainsKey("data") == true || result?.ContainsKey("errors") == true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GraphQL connection test failed for {Endpoint}", sourceRequest.ApiEndpoint ?? sourceRequest.Url);
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync(ApplicationConnectionModel source)
    {
        var dto = MapToDto(source);
        return await TestConnectionAsync(dto);
    }

    public string GetConnectionDescription()
    {
        return "GraphQL API connection with introspection support and various authentication methods";
    }

    private HttpRequestMessage CreateGraphQLRequest(ConnectionSourceRequestDto sourceRequest)
    {
        var uri = !string.IsNullOrEmpty(sourceRequest.ApiEndpoint) 
            ? sourceRequest.ApiEndpoint 
            : $"{sourceRequest.Url}";

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(uri),
            Method = HttpMethod.Post
        };

        // Add authentication
        AddAuthentication(request, sourceRequest);

        // Add standard GraphQL headers
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add custom headers
        if (!string.IsNullOrEmpty(sourceRequest.Headers))
        {
            try
            {
                var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(sourceRequest.Headers);
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Failed to parse headers JSON: {Headers}", sourceRequest.Headers);
            }
        }

        // Add GraphQL query/mutation in request body
        if (!string.IsNullOrEmpty(sourceRequest.RequestBody))
        {
            request.Content = new StringContent(sourceRequest.RequestBody, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private void AddAuthentication(HttpRequestMessage request, ConnectionSourceRequestDto sourceRequest)
    {
        switch (sourceRequest.AuthenticationType)
        {
            case AuthenticationTypeEnum.ApiKey:
                if (!string.IsNullOrEmpty(sourceRequest.ApiKey))
                {
                    request.Headers.Add("X-API-Key", sourceRequest.ApiKey);
                }
                break;

            case AuthenticationTypeEnum.BearerToken:
            case AuthenticationTypeEnum.JwtToken:
                if (!string.IsNullOrEmpty(sourceRequest.BearerToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sourceRequest.BearerToken);
                }
                break;

            case AuthenticationTypeEnum.BasicAuth:
                if (!string.IsNullOrEmpty(sourceRequest.Username) && !string.IsNullOrEmpty(sourceRequest.Password))
                {
                    var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{sourceRequest.Username}:{sourceRequest.Password}"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                }
                break;

            case AuthenticationTypeEnum.OAuth2:
                if (!string.IsNullOrEmpty(sourceRequest.BearerToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sourceRequest.BearerToken);
                }
                break;

            case AuthenticationTypeEnum.NoAuth:
            default:
                // No authentication
                break;
        }
    }

    private Dictionary<string, object>? ParseGraphQLResponse(string content)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(content);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private ConnectionSourceRequestDto MapToDto(ApplicationConnectionModel source)
    {
        return new ConnectionSourceRequestDto
        {
            Host = source.Host,
            Port = source.Port,
            Url = source.Url,
            AuthenticationType = source.AuthenticationType,
            Username = source.Username,
            Password = string.IsNullOrEmpty(source.Password) ? null : encryptionService.Decrypt(source.Password),
            ApiEndpoint = source.ApiEndpoint,
            HttpMethod = source.HttpMethod,
            Headers = source.Headers,
            RequestBody = source.RequestBody,
            ApiKey = string.IsNullOrEmpty(source.ApiKey) ? null : encryptionService.Decrypt(source.ApiKey),
            BearerToken = string.IsNullOrEmpty(source.BearerToken) ? null : encryptionService.Decrypt(source.BearerToken),
            ClientId = source.ClientId,
            ClientSecret = string.IsNullOrEmpty(source.ClientSecret) ? null : encryptionService.Decrypt(source.ClientSecret),
            RefreshToken = string.IsNullOrEmpty(source.RefreshToken) ? null : encryptionService.Decrypt(source.RefreshToken),
            AuthorizationUrl = source.AuthorizationUrl,
            TokenUrl = source.TokenUrl,
            Scope = source.Scope
        };
    }
}