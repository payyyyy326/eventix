using Eventix.Share.Common.Models;

namespace Eventix.Common.Constants.SystemData
{
    /// <summary>
    /// Centralized system messages for consistent API responses
    /// </summary>
    public static class SystemSuccess
    {
        #region Success Messages (000-099)

        // General Success
        public static readonly SystemMessage SUCCESS = new("200", "Operation completed successfully", true);
        public static readonly SystemMessage EMPTY_RESPONSE = new("200", "No data", true);

        // Auth Success Messages (001-019)
        public static readonly SystemMessage LOGIN_SUCCESS = new("001", "Login successful", true);
        public static readonly SystemMessage REGISTER_SUCCESS = new("002", "User registered successfully", true);
        public static readonly SystemMessage LOGOUT_SUCCESS = new("003", "Logout successful", true);
        public static readonly SystemMessage NO_ACTIVE_SESSION = new("004", "No active session found", true);
        public static readonly SystemMessage INVALID_ROLE = new("005", "Invalid user role", true);
        public static readonly SystemMessage REGISTER_PENDING_VERIFY = new("006", "Verification OTP has been sent to your email.", true);
        public static readonly SystemMessage EMAIL_VERIFY_SUCCESS = new("007", "Email verified successfully. Registration completed.", true);
        public static readonly SystemMessage REFRESH_TOKEN_SUCCESS = new("008", "Refresh token successful.", true);
        public static readonly SystemMessage OTP_SEND_SUCCESS = new("009", "OTP has been sent successfully.", true);

        // User Success Messages (100-119)
        public static readonly SystemMessage USERS_RETRIEVED = new("101", "Users retrieved successfully", true);
        public static readonly SystemMessage USER_RETRIEVED = new("102", "User retrieved successfully", true);
        public static readonly SystemMessage USER_UPDATED = new("103", "User updated successfully", true);
        public static readonly SystemMessage USER_CHANGED_PASSWORD = new("104", "Change password successfully", true);
        public static readonly SystemMessage USER_CREATED = new("105", "User created successfully", true);
        public static readonly SystemMessage USERS_BY_ROLE_RETRIEVED = new("106", "Users by role retrieved successfully", true);
        public static readonly SystemMessage PASSWORD_RESET_SUCCESS = new("107", "Password reset successfully", true);

        // TaskItem Success Messages (040-059)
        public static readonly SystemMessage TICKET_TYPES_RETRIEVED = new("040", "TicketTypes retrieved successfully", true);
        public static readonly SystemMessage TICKET_TYPE_RETRIEVED = new("041", "TicketType retrieved successfully", true);
        public static readonly SystemMessage TICKET_TYPE_CREATED = new("042", "TicketType created successfully", true);
        public static readonly SystemMessage TICKET_TYPE_UPDATED = new("043", "TicketType updated successfully", true);
        public static readonly SystemMessage TICKET_TYPE_DELETED = new("045", "TicketType deleted successfully", true);
        public static readonly SystemMessage TICKET_TYPE_LEVEL_SORTED = new("046", "TicketType level sorted successfully", true);

        // Notification Success Messages (120-139)
        public static readonly SystemMessage NOTI_BY_USER_RETRIEVED = new("120", "Notifications retrieved successfully", true);
        public static readonly SystemMessage NOTI_DETAIL_RETRIEVED = new("121", "Notification retrieved successfully", true);
        public static readonly SystemMessage MARK_READ_SUCCESS = new("122", "Notification has been read", true);
        public static readonly SystemMessage DELETE_NOTI_SUCCESS = new("122", "Delete notification successfully", true);

        // Goals Success Messages (141-160)
        public static readonly SystemMessage GOAL_RETRIEVED = new("141", "Goal retrieved successfully", true);
        public static readonly SystemMessage GOAL_CREATED = new("142", "Goal created successfully", true);
        public static readonly SystemMessage GOAL_UPDATED = new("143", "Goal updated successfully", true);
        public static readonly SystemMessage GOAL_DELETED = new("144", "Goal deleted successfully", true);

        // User Settings Success Messages (161-180)
        public static readonly SystemMessage USER_SETTINGS_RETRIEVED = new("161", "User settings retrieved successfully", true);
        public static readonly SystemMessage USER_SETTINGS_UPDATED = new("162", "User settings updated successfully", true);

        // SubTask Success Messages (181-200)
        public static readonly SystemMessage SUBTASKS_RETRIEVED = new("181", "SubTasks retrieved successfully", true);
        public static readonly SystemMessage SUBTASK_CREATED = new("182", "SubTask created successfully", true);
        public static readonly SystemMessage SUBTASK_UPDATED = new("183", "SubTask updated successfully", true);
        public static readonly SystemMessage SUBTASK_DELETED = new("184", "SubTask deleted successfully", true);

        //Category Success Messages (201-220)
        public static readonly SystemMessage CATEGORIES_RETRIEVED = new("201", "Categories retrieved successfully", true);
        public static readonly SystemMessage CATEGORY_RETRIEVED = new("202", "Category retrieved successfully", true);
        public static readonly SystemMessage CATEGORY_CREATED = new("203", "Category created successfully", true);
        public static readonly SystemMessage CATEGORY_UPDATED = new("204", "Category updated successfully", true);
        public static readonly SystemMessage CATEGORY_DELETED = new("205", "Category deleted successfully", true);
        //Reminder Success Messages (221-240)
        public static readonly SystemMessage REMINDERS_RETRIEVED = new("221", "Reminders retrieved successfully", true);
        public static readonly SystemMessage REMINDER_RETRIEVED = new("222", "Reminder retrieved successfully", true);
        public static readonly SystemMessage REMINDER_CREATED = new("223", "Reminder created successfully", true);
        public static readonly SystemMessage REMINDER_UPDATED = new("224", "Reminder updated successfully", true);
        public static readonly SystemMessage REMINDER_DELETED = new("225", "Reminder deleted successfully", true);
        //Notification Success Messages (241-260)
        public static readonly SystemMessage NOTIFICATIONS_RETRIEVED = new("241", "Notifications retrieved successfully", true);
        public static readonly SystemMessage NOTIFICATION_RETRIEVED = new("242", "Notification retrieved successfully", true);
        public static readonly SystemMessage NOTIFICATION_CREATED = new("243", "Notification created successfully", true);
        public static readonly SystemMessage NOTIFICATION_UPDATED = new("244", "Notification updated successfully", true);
        public static readonly SystemMessage NOTIFICATION_READ = new("245", "Notification marked as read successfully", true);

        //Venue Success Messages (261-280)
        public static readonly SystemMessage VENUES_RETRIEVED = new("261", "Venues retrieved successfully", true);
        public static readonly SystemMessage VENUE_RETRIEVED = new("262", "Venue retrieved successfully", true);
        public static readonly SystemMessage VENUE_CREATED = new("263", "Venue created successfully", true);
        public static readonly SystemMessage VENUE_UPDATED = new("264", "Venue updated successfully", true);

        //Seat Success Messages (281-300)
        public static readonly SystemMessage SEATS_RETRIEVED = new("281", "Seats retrieved successfully", true);
        public static readonly SystemMessage SEAT_RETRIEVED = new("282", "Seat retrieved successfully", true);
        public static readonly SystemMessage SEATS_CREATED = new("283", "Seat created successfully", true);

        //OrganizerProfile Success Messages (301-320)
        public static readonly SystemMessage ORGANIZERS_RETRIEVED = new("301", "Organizers retrieved successfully", true);
        public static readonly SystemMessage ORGANIZER_RETRIEVED = new("302", "Organizer retrieved successfully", true);
        public static readonly SystemMessage ORGANIZER_CREATED = new("303", "Organizer created successfully", true);
        public static readonly SystemMessage ORGANIZER_UPDATED = new("304", "Organizer updated successfully", true);
        public static readonly SystemMessage ORGANIZER_DELETED = new("305", "Organizer deleted successfully", true);
        public static readonly SystemMessage ORGANIZER_APPROVED = new("306", "Organizer approved successfully", true);
        public static readonly SystemMessage ORGANIZER_REJECTED = new("307", "Organizer rejected successfully", true);

        //Event Success Messages (321-340)
        public static readonly SystemMessage EVENTS_RETRIEVED = new("321", "Events retrieved successfully", true);
        public static readonly SystemMessage EVENT_RETRIEVED = new("322", "Event retrieved successfully", true);
        public static readonly SystemMessage EVENT_CREATED = new("323", "Event created successfully", true);
        public static readonly SystemMessage EVENT_UPDATED = new("324", "Event updated successfully", true);
        public static readonly SystemMessage EVENT_DELETED = new("325", "Event deleted successfully", true);
        public static readonly SystemMessage EVENT_BANNER_UPLOADED = new("326", "Event banner uploaded successfully", true);
        public static readonly SystemMessage EVENT_IMAGE_UPLOADED = new("327", "Event image uploaded successfully", true);
        public static readonly SystemMessage EVENT_PUBLISHED = new("328", "Event published successfully", true);

        // Booking Success Messages (341-360)
        public static readonly SystemMessage BOOKING_CREATED = new("341", "Tickets reserved successfully", true);
        public static readonly SystemMessage BOOKINGS_RETRIEVED = new("342", "Bookings retrieved successfully", true);
        public static readonly SystemMessage BOOKING_CANCELLED = new("343", "Booking cancelled successfully", true);

        #endregion
    }
}
