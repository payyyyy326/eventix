using Eventix.Share.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Eventix.Controllers
{
    /// <summary>
    /// Base controller class that provides type-safe response methods
    /// All API controllers should inherit from this class to ensure consistent response patterns
    /// </summary>
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        /// <summary>
        /// Returns a successful response with typed data
        /// </summary>
        /// <typeparam name="T">The type of the response data</typeparam>
        /// <param name="message">System message for the response</param>
        /// <param name="data">The response data</param>
        /// <returns>ActionResult with typed ApiResponseModel</returns>
        protected ActionResult<ApiResponseModel<T>> SuccessResponse<T>(SystemMessage message, T data)
        {
            return Ok(ApiResponseModel<T>.Success(message, data));
        }

        /// <summary>
        /// Returns a successful response without data (for operations like delete, update confirmations)
        /// </summary>
        /// <param name="message">System message for the response</param>
        /// <returns>ActionResult with object ApiResponseModel</returns>
        protected ActionResult<ApiResponseModel<object>> SuccessResponse(SystemMessage message)
        {
            return Ok(ApiResponseModel<object>.Success(message, null));
        }

        /// <summary>
        /// Creates a typed response with compile-time validation
        /// This ensures the data type matches the declared response type
        /// </summary>
        /// <typeparam name="T">The response model type</typeparam>
        /// <param name="message">System message</param>
        /// <param name="data">Response data that must match type T</param>
        /// <returns>ActionResult with typed ApiResponseModel</returns>
        protected ActionResult<ApiResponseModel<T>> TypedSuccessResponse<T>(SystemMessage message, T data) where T : class
        {
            return Ok(ApiResponseModel<T>.Success(message, data));
        }

        protected ActionResult<ApiResponseModel<PaginationResponse<T>>> TypedEmptyResponse<T>(SystemMessage message, PaginationRequest<T> request) where T : class
        {
            var emptyPaged = new PaginationResponse<T>
            {
                CurrentPage = request.CurrentPage,
                PageSize = request.PageSize,
                TotalRows = 0,
                TotalPages = 0,
                DataList = new List<T>()
            };

            return Ok(ApiResponseModel<PaginationResponse<T>>.Success(message, emptyPaged));
        }
    }
}
