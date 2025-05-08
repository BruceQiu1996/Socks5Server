using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Socks5_Server.Dtos;
using Socks5_Server.Models;
using Socks5_Server.Services;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Socks5_Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IUserService _userService;
        private readonly ClientConnectionManager _clientConnectionManager;
        private static readonly ConcurrentDictionary<string, StreamWriter> _clients = new();
        public AccountController(IUserService userService,
                                 ClientConnectionManager clientConnectionManager,
                                 ILogger<AccountController> logger)
        {
            _logger = logger;
            _userService = userService;
            _clientConnectionManager = clientConnectionManager;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult> Login([FromBody] UserLoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest("数据存在错误");

            try
            {
                var user = await _userService.FindSingleUserByUserNameAndPasswordAsync(dto.UserName, dto.Password);
                if (user == null)
                    return BadRequest("用户名密码错误");

                var resp = _userService.UserLogin(user);

                return Ok(new { token = resp });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return Problem();
            }
        }

        [HttpGet("accounts")]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<ActionResult> Get()
        {
            try
            {
                var user = await _userService.GetUsersAsync();
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return Problem();
            }
        }

        [HttpPost()]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<ActionResult> Create([FromBody] UserCreationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest("数据存在错误");

            var user = await _userService.FindSingleUserByUserName(dto.UserName);
            if (user != null)
                return BadRequest("用户名已存在");
            try
            {
                await _userService.CreateUserAsync(dto);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return Problem();
            }
        }

        [HttpDelete()]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<ActionResult> Delete([FromBody] string userId)
        {
            if (!ModelState.IsValid)
                return BadRequest("数据存在错误");
            try
            {
                var user = await _userService.DeleteUserAsync(userId);
                if (user != null)
                {
                    //TODO驱赶已连接的线路下线
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return Problem();
            }
        }

        [HttpPut()]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<ActionResult> Update([FromBody] UserUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest("数据存在错误");
            try
            {
                var oldUser = await _userService.FindSingleUserByUserId(updateDto.UserId);
                var user = await _userService.UpdateUserAsync(updateDto);
                if (user != null)
                {
                    //TODO更新目前已连线的用户
                    _clientConnectionManager.UpdateUserInfo(user);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return Problem();
            }
        }

        [AllowAnonymous]
        [HttpGet("flow/{username}")]
        public async Task Flow(string username)
        {
            // 设置 SSE 响应头
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            var clientId = Guid.NewGuid().ToString();
            var streamWriter = new StreamWriter(Response.Body);

            _clients.TryAdd(clientId, streamWriter);
            try
            {
                while (!HttpContext.RequestAborted.IsCancellationRequested)
                {
                    var user = await _userService.FindSingleUserByUserName(username);
                    if (user != null)
                    {
                        await streamWriter.WriteLineAsync(JsonSerializer.Serialize(new 
                        {
                            Up = user.UploadBytes,
                            Down = user.DownloadBytes
                        }));
                        await streamWriter.FlushAsync();
                    }
                    await Task.Delay(3000);
                }
            }
            finally
            {
                _clients.TryRemove(clientId, out _);
                await streamWriter.DisposeAsync();
            }
        }
    }
}
