namespace Eventix.Web.Models
{
    public class ApiResponseModel<T>
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
    }
}