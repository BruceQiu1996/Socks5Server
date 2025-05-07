using System.Net;
using System.Net.Sockets;
using System.Text;

public class HttpToSocks5Proxy
{
    private string _socks5Host;
    private int _socks5Port;
    private int _httpProxyPort;
    private string _httpProxyUsername;
    private string _httpProxyPassword;


    private CancellationTokenSource cancellationTokenSource;
    private TcpListener _tcpListener;
    public void Start(string socks5Host, int socks5Port, string username, string password, int httpProxyPort = 8080)
    {
        _socks5Host = socks5Host;
        _socks5Port = socks5Port;
        _httpProxyPort = httpProxyPort;
        _httpProxyUsername = username;
        _httpProxyPassword = password;

        _tcpListener = new TcpListener(IPAddress.Any, _httpProxyPort);
        _tcpListener.Start();
        cancellationTokenSource = new CancellationTokenSource();
        var _ = Task.Run(async () =>
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                var client = await _tcpListener.AcceptTcpClientAsync();
                HandleClientAsync(client); // 异步处理客户端请求
            }
        }, cancellationTokenSource.Token);
    }

    public void Stop()
    {
        cancellationTokenSource?.Cancel();
        _tcpListener?.Stop();
        _tcpListener?.Dispose();
    }

    private async Task HandleClientAsync(TcpClient httpClient)
    {
        using (httpClient)
        using (var httpStream = httpClient.GetStream())
        {
            try
            {
                // 1. 读取 HTTP 代理请求（如 CONNECT 或 GET/POST）
                var buffer = new byte[4096];
                var bytesRead = await httpStream.ReadAsync(buffer, 0, buffer.Length);
                var request = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                // 2. 解析目标地址（如 CONNECT example.com:443 HTTP/1.1）
                if (!TryParseHttpRequest(request, out string targetHost, out int targetPort))
                {
                    await SendHttpResponse(httpStream, 400, "Bad Request");
                    return;
                }

                // 3. 连接 SOCKS5 代理
                using (var socks5Client = new TcpClient())
                {
                    await socks5Client.ConnectAsync(_socks5Host, _socks5Port);
                    var socks5Stream = socks5Client.GetStream();

                    // 4. 发送 SOCKS5 握手 + 连接请求
                    //await PerformSocks5Handshake(socks5Stream, targetHost, targetPort);
                    await PerformSocks5Handshake(socks5Stream, targetHost, targetPort, _httpProxyUsername, _httpProxyPassword);
                    // 5. 返回 HTTP 200 表示代理成功（如果是 CONNECT 请求）
                    if (request.StartsWith("CONNECT"))
                    {
                        await SendHttpResponse(httpStream, 200, "Connection Established");
                    }

                    // 6. 双向转发数据（HTTP <-> SOCKS5）
                    await Task.WhenAny(
                        ForwardDataAsync(httpStream, socks5Stream),
                        ForwardDataAsync(socks5Stream, httpStream)
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"代理错误: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 带有身份验证的登录
    /// </summary>
    /// <param name="socks5Stream"></param>
    /// <param name="targetHost"></param>
    /// <param name="targetPort"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private async Task PerformSocks5Handshake(NetworkStream socks5Stream,
                                              string targetHost,
                                              int targetPort,
                                              string username,
                                              string password)
    {
        // === 1. 协商认证方法 ===
        // 发送支持的认证方法：无认证(0x00) 和 用户名/密码(0x02)
        var authMethods = new byte[] { 0x05, 0x02, 0x00, 0x02 };
        await socks5Stream.WriteAsync(authMethods, 0, authMethods.Length);

        // 读取服务器选择的认证方法
        var authResponse = new byte[2];
        await socks5Stream.ReadAsync(authResponse, 0, 2);

        if (authResponse[1] == 0xFF)
            throw new Exception("SOCKS5服务器不支持任何提供的认证方法");

        // === 2. 用户名/密码认证 ===
        if (authResponse[1] == 0x02)
        {
            // 构建认证请求包
            var authRequest = new byte[3 + username.Length + password.Length];
            authRequest[0] = 0x01; // 认证子协商版本
            authRequest[1] = (byte)username.Length;
            Encoding.ASCII.GetBytes(username).CopyTo(authRequest, 2);
            authRequest[2 + username.Length] = (byte)password.Length;
            Encoding.ASCII.GetBytes(password).CopyTo(authRequest, 3 + username.Length);

            await socks5Stream.WriteAsync(authRequest, 0, authRequest.Length);

            // 读取认证响应
            var authResult = new byte[2];
            await socks5Stream.ReadAsync(authResult, 0, 2);
            if (authResult[1] != 0x00)
                throw new Exception("SOCKS5用户名/密码认证失败");
        }

        // === 3. 发送连接请求 ===
        var request = new byte[7 + targetHost.Length];
        request[0] = 0x05; // VER
        request[1] = 0x01; // CMD=CONNECT
        request[2] = 0x00; // RSV
        request[3] = 0x03; // ATYP=域名
        request[4] = (byte)targetHost.Length;
        Encoding.ASCII.GetBytes(targetHost).CopyTo(request, 5);
        BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)targetPort)).CopyTo(request, 5 + targetHost.Length);

        await socks5Stream.WriteAsync(request, 0, request.Length);

        // === 4. 读取连接响应 ===
        var response = new byte[10];
        await socks5Stream.ReadAsync(response, 0, 10);
        if (response[1] != 0x00)
            throw new Exception($"SOCKS5连接失败 (状态码: {response[1]})");
    }

    private async Task PerformSocks5Handshake(NetworkStream socks5Stream, string targetHost, int targetPort)
    {
        // 1. SOCKS5 握手（无认证）
        var handshake = new byte[] { 0x05, 0x01, 0x00 };
        await socks5Stream.WriteAsync(handshake, 0, handshake.Length);

        var handshakeResponse = new byte[2];
        await socks5Stream.ReadAsync(handshakeResponse, 0, 2);

        // 2. 发送 SOCKS5 连接请求
        var request = new byte[7 + targetHost.Length];
        request[0] = 0x05; // VER
        request[1] = 0x01; // CMD=CONNECT
        request[2] = 0x00; // RSV
        request[3] = 0x03; // ATYP=域名
        request[4] = (byte)targetHost.Length;
        Encoding.ASCII.GetBytes(targetHost).CopyTo(request, 5);
        BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)targetPort)).CopyTo(request, 5 + targetHost.Length);

        await socks5Stream.WriteAsync(request, 0, request.Length);

        // 3. 读取 SOCKS5 响应
        var response = new byte[10];
        await socks5Stream.ReadAsync(response, 0, 10);
        if (response[1] != 0x00)
            throw new Exception($"SOCKS5 连接失败 (状态码: {response[1]})");
    }

    private bool TryParseHttpRequest(string request, out string host, out int port)
    {
        host = null;
        port = 0;

        // 解析 CONNECT 请求（如 CONNECT example.com:443 HTTP/1.1）
        if (request.StartsWith("CONNECT"))
        {
            var parts = request.Split(' ')[1].Split(':');
            host = parts[0];
            port = int.Parse(parts[1]);
            return true;
        }

        // TODO: 解析 GET/POST 请求（需处理 Host 头）
        return false;
    }


    private async Task SendHttpResponse(NetworkStream stream, int statusCode, string message)
    {
        var response = Encoding.ASCII.GetBytes($"HTTP/1.1 {statusCode} {message}\r\n\r\n");
        await stream.WriteAsync(response, 0, response.Length);
    }

    private async Task ForwardDataAsync(NetworkStream src, NetworkStream dest)
    {
        var buffer = new byte[4096];
        int bytesRead;
        while ((bytesRead = await src.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await dest.WriteAsync(buffer, 0, bytesRead);
        }
    }
}