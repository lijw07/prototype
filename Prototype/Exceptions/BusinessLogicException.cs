using System.Runtime.Serialization;

namespace Prototype.Exceptions
{
    /// <summary>
    /// Exception thrown when business logic validation fails
    /// </summary>
    [Serializable]
    public class BusinessLogicException : Exception
    {
        public string? ErrorCode { get; set; }
        public Dictionary<string, object>? ErrorData { get; set; }

        public BusinessLogicException() : base() { }

        public BusinessLogicException(string message) : base(message) { }

        public BusinessLogicException(string message, Exception innerException) 
            : base(message, innerException) { }

        public BusinessLogicException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        public BusinessLogicException(string message, string errorCode, Dictionary<string, object> errorData) 
            : base(message)
        {
            ErrorCode = errorCode;
            ErrorData = errorData;
        }

        protected BusinessLogicException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
            ErrorCode = info.GetString(nameof(ErrorCode));
            ErrorData = info.GetValue(nameof(ErrorData), typeof(Dictionary<string, object>)) as Dictionary<string, object>;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ErrorCode), ErrorCode);
            info.AddValue(nameof(ErrorData), ErrorData);
        }
    }
}