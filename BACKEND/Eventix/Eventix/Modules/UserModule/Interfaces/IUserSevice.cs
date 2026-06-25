using Eventix.Share.Common.Models;
using Eventix.Share.DTOs.User;
using Eventix.Share.User;

namespace Eventix.Modules.UserModule.Interfaces
{
    public interface IUserService
    {
        public Task<UserResponse> GetUserByIdAsync(Guid userId);

        public Task<UserResponse> GetUserByEmailAsync(string email);

        public Task<PaginationRequest<UserResponse>> GetAllUsersAsync();

        public Task<UserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);

        Task<UserResponse> UploadAvatarAsync(Guid userId, UploadAvatarRequest request);

        public Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);

        public Task<bool> DeleteUserAsync(Guid userId);
    }
}
