using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Windows;

namespace Socks5Client.ViewModels
{
    public class MainPageViewModel : ObservableObject
    {
        public RelayCommand OpenProxyCommand { get; set; }
        public RelayCommand CloseProxyCommand { get; set; }
        private readonly HttpToSocks5Proxy _httpToSocks5Proxy;
        private readonly SseService _sseService;
        public MainPageViewModel()
        {
            OpenProxyCommand = new RelayCommand(OpenProxy);
            CloseProxyCommand = new RelayCommand(CloseProxy);
            _httpToSocks5Proxy = new HttpToSocks5Proxy();
            Upload = "NAN";
            Download = "NAN";
            _sseService = new SseService();
            _sseService.MessageReceived += (message) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var obj = JsonSerializer.Deserialize<JsonElement>(message);
                    if (obj.TryGetProperty("Up", out JsonElement upElement) && upElement.ValueKind == JsonValueKind.Number)
                    {
                        Upload = ConvertLongBytesToText(upElement.GetInt64()); // 12636681
                    }

                    if (obj.TryGetProperty("Down", out JsonElement downElement) && downElement.ValueKind == JsonValueKind.Number)
                    {
                        Download = ConvertLongBytesToText(downElement.GetInt64()); // 12636681
                    }
                });
            };
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }

        private string _upload;
        public string Upload
        {
            get => _upload;
            set => SetProperty(ref _upload, value);
        }

        private string _download;
        public string Download
        {
            get => _download;
            set => SetProperty(ref _download, value);
        }

        /// <summary>
        /// 开启代理
        /// </summary>
        private void OpenProxy()
        {
            try
            {
                if (IsRunning)
                    return;

                if (string.IsNullOrEmpty(App.SettingsModel.Socks5Host))
                {
                    HandyControl.Controls.MessageBox.Show("请设置服务器IP地址", "错误", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    return;
                }

                SystemProxy.SetProxy($"127.0.0.1:{App.SettingsModel.LocalPort}", true);
                _httpToSocks5Proxy.Start(App.SettingsModel.Socks5Host, (int)App.SettingsModel.Socks5Port,
                            App.SettingsModel.UserName,
                            App.SettingsModel.Password,
                            (int)App.SettingsModel.LocalPort);

                Task.Run(async () => {await Task.Delay(500); await _sseService.ConnectAsync(App.SettingsModel.Socks5Host, App.SettingsModel.UserName); });
                IsRunning = true;
            }
            catch (Exception ex)
            {
                IsRunning = false;
                HandyControl.Controls.MessageBox.Show("启动失败", "错误", MessageBoxButton.YesNo, MessageBoxImage.Error);
            }
        }

        private void CloseProxy()
        {
            try
            {
                if (!IsRunning)
                    return;

                SystemProxy.SetProxy("", false);
                _httpToSocks5Proxy.Stop();
                _sseService.Disconnect();
                IsRunning = false;
            }
            catch (Exception ex)
            {
                IsRunning = false;
                HandyControl.Controls.MessageBox.Show("关闭失败", "错误", MessageBoxButton.YesNo, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 流量数字转为文本显示
        /// </summary>
        /// <param name="bytes">流量，单位byte</param>
        /// <returns>显示文本</returns>
        private string ConvertLongBytesToText(long bytes)
        {
            if (bytes < 1024)
            {
                return bytes + "B";
            }
            else if (bytes >= 1024 && bytes < 1024 * 1024)
            {
                return (bytes * 1.0 / 1024).ToString("0.00") + "KB";
            }
            else if (bytes >= 1024 * 1024 && bytes < 1024 * 1024 * 1024)
            {
                return (bytes * 1.0 / (1024 * 1024)).ToString("0.00") + "MB";
            }
            else
            {
                return (bytes * 1.0 / (1024 * 1024 * 1024)).ToString("0.00") + "GB";
            }
        }
    }

    //SSE调用显示流量
    public class SseService
    {
        private HttpClient _httpClient;
        private CancellationTokenSource _cts;

        public event Action<string>? MessageReceived;

        public async Task ConnectAsync(string remoteAddress, string userName)
        {
            _httpClient = new HttpClient(new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{remoteAddress}:5000"), // 明确指定代理
            });
            _cts = new CancellationTokenSource();

            try
            {
                using var response = await _httpClient
                    .GetAsync($"http://{remoteAddress}:5000/account/flow/{userName}", HttpCompletionOption.ResponseHeadersRead,_cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var reader = new StreamReader(stream);
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync();
                        if (!string.IsNullOrEmpty(line))
                        {
                            MessageReceived?.Invoke(line);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SSE连接错误: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            _cts?.Cancel();
        }
    }
}
