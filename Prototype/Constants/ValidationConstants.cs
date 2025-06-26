namespace Prototype.Constants
{
    public static class ValidationConstants
    {
        public const int MinPasswordLength = 8;
        public const int MaxPasswordLength = 50;
        public const int MinUsernameLength = 3;
        public const int MaxUsernameLength = 50;
        public const int MaxEmailLength = 100;
        
        public static class RegexPatterns
        {
            public const string Email = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            public const string Username = @"^[a-zA-Z0-9_-]+$";
            public const string StrongPassword = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]";
            public const string PhoneNumber = @"^\+?[1-9]\d{1,14}$";
        }
        
        public static class ErrorMessages
        {
            public const string RequiredField = "{0} is required";
            public const string InvalidEmail = "Invalid email format";
            public const string InvalidUsername = "Username can only contain letters, numbers, underscores, and hyphens";
            public const string WeakPassword = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character";
            public const string PasswordTooShort = "Password must be at least 8 characters long";
            public const string PasswordTooLong = "Password must not exceed 50 characters";
            public const string UsernameTooShort = "Username must be at least 3 characters long";
            public const string UsernameTooLong = "Username must not exceed 50 characters";
        }
    }
}