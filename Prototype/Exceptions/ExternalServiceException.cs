using System.Runtime.Serialization;

namespace Prototype.Exceptions
{
    /// <summary>
    /// Exception thrown when an external service call fails
    /// </summary>
    [Serializable]
    public class ExternalServiceException : Exception
    {
        public string? ServiceName { get; set; }
        public string? Endpoint { get; set; }
        public int? StatusCode { get; set; }
        public string? ResponseBody { get; set; }

        public ExternalServiceException() : base("External service call failed") { }

        public ExternalServiceException(string message) : base(message) { }

        public ExternalServiceException(string message, Exception innerException) 
            : base(message, innerException) { }

        public ExternalServiceException(string serviceName, string endpoint, string message) 
            : base($"Call to {serviceName} failed: {message}")
        {
            ServiceName = serviceName;
            Endpoint = endpoint;
        }

        public ExternalServiceException(string serviceName, string endpoint, int statusCode, string responseBody) 
            : base($"Call to {serviceName} failed with status {statusCode}")
        {
            ServiceName = serviceName;
            Endpoint = endpoint;
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }

        protected ExternalServiceException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
            ServiceName = info.GetString(nameof(ServiceName));
            Endpoint = info.GetString(nameof(Endpoint));
            StatusCode = info.GetInt32(nameof(StatusCode));
            ResponseBody = info.GetString(nameof(ResponseBody));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ServiceName), ServiceName);
            info.AddValue(nameof(Endpoint), Endpoint);
            info.AddValue(nameof(StatusCode), StatusCode);
            info.AddValue(nameof(ResponseBody), ResponseBody);
        }
    }
}