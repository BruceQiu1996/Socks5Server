using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Socks5Manager.Http
{
    public class ServerHttpService
    {
        private HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        public ServerHttpService(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_configuration.GetSection("ServerAddress").Value);
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void SetToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }

        public async Task<HttpResponseMessage> PostAsync(string url, dynamic body)
        {
            var content = new StringContent(JsonSerializer.Serialize(body));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var resp = await _httpClient.PostAsync(url, content);

            return resp;
        }

        public async Task<HttpResponseMessage> PutAsync(string url, dynamic body)
        {
            var content = new StringContent(JsonSerializer.Serialize(body));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var resp = await _httpClient.PutAsync(url, content);

            return resp;
        }

        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            return await _httpClient.GetAsync(url);
        }
    }
}
