using System.Runtime.Serialization;

namespace Prototype.Exceptions
{
    /// <summary>
    /// Exception thrown when authorization fails
    /// </summary>
    [Serializable]
    public class AuthorizationException : Exception
    {
        public string? RequestedResource { get; set; }
        public string? RequiredPermission { get; set; }
        public Guid? UserId { get; set; }

        public AuthorizationException() : base("Access denied") { }

        public AuthorizationException(string message) : base(message) { }

        public AuthorizationException(string message, Exception innerException) 
            : base(message, innerException) { }

        public AuthorizationException(string requestedResource, string requiredPermission) 
            : base($"Access denied to resource: {requestedResource}")
        {
            RequestedResource = requestedResource;
            RequiredPermission = requiredPermission;
        }

        public AuthorizationException(string requestedResource, string requiredPermission, Guid userId) 
            : base($"User {userId} does not have permission '{requiredPermission}' for resource: {requestedResource}")
        {
            RequestedResource = requestedResource;
            RequiredPermission = requiredPermission;
            UserId = userId;
        }

        protected AuthorizationException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
            RequestedResource = info.GetString(nameof(RequestedResource));
            RequiredPermission = info.GetString(nameof(RequiredPermission));
            var userIdString = info.GetString(nameof(UserId));
            if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var userId))
            {
                UserId = userId;
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(RequestedResource), RequestedResource);
            info.AddValue(nameof(RequiredPermission), RequiredPermission);
            info.AddValue(nameof(UserId), UserId?.ToString());
        }
    }
}