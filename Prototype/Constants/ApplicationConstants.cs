namespace Prototype.Constants
{
    public static class ApplicationConstants
    {
        public const string AdminUsername = "admin";
        public const string DefaultDeviceInfo = "Unknown";
        public const string DefaultIpAddress = "127.0.0.1";
        
        public static class Claims
        {
            public const string UserId = "userId";
            public const string Username = "username";
            public const string Email = "email";
        }
        
        public static class Roles
        {
            public const string Admin = "Admin";
            public const string User = "User";
            public const string SuperAdmin = "SuperAdmin";
        }
        
        public static class ErrorMessages
        {
            public const string UnauthorizedAccess = "Unauthorized access";
            public const string InvalidCredentials = "Invalid username or password";
            public const string UserNotFound = "User not found";
            public const string InvalidRequest = "Invalid request";
            public const string ServerError = "An error occurred while processing your request";
        }
        
        public static class SuccessMessages
        {
            public const string LoginSuccess = "Login successful";
            public const string LogoutSuccess = "Logout successful";
            public const string PasswordResetSuccess = "Password reset successful";
            public const string EmailSentSuccess = "Email sent successfully";
            public const string OperationSuccess = "Operation completed successfully";
        }
        
        public static class Pagination
        {
            public const int DefaultPage = 1;
            public const int DefaultPageSize = 50;
            public const int MaxPageSize = 100;
            public const int MinPageSize = 1;
        }
    }
}