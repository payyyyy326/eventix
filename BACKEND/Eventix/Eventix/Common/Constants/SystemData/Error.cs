using Eventix.Common.Models;

namespace Eventix.Common.Constants.SystemData
{
    public class SystemError
    {
        // General Error Messages (400-419)
        public static readonly SystemMessage BAD_REQUEST = new("400", "Bad request", false);
        public static readonly SystemMessage VALIDATION_ERROR = new("401", "Validation error", false);
        public static readonly SystemMessage MISSING_REQUIRED_FIELD = new("402", "Missing required field", false);
        public static readonly SystemMessage INVALID_FORMAT = new("403", "Invalid format", false);

        // Authentication/Authorization Errors (420-439)
        public static readonly SystemMessage UNAUTHORIZED = new("420", "Unauthorized access", false);
        public static readonly SystemMessage INVALID_CREDENTIALS = new("421", "Invalid credentials", false);
        public static readonly SystemMessage ACCOUNT_NOT_EXIST = new("422", "Account does not exist", false);
        public static readonly SystemMessage EMAIL_PASSWORD_INCORRECT = new("423", "Email or password incorrect", false);
        public static readonly SystemMessage INCORRECT_PASSWORD = new("424", "Incorrect password", false);
        public static readonly SystemMessage TOKEN_EXPIRED = new("425", "Token expired", false);
        public static readonly SystemMessage ACCESS_DENIED = new("426", "Access denied", false);
        public static readonly SystemMessage CURRENT_PASSWORD_INCORRECT = new("427", "Current password incorrect", false);
        public static readonly SystemMessage NOT_PERMISSION = new("428", "You do not have permission", false);
        public static readonly SystemMessage INVALID_OR_EXPIRED_RESET_TOKEN = new("429", "OTP is invalid or expired", false);
        public static readonly SystemMessage INVALID_REFRESH_TOKEN = new("430", "Invalid refresh token", false);
        public static readonly SystemMessage REFRESH_TOKEN_EXPIRED = new("431", "Refresh token expired", false);
        public static readonly SystemMessage ACCOUNT_NOT_ACTIVE = new("432", "Account not active", false);

        // Validation Errors (440-459)
        public static readonly SystemMessage EMAIL_REQUIRED = new("440", "Email is required", false);
        public static readonly SystemMessage PASSWORD_REQUIRED = new("441", "Password is required", false);
        public static readonly SystemMessage FULLNAME_REQUIRED = new("442", "Full name is required", false);
        public static readonly SystemMessage PHONE_REQUIRED = new("443", "Phone number is required", false);
        public static readonly SystemMessage INVALID_EMAIL_FORMAT = new("444", "Invalid email format", false);
        public static readonly SystemMessage PASSWORD_TOO_SHORT = new("445", "Password must be at least 6 characters", false);
        public static readonly SystemMessage ID_REQUIRED = new("447", "ID is required", false);
        public static readonly SystemMessage INVALID_ID = new("448", "ID must be greater than 0", false);
        public static readonly SystemMessage INVALID_QUANTITY = new("450", "Quantity must be greater 0", false);
        public static readonly SystemMessage PASSWORD_NOT_MATCH = new("451", "Password and confirm password does not match", false);
        public static readonly SystemMessage OTP_REQUIRED = new("452", "OTP is required", false);
        public static readonly SystemMessage FIELD_REQUIRED = new("453", "Field is required", false);
        public static readonly SystemMessage INVALID_OTP = new("454", "Invalid OTP", false);
        public static readonly SystemMessage INVALID_FILE_TYPE = new("455", "Only jpg, jpeg, png, webp files are allowed", false);

        // Conflict Errors (460-479)
        public static readonly SystemMessage EMAIL_ALREADY_EXISTS = new("460", "Email already exists", false);
        public static readonly SystemMessage PHONE_ALREADY_EXISTS = new("461", "Phone number already exists", false);
        public static readonly SystemMessage EMAIL_ALREADY_VERIFIED = new("462", "Email already verified", false);
        public static readonly SystemMessage OTP_RESEND_TOO_SOON = new("463", "Please wait before requesting another OTP.", false);
        public static readonly SystemMessage CATEGORY_EXIST = new("464", "Category already exists", false);
        public static readonly SystemMessage VENUE_EXIST = new("465", "Venue already exists", false);
        public static readonly SystemMessage ORGANIZER_EXIST = new("466", "User already has an organizer profile.", false);


        // Not Found Errors (480-499)
        public static readonly SystemMessage EMAIL_NOT_FOUND = new("480", "Email not found", false);
        public static readonly SystemMessage USER_NOT_FOUND = new("481", "User not found", false);
        public static readonly SystemMessage NO_TASKS_FOUND = new("482", "No tasks found", false);
        public static readonly SystemMessage CATEGORY_NOT_FOUND = new("483", "No category found", false);
        public static readonly SystemMessage GOAL_NOT_FOUND = new("484", "No goal found", false);
        public static readonly SystemMessage GOALS_NOT_FOUND = new("485", "No goals found", false);
        public static readonly SystemMessage TASK_NOT_FOUND = new("486", "Task not found", false);
        public static readonly SystemMessage USER_SETTINGS_NOT_FOUND = new("487", "User settings not found", false);
        public static readonly SystemMessage INVALID_TASK_STATUS = new("488", "Invalid task status", false);
        public static readonly SystemMessage SUBTASK_NOT_FOUND = new("489", "SubTask not found", false);
        public static readonly SystemMessage REMINDER_NOT_FOUND = new("490", "Reminder not found", false);
        public static readonly SystemMessage NOTIFICATION_NOT_FOUND = new("491", "Notification not found", false);
        public static readonly SystemMessage VENUE_NOT_FOUND = new("492", "Venue not found", false);
        public static readonly SystemMessage ORGANIZER_NOT_FOUND = new("493", "Organizer not found", false);
        public static readonly SystemMessage EVENT_NOT_FOUND = new("494", "Event not found", false);



        // Business Logic Errors (500-519)
        public static readonly SystemMessage INVALID_DEADLINE = new("500", "Invalid deadline", false);
        public static readonly SystemMessage INVALID_DATE_RANGE = new("501", "FromDate cannot be greater than ToDate", false);
        public static readonly SystemMessage GOAL_UPDATE_TIME_EXPIRED = new("502", "Goal can only be updated before or on its start date.", false);
        public static readonly SystemMessage TASK_UPDATE_TIME_EXPIRED = new("503", "Task can only be updated before or on its start date.", false);
        public static readonly SystemMessage INVALID_SEAT_RANGE = new("509", "The number of seats greater the capacity of venue.", false);
        public static readonly SystemMessage INVALID_LEVEL = new("510", "Level must be greater than 0", false);
        public static readonly SystemMessage ORGANIZER_NOT_APPROVED = new("511", "Organizer profile is not approved yet", false);
        public static readonly SystemMessage INVALID_PRICE_RANGE = new("512", "Price must be greater than or equal to 0", false);


        // Server Errors (520-539)
        public static readonly SystemMessage INTERNAL_SERVER_ERROR = new("520", "Internal server error", false);
        public static readonly SystemMessage DATABASE_ERROR = new("521", "Database error", false);
        public static readonly SystemMessage UPDATE_CONCURRENCY_ERROR = new("522", "Error updating resource", false);
        public static readonly SystemMessage TRANSACTION_FAILED = new("523", "Transaction failed", false);

    }
}
