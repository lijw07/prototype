using System.Runtime.Serialization;

namespace Prototype.Exceptions
{
    /// <summary>
    /// Exception thrown when authentication fails
    /// </summary>
    [Serializable]
    public class AuthenticationException : Exception
    {
        public string? AuthenticationMethod { get; set; }
        public DateTime? AttemptTime { get; set; }
        public string? IpAddress { get; set; }

        public AuthenticationException() : base("Authentication failed") { }

        public AuthenticationException(string message) : base(message) { }

        public AuthenticationException(string message, Exception innerException) 
            : base(message, innerException) { }

        public AuthenticationException(string message, string authenticationMethod) 
            : base(message)
        {
            AuthenticationMethod = authenticationMethod;
            AttemptTime = DateTime.UtcNow;
        }

        public AuthenticationException(string message, string authenticationMethod, string ipAddress) 
            : base(message)
        {
            AuthenticationMethod = authenticationMethod;
            IpAddress = ipAddress;
            AttemptTime = DateTime.UtcNow;
        }

        protected AuthenticationException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
            AuthenticationMethod = info.GetString(nameof(AuthenticationMethod));
            AttemptTime = info.GetDateTime(nameof(AttemptTime));
            IpAddress = info.GetString(nameof(IpAddress));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(AuthenticationMethod), AuthenticationMethod);
            info.AddValue(nameof(AttemptTime), AttemptTime);
            info.AddValue(nameof(IpAddress), IpAddress);
        }
    }
}