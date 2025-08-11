using SmartGuardHub.Features.DeviceManagement;

namespace SmartGuardHub.Features.Users
{
    public interface IUserRepository
    {
        Task<User?> GetByUserNameAsync(string userName);
        Task<IEnumerable<User>> GetActiveUsersAsync();
        Task<IEnumerable<User>> GetUsersWithRemoteActionAsync();
        Task<User?> GetUserWithDevicesAsync(int userId);
        Task<IEnumerable<User>> GetUsersWithDevicesAsync();
        Task<bool> UserNameExistsAsync(string userName);
        Task<IEnumerable<User>> GetUsersByCreationDateRangeAsync(DateTime startDate, DateTime endDate);
        Task DeactivateUserAsync(int userId);
        Task ActivateUserAsync(int userId);
        Task ToggleRemoteActionPermissionAsync(int userId);

        // Methods for managing user-device relationships
        Task<User> CreateUserWithDevicesAsync(User user, IEnumerable<int> deviceIds);
        Task AddDevicesToUserAsync(int userId, IEnumerable<int> deviceIds);
        Task RemoveDevicesFromUserAsync(int userId, IEnumerable<int> deviceIds);
        Task UpdateUserDevicesAsync(int userId, IEnumerable<int> deviceIds);
        Task<IEnumerable<int>> GetUserDeviceIdsAsync(int userId);
    }
}
