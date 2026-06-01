using Eventix.Common.Constants.SystemData;
using Eventix.Common.Models;
using Eventix.Controllers;
using Eventix.Modules.CategoryModule.DTOs;
using Eventix.Modules.CategoryModule.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static Eventix.Common.Constants.SystemConstants;

namespace Eventix.Modules.CategoryModule.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class CategoryController : BaseApiController
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [AllowAnonymous]
        //GET: api/category/categories
        [HttpGet("categories")]
        public async Task<ActionResult<ApiResponseModel<PaginationResponse<CategoryResponse>>>> GetAllCategories([FromQuery] PaginationRequest<CategoryResponse> request)
        {
            var categories = await _categoryService.GetAllCategoriesAsync(request);
            return SuccessResponse(SystemSuccess.CATEGORIES_RETRIEVED, categories);
        }

        [AllowAnonymous]
        //GET: api/category/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponseModel<CategoryResponse>>> GetCategoryById(Guid id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            return SuccessResponse(SystemSuccess.CATEGORY_RETRIEVED, category);
        }

        [Authorize(Roles = RoleConstants.ADMIN)]
        //POST: api/category
        [HttpPost("create")]
        public async Task<ActionResult<ApiResponseModel<CategoryResponse>>> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var category = await _categoryService.CreateCategoryAsync(userId, request);
            return SuccessResponse(SystemSuccess.CATEGORY_CREATED, category);
        }

        //PUT: api/category/{id}
        [Authorize(Roles = RoleConstants.ADMIN)]
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponseModel<CategoryResponse>>> UpdateCategory(Guid id, [FromBody] UpdateCategoryRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var category = await _categoryService.UpdateCategoryAsync(userId, id, request);
            return SuccessResponse(SystemSuccess.CATEGORY_UPDATED, category);
        }
    }
}
