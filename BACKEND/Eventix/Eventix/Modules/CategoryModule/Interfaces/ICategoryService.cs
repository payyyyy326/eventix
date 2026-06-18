using Eventix.Share.Category;
using Eventix.Share.Common.Models;

namespace Eventix.Modules.CategoryModule.Interfaces
{
    public interface ICategoryService
    {
        Task<PaginationResponse<CategoryResponse>> GetAllCategoriesAsync(PaginationRequest<CategoryResponse> request);
        Task<CategoryResponse> GetCategoryByIdAsync(Guid id);
        Task<CategoryResponse> CreateCategoryAsync(Guid userId, CreateCategoryRequest request);
        Task<CategoryResponse> UpdateCategoryAsync(Guid userId, Guid id, UpdateCategoryRequest request);
        Task DeleteCategoryAsync(Guid id);
    }
}
