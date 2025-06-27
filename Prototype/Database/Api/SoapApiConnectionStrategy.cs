using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Database.Api;

public class SoapApiConnectionStrategy(
    HttpClient httpClient,
    IPasswordEncryptionService encryptionService,
    ILogger<SoapApiConnectionStrategy> logger)
    : IApiConnectionStrategy
{
    public DataSourceTypeEnum ConnectionType => DataSourceTypeEnum.SoapApi;

    public Dictionary<AuthenticationTypeEnum, bool> GetSupportedAuthTypes()
    {
        return new Dictionary<AuthenticationTypeEnum, bool>
        {
            { AuthenticationTypeEnum.NoAuth, true },
            { AuthenticationTypeEnum.BasicAuth, true },
            { AuthenticationTypeEnum.BearerToken, true },
            { AuthenticationTypeEnum.ApiKey, true },
            { AuthenticationTypeEnum.Custom, true }
        };
    }

    public async Task<object> ExecuteRequestAsync(ConnectionSourceDto source)
    {
        var request = CreateSoapRequest(source);
        var response = await httpClient.SendAsync(request);
        
        var content = await response.Content.ReadAsStringAsync();
        
        return new
        {
            StatusCode = (int)response.StatusCode,
            Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value)),
            Content = content,
            IsSuccess = response.IsSuccessStatusCode,
            SoapFault = ExtractSoapFault(content)
        };
    }

    public async Task<object> ExecuteRequestAsync(ApplicationConnectionModel source)
    {
        var dto = MapToDto(source);
        return await ExecuteRequestAsync(dto);
    }

    public async Task<bool> TestConnectionAsync(ConnectionSourceDto source)
    {
        try
        {
            // Try to get WSDL first
            var wsdlUri = GetWsdlUri(source);
            var wsdlRequest = new HttpRequestMessage(HttpMethod.Get, wsdlUri);
            AddAuthentication(wsdlRequest, source);

            var wsdlResponse = await httpClient.SendAsync(wsdlRequest);
            
            if (wsdlResponse.IsSuccessStatusCode)
            {
                var wsdlContent = await wsdlResponse.Content.ReadAsStringAsync();
                return IsValidWsdl(wsdlContent);
            }

            // If WSDL not available, try a basic SOAP request
            var testSoapEnvelope = CreateTestSoapEnvelope();
            var testSource = new ConnectionSourceDto
            {
                Host = source.Host,
                Port = source.Port,
                Url = source.Url,
                ApiEndpoint = source.ApiEndpoint,
                AuthenticationType = source.AuthenticationType,
                Username = source.Username,
                Password = source.Password,
                ApiKey = source.ApiKey,
                BearerToken = source.BearerToken,
                Headers = source.Headers,
                RequestBody = testSoapEnvelope,
                HttpMethod = "POST"
            };

            var request = CreateSoapRequest(testSource);
            var response = await httpClient.SendAsync(request);
            
            // SOAP endpoints might return 500 with valid SOAP fault, which is still a valid response
            return response.IsSuccessStatusCode || 
                   response.StatusCode == System.Net.HttpStatusCode.InternalServerError && 
                   IsValidSoapResponse(await response.Content.ReadAsStringAsync());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SOAP API connection test failed for {Endpoint}", source.ApiEndpoint ?? source.Url);
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
        return "SOAP API connection with WSDL support and various authentication methods";
    }

    private HttpRequestMessage CreateSoapRequest(ConnectionSourceDto source)
    {
        var uri = !string.IsNullOrEmpty(source.ApiEndpoint) 
            ? source.ApiEndpoint 
            : $"{source.Url}";

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(uri),
            Method = HttpMethod.Post
        };

        // Add authentication
        AddAuthentication(request, source);

        // Add SOAP-specific headers
        request.Headers.Add("SOAPAction", "\"\""); // Default empty SOAPAction
        
        // Add custom headers (might override SOAPAction)
        if (!string.IsNullOrEmpty(source.Headers))
        {
            try
            {
                var headers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(source.Headers);
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (header.Key.Equals("SOAPAction", StringComparison.OrdinalIgnoreCase))
                        {
                            request.Headers.Remove("SOAPAction");
                            request.Headers.Add("SOAPAction", header.Value);
                        }
                        else
                        {
                            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                logger.LogWarning(ex, "Failed to parse headers JSON: {Headers}", source.Headers);
            }
        }

        // Add SOAP envelope in request body
        if (!string.IsNullOrEmpty(source.RequestBody))
        {
            request.Content = new StringContent(source.RequestBody, Encoding.UTF8, "text/xml");
        }

        return request;
    }

    private void AddAuthentication(HttpRequestMessage request, ConnectionSourceDto source)
    {
        switch (source.AuthenticationType)
        {
            case AuthenticationTypeEnum.BasicAuth:
                if (!string.IsNullOrEmpty(source.Username) && !string.IsNullOrEmpty(source.Password))
                {
                    var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{source.Username}:{source.Password}"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                }
                break;

            case AuthenticationTypeEnum.BearerToken:
                if (!string.IsNullOrEmpty(source.BearerToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", source.BearerToken);
                }
                break;

            case AuthenticationTypeEnum.ApiKey:
                if (!string.IsNullOrEmpty(source.ApiKey))
                {
                    request.Headers.Add("X-API-Key", source.ApiKey);
                }
                break;

            case AuthenticationTypeEnum.NoAuth:
            default:
                // No authentication
                break;
        }
    }

    private string GetWsdlUri(ConnectionSourceDto source)
    {
        var baseUri = !string.IsNullOrEmpty(source.ApiEndpoint) 
            ? source.ApiEndpoint 
            : source.Url;

        // Common WSDL URL patterns
        if (baseUri.Contains("?wsdl", StringComparison.OrdinalIgnoreCase))
            return baseUri;
        
        if (baseUri.EndsWith("/"))
            return baseUri + "?wsdl";
        
        return baseUri + "?wsdl";
    }

    private bool IsValidWsdl(string content)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(content);
            
            // Check for WSDL namespace and definitions element
            return doc.DocumentElement?.NamespaceURI == "http://schemas.xmlsoap.org/wsdl/" ||
                   doc.DocumentElement?.LocalName == "definitions";
        }
        catch (XmlException)
        {
            return false;
        }
    }

    private bool IsValidSoapResponse(string content)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(content);
            
            // Check for SOAP envelope
            return doc.DocumentElement?.LocalName == "Envelope" &&
                   (doc.DocumentElement.NamespaceURI == "http://schemas.xmlsoap.org/soap/envelope/" ||
                    doc.DocumentElement.NamespaceURI == "http://www.w3.org/2003/05/soap-envelope");
        }
        catch (XmlException)
        {
            return false;
        }
    }

    private string CreateTestSoapEnvelope()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soap:Header/>
    <soap:Body>
        <TestMethod xmlns=""http://tempuri.org/""/>
    </soap:Body>
</soap:Envelope>";
    }

    private string? ExtractSoapFault(string content)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(content);
            
            var faultNode = doc.SelectSingleNode("//soap:Fault", CreateNamespaceManager(doc));
            return faultNode?.InnerXml;
        }
        catch (XmlException)
        {
            return null;
        }
    }

    private XmlNamespaceManager CreateNamespaceManager(XmlDocument doc)
    {
        var nsmgr = new XmlNamespaceManager(doc.NameTable);
        nsmgr.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
        nsmgr.AddNamespace("soap12", "http://www.w3.org/2003/05/soap-envelope");
        return nsmgr;
    }

    private ConnectionSourceDto MapToDto(ApplicationConnectionModel source)
    {
        return new ConnectionSourceDto
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
            ClientSecret = string.IsNullOrEmpty(source.ClientSecret) ? null : encryptionService.Decrypt(source.ClientSecret)
        };
    }
}