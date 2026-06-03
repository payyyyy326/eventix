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
            public const string DELETED = "DELETED";
        }

        public static class AppPaths
        {
            public const string AVATAR_UPLOADS = "uploads/avatars";
        }
        public static class SeatImportColumns
        {
            public const int Section = 0;
            public const int StartRow = 1;
            public const int EndRow = 2;
            public const int StartNumber = 3;
            public const int EndNumber = 4;
            public const int StartX = 5;
            public const int StartY = 6;
            public const int GapX = 7;
            public const int GapY = 7;
        }

        public static class SeatStatus
        {
            public const string AVAILABLE = "Available";
        }
    }
}