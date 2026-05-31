using Eventix.Modules.UserModule.DTOs;

namespace Eventix.Modules.UserModule.Interfaces
{
    public interface IUserService
    {
        public Task<UserResponse> GetUserByIdAsync(Guid userId);

        public Task<UserResponse> GetUserByEmailAsync(string email);

        public Task<IEnumerable<UserResponse>> GetAllUsersAsync();

        public Task<UserResponse> UpdateUserAsync(Guid userId, UserUpdateRequest request);

        public Task<bool> DeleteUserAsync(Guid userId);
    }
}
