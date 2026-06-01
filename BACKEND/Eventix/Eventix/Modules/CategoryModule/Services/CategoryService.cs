using Eventix.Common.Constants.SystemData;
using Eventix.Common.Exceptions;
using Eventix.Common.Models;
using Eventix.Data;
using Eventix.Entities;
using Eventix.Extensions;
using Eventix.Modules.CategoryModule.DTOs;
using Eventix.Modules.CategoryModule.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Eventix.Modules.CategoryModule.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _context;

        public CategoryService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<CategoryResponse> CreateCategoryAsync(Guid userId, CreateCategoryRequest request)
        {
            var userExist = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExist)
            {
                throw new NotFoundException(SystemError.UNAUTHORIZED);
            }

            var categoryExist = _context.Categories.Any(c => c.Name == request.Name || c.Slug == request.Slug);
            if (categoryExist)
            {
                throw new BadRequestException(SystemError.CATEGORY_EXIST);
            }
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var category = new Category
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Slug = request.Slug,
                    Description = request.Description,
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new CategoryResponse
                {
                    Id = category.Id,
                    Name = category.Name,
                    Slug = category.Slug,
                    Description = category.Description,
                    IsActive = category.IsActive,
                    CreatedAt = category.CreatedAt,
                    CreatedBy = category.CreatedBy,
                    UpdatedAt = category.UpdatedAt,
                    UpdatedBy = category.UpdatedBy
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public Task DeleteCategoryAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<PaginationResponse<CategoryResponse>> GetAllCategoriesAsync(PaginationRequest<CategoryResponse> request)
        {
            var categories = _context.Categories
                .AsNoTracking()
                .Select(c => new CategoryResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    CreatedBy = c.CreatedBy,
                    UpdatedAt = c.UpdatedAt,
                    UpdatedBy = c.UpdatedBy
                });

            var response = await categories.GetPaged(request.CurrentPage, request.PageSize);

            return response;
        }

        public Task<CategoryResponse> GetCategoryByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<CategoryResponse> UpdateCategoryAsync(Guid userId, Guid id, UpdateCategoryRequest request)
        {
            var userExist = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExist)
            {
                throw new BadRequestException(SystemError.UNAUTHORIZED);
            }

            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                throw new BadRequestException(SystemError.CATEGORY_NOT_FOUND);
            }

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                category.Name = request.Name;
                category.Slug = request.Slug;
                category.Description = request.Description;
                category.IsActive = request.IsActive;
                category.UpdatedAt = DateTime.UtcNow;
                category.UpdatedBy = userId;
                _context.Categories.Update(category);
                _context.SaveChanges();
                transaction.Commit();

                var response = new CategoryResponse
                {
                    Id = category.Id,
                    Name = category.Name,
                    Slug = category.Slug,
                    Description = category.Description,
                    IsActive = category.IsActive,
                    CreatedAt = category.CreatedAt,
                    CreatedBy = category.CreatedBy,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = userId
                };
                return response;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
