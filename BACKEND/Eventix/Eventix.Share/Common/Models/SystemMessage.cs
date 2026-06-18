namespace Eventix.Share.Common.Models
{
    public class SystemMessage
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public bool IsSuccess { get; set; }

        public SystemMessage(string code, string message, bool isSuccess = true)
        {
            Code = code;
            Message = message;
            IsSuccess = isSuccess;
        }
        #region Helper Methods

        /// <summary>
        /// Get a custom error message with a specific code
        /// </summary>
        public static SystemMessage CustomError(string code, string message)
        {
            return new SystemMessage(code, message, false);
        }

        /// <summary>
        /// Get a custom success message with a specific code
        /// </summary>
        public static SystemMessage CustomSuccess(string code, string message)
        {
            return new SystemMessage(code, message, true);
        }

        /// <summary>
        /// Get error message with dynamic content
        /// </summary>
        public static SystemMessage DynamicError(string baseCode, string message)
        {
            return new SystemMessage(baseCode, message, false);
        }

        /// <summary>
        /// Get success message with dynamic content
        /// </summary>
        public static SystemMessage DynamicSuccess(string baseCode, string message)
        {
            return new SystemMessage(baseCode, message, true);
        }

        #endregion
    }
}
