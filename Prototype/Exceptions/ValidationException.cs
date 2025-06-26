using System.Runtime.Serialization;

namespace Prototype.Exceptions
{
    /// <summary>
    /// Exception thrown when validation fails
    /// </summary>
    [Serializable]
    public class ValidationException : Exception
    {
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public Dictionary<string, List<string>>? FieldErrors { get; set; }

        public ValidationException() : base() { }

        public ValidationException(string message) : base(message) { }

        public ValidationException(string message, Exception innerException) 
            : base(message, innerException) { }

        public ValidationException(List<string> errors) 
            : base("Validation failed")
        {
            ValidationErrors = errors ?? new List<string>();
        }

        public ValidationException(string message, List<string> errors) 
            : base(message)
        {
            ValidationErrors = errors ?? new List<string>();
        }

        public ValidationException(Dictionary<string, List<string>> fieldErrors) 
            : base("Validation failed")
        {
            FieldErrors = fieldErrors;
            ValidationErrors = fieldErrors?.SelectMany(x => x.Value).ToList() ?? new List<string>();
        }

        protected ValidationException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
            ValidationErrors = info.GetValue(nameof(ValidationErrors), typeof(List<string>)) as List<string> ?? new List<string>();
            FieldErrors = info.GetValue(nameof(FieldErrors), typeof(Dictionary<string, List<string>>)) as Dictionary<string, List<string>>;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ValidationErrors), ValidationErrors);
            info.AddValue(nameof(FieldErrors), FieldErrors);
        }
    }
}