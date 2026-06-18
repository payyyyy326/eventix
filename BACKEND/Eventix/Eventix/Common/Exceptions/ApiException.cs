using Eventix.Share.Common.Models;
using System.Net;

namespace Eventix.Common.Exceptions
{
    public class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public bool IsSuccess { get; }
        public string Code { get; }

        public ApiException(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError, bool isSuccess = false, string code = "500")
            : base(message)
        {
            StatusCode = statusCode;
            IsSuccess = isSuccess;
            Code = code;
        }

        public ApiException(SystemMessage systemMessage, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
            : base(systemMessage.Message)
        {
            StatusCode = statusCode;
            IsSuccess = systemMessage.IsSuccess;
            Code = systemMessage.Code;
        }

        public ApiResponseModel<T> ToApiResponse<T>()
        {
            return new ApiResponseModel<T>(Code, Message, IsSuccess, default(T));
        }
    }
}
