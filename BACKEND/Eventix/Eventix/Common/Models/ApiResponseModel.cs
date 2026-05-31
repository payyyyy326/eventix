namespace Eventix.Common.Models
{
    public class ApiResponseModel<T>
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }

        public ApiResponseModel(string message, bool isSuccess, T? data)
        {
            Code = "000"; // Default code
            Message = message;
            IsSuccess = isSuccess;
            Data = data;
        }

        public ApiResponseModel(string code, string message, bool isSuccess, T? data)
        {
            Code = code;
            Message = message;
            IsSuccess = isSuccess;
            Data = data;
        }

        public ApiResponseModel(SystemMessage systemMessage, T? data)
        {
            Code = systemMessage.Code;
            Message = systemMessage.Message;
            IsSuccess = systemMessage.IsSuccess;
            Data = data;
        }

        // Factory methods for success responses
        public static ApiResponseModel<T> Success(SystemMessage systemMessage, T? data = default)
        {
            return new ApiResponseModel<T>(systemMessage, data);
        }

        public static ApiResponseModel<T> Success(string message, T? data = default)
        {
            return new ApiResponseModel<T>("200", message, true, data);
        }
    }
}
