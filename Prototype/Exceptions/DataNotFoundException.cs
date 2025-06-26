using System.Runtime.Serialization;

namespace Prototype.Exceptions
{
    /// <summary>
    /// Exception thrown when requested data is not found
    /// </summary>
    [Serializable]
    public class DataNotFoundException : Exception
    {
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public Dictionary<string, object>? SearchCriteria { get; set; }

        public DataNotFoundException() : base("Requested data not found") { }

        public DataNotFoundException(string message) : base(message) { }

        public DataNotFoundException(string message, Exception innerException) 
            : base(message, innerException) { }

        public DataNotFoundException(string entityType, string entityId) 
            : base($"{entityType} with ID '{entityId}' not found")
        {
            EntityType = entityType;
            EntityId = entityId;
        }

        public DataNotFoundException(string entityType, Dictionary<string, object> searchCriteria) 
            : base($"{entityType} not found with specified criteria")
        {
            EntityType = entityType;
            SearchCriteria = searchCriteria;
        }

        protected DataNotFoundException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
            EntityType = info.GetString(nameof(EntityType));
            EntityId = info.GetString(nameof(EntityId));
            SearchCriteria = info.GetValue(nameof(SearchCriteria), typeof(Dictionary<string, object>)) as Dictionary<string, object>;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(EntityType), EntityType);
            info.AddValue(nameof(EntityId), EntityId);
            info.AddValue(nameof(SearchCriteria), SearchCriteria);
        }
    }
}