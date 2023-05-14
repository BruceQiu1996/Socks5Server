using Socks5_Server.Dtos;
using Socks5_Server.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Socks5_Server.Services
{
    public interface IUserService
    {
        string UserLogin(User user);
        Task<User> FindSingleUserByUserName(string userName);
        Task<User> FindSingleUserByUserNameAndPasswordAsync(string userName, string password);
        Task CreateUserAsync(UserCreationDto creationDto);
        Task<User> DeleteUserAsync(string userId);
        Task<User> UpdateUserAsync(UserUpdateDto updateDto);
        Task<IEnumerable<UserDto>> GetUsersAsync();
    }
}
