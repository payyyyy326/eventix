using Eventix.Common.Constants;
using Eventix.Common.Constants.SystemData;
using Eventix.Common.Exceptions;
using Eventix.Data;
using Eventix.Modules.UserModule.DTOs;
using Eventix.Modules.UserModule.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Eventix.Modules.UserModule.Service
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public UserService(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<UserResponse> GetUserByIdAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new ApiException(SystemError.USER_NOT_FOUND);
            }

            return new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                EmailVerified = user.EmailVerified,
                EmailVerifiedAt = user.EmailVerifiedAt,
                Roles = user.Roles.Select(r => r.Name).ToList()
            };
        }

        public async Task<UserResponse> GetUserByEmailAsync(string email)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                throw new ApiException(SystemError.USER_NOT_FOUND);
            }

            return new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                EmailVerified = user.EmailVerified,
                EmailVerifiedAt = user.EmailVerifiedAt,
                Roles = user.Roles.Select(r => r.Name).ToList()
            };
        }

        public async Task<Common.Models.PaginationRequest<UserResponse>> GetAllUsersAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<UserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new BadRequestException(SystemError.USER_NOT_FOUND);
            }

            if (!string.IsNullOrEmpty(request.PhoneNumber) && request.PhoneNumber != user.PhoneNumber)
            {
                var phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber && u.Id != userId);
                if (phoneExists)
                {
                    throw new BadRequestException(SystemError.PHONE_ALREADY_EXISTS);
                }
                user.PhoneNumber = request.PhoneNumber;
            }

            user.FullName = request.FullName;

            if (request.Avatar != null)
            {
                var webRootPath = _environment.WebRootPath
                    ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                var uploadsFolder = Path.Combine(
                    webRootPath,
                    SystemConstants.AppPaths.AVATAR_UPLOADS
                );

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName =
                    Guid.NewGuid().ToString() +
                    Path.GetExtension(request.Avatar.FileName);

                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await request.Avatar.CopyToAsync(fileStream);
                }

                user.AvatarUrl =
                    $"/{SystemConstants.AppPaths.AVATAR_UPLOADS}/{uniqueFileName}";
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                EmailVerified = user.EmailVerified,
                EmailVerifiedAt = user.EmailVerifiedAt,
                Roles = user.Roles.Select(r => r.Name).ToList()
            };
        }

        public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new BadRequestException(SystemError.USER_NOT_FOUND);
            }

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            {
                throw new BadRequestException(SystemError.CURRENT_PASSWORD_INCORRECT);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            user.Status = SystemConstants.StatusAccount.DELETED;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
