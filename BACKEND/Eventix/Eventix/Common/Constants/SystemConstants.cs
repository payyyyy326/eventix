namespace Eventix.Common.Constants
{
    public static class SystemConstants
    {
        public static class RoleConstants
        {
            public const string ROLE = "Role";
            public const string ADMIN = "Admin";
            public const string USER = "User";
            public const string CUSTOMER = "Customer";
        }

        public static class PurposeEmail
        {
            public const string REGISTER = "Register";
            public const string RESET_PASSWORD = "ResetPassword";

        }
        public static class PolicyConstants
        {
            public const string AdminOnly = "AdminOnly";

            public const string OrganizerOnly = "OrganizerOnly";

            public const string CustomerOnly = "CustomerOnly";

            public const string AdminOrOrganizer = "AdminOrOrganizer";
        }
        public static class NotificationType
        {
            public const string REMINDER = "REMINDER";
            public const string SYSTEM = "SYSTEM";
            public const string AI = "AI";
        }

        public static class NotificationPriority
        {
            public const string LOW = "LOW";
            public const string NORMAL = "NORMAL";
            public const string HIGH = "HIGH";
        }
        public static class StatusAccount
        {
            public const string ACTIVE = "ACTIVE";
            public const string INACTIVE = "INACTIVE";
            public const string BANNED = "BANNED";
        }
    }
}