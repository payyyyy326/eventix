using Eventix.Common.Models;
using Eventix.Modules.UserModule.DTOs;

namespace Eventix.Modules.UserModule.Interfaces
{
    public interface IUserService
    {
        public Task<UserResponse> GetUserByIdAsync(Guid userId);

        public Task<UserResponse> GetUserByEmailAsync(string email);

        public Task<PaginationRequest<UserResponse>> GetAllUsersAsync();

        public Task<UserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);

        public Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);

        public Task<bool> DeleteUserAsync(Guid userId);
    }
}
