using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Socks5_Server.Configuration;
using Socks5_Server.Data;
using Socks5_Server.Dtos;
using Socks5_Server.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Socks5_Server.Services
{
    public class UserService : IUserService
    {
        private readonly IDbContextFactory<Socks5ServerDbContext> _dbContextFactory;
        private readonly JwtConfiguration _jwtConfiguration;
        private readonly ClientConnectionManager _clientConnectionManager;

        public UserService(IDbContextFactory<Socks5ServerDbContext> dbContextFactory,
                           IOptions<JwtConfiguration> options,
                           ClientConnectionManager clientConnectionManager)
        {
            _dbContextFactory = dbContextFactory;
            _jwtConfiguration = options.Value;
            _clientConnectionManager = clientConnectionManager;
        }

        public async Task CreateUserAsync(UserCreationDto creationDto)
        {
            using (var context = _dbContextFactory.CreateDbContext())
            {
                await context.Users.AddAsync(new User()
                {
                    Role = Role.user,
                    UserName = creationDto.UserName,
                    Password = creationDto.Password,
                    ExpireTime = creationDto.ExpireTime
                });

                await context.SaveChangesAsync();
            }
        }

        public async Task<User> DeleteUserAsync(string userId)
        {
            using (var context = _dbContextFactory.CreateDbContext())
            {
                var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId);
                if (user != null)
                {
                    context.Users.Remove(user);
                    await context.SaveChangesAsync();

                    return user;
                }

                return null;
            }
        }

        public async Task<User> FindSingleUserByUserName(string userName)
        {
            using (var context = _dbContextFactory.CreateDbContext())
            {
                return await context.Users.FirstOrDefaultAsync(x => x.UserName == userName);
            }
        }

        public async Task<User> FindSingleUserByUserNameAndPasswordAsync(string userName, string password)
        {
            using (var context = _dbContextFactory.CreateDbContext())
            {
                return await context.Users.FirstOrDefaultAsync(x => x.UserName == userName && x.Password == password);
            }
        }

        public async Task<IEnumerable<UserDto>> GetUsersAsync()
        {
            using (var context = _dbContextFactory.CreateDbContext())
            {
                var users = await context.Users.ToListAsync();
                var userDtos = new List<UserDto>();
                users.ForEach(x =>
                {
                    UserDto token = new UserDto();
                    token.UserName = x.UserName;
                    token.ExpireTime = x.ExpireTime;
                    token.UserId = x.Id;

                    var clients = _clientConnectionManager.Where(x => x.UserName == x.UserName);
                    if (clients.Count() > 0)
                    {
                        token.IsOnline = true;
                    }

                    token.UploadBytes = x.UploadBytes + clients.Sum(x => x.UploadBytes);
                    token.DownloadBytes = x.DownloadBytes + clients.Sum(x => x.DownloadBytes);
                    userDtos.Add(token);
                });

                return userDtos;
            }
        }

        public async Task<User> UpdateUserAsync(UserUpdateDto updateDto)
        {
            using (var context = _dbContextFactory.CreateDbContext())
            {
                var user = await context.Users.FirstOrDefaultAsync(x => x.Id == updateDto.UserId);
                if (user != null)
                {
                    user.ExpireTime = updateDto.ExpireTime;
                    user.Password = updateDto.Password;
                    context.Users.Update(user);
                    await context.SaveChangesAsync();

                    return user;
                }

                return null;
            }
        }

        public string UserLogin(User user)
        {
            var securityKey = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtConfiguration.Secret)), SecurityAlgorithms.HmacSha256);
            List<Claim> claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Iss, _jwtConfiguration.Iss),
                    new Claim(JwtRegisteredClaimNames.Aud, _jwtConfiguration.Aud),
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                };
            SecurityToken securityToken = new JwtSecurityToken(signingCredentials: securityKey, expires: DateTime.Now.AddDays(3), claims: claims);
            var token = new JwtSecurityTokenHandler().WriteToken(securityToken);

            return token;
        }
    }
}
