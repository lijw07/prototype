namespace Prototype.Enum;

public enum ApplicationActionTypeEnum
{
    ConnectionAttempt,
    ConnectionSuccess,
    ConnectionFailure,
    ConnectionChanged,
    ApplicationAdded,
    ApplicationRemoved,
    StatusChanged,
    HealthCheck,
    ResponseTimeMeasured,
    ErrorLogged,
    Other
}