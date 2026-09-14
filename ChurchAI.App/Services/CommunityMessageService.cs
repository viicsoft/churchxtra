using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ChurchAI.App.Services.Interfaces;

namespace ChurchAI.App.Services;

public class CommunityMessageService : ICommunityMessageService, IDisposable
{
    private TcpListener? _tcpListener;
    private CancellationTokenSource? _cts;
    private bool _isServerRunning;
    private string _serverUrl = string.Empty;
    private BitmapImage? _qrCodeImage;

    public bool IsServerRunning => _isServerRunning;
    public string ServerUrl => _serverUrl;
    public BitmapImage? QrCodeImage => _qrCodeImage;
    public ObservableCollection<CommunityMessage> ReceivedMessages { get; } = new();

    public event EventHandler<CommunityMessageEventArgs>? MessageReceived;

    public CommunityMessageService()
    {
        StartServer();
    }

    public void StartServer()
    {
        if (_isServerRunning) return;

        var localIp = GetLocalIPAddress();
        _serverUrl = $"http://{localIp}:8090/";
        _qrCodeImage = GenerateQrCode(_serverUrl);

        try
        {
            _tcpListener = new TcpListener(IPAddress.Parse(localIp), 8090);
            _tcpListener.Start();
            _isServerRunning = true;
            _cts = new CancellationTokenSource();
            
            var token = _cts.Token;
            Task.Run(() => ListenTcpAsync(token), token);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to start TCP listener on local IP: {ex.Message}");
            try
            {
                // Fallback to loopback
                _tcpListener = new TcpListener(IPAddress.Loopback, 8090);
                _tcpListener.Start();
                _isServerRunning = true;
                _serverUrl = "http://127.0.0.1:8090/";
                _qrCodeImage = GenerateQrCode(_serverUrl);
                _cts = new CancellationTokenSource();
                
                var token = _cts.Token;
                Task.Run(() => ListenTcpAsync(token), token);
            }
            catch (Exception fallbackEx)
            {
                System.Diagnostics.Debug.WriteLine($"Failed fallback TCP listener: {fallbackEx.Message}");
                _isServerRunning = false;
                _serverUrl = string.Empty;
                _qrCodeImage = null;
            }
        }
    }

    public void StopServer()
    {
        if (!_isServerRunning) return;

        try
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _tcpListener?.Stop();
            _tcpListener = null;
        }
        catch { }
        finally
        {
            _isServerRunning = false;
            _serverUrl = string.Empty;
            _qrCodeImage = null;
        }
    }

    private async Task ListenTcpAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var client = await _tcpListener!.AcceptTcpClientAsync(token);
                _ = Task.Run(() => HandleTcpClientAsync(client), token);
            }
            catch
            {
                break;
            }
        }
    }

    private async Task HandleTcpClientAsync(TcpClient client)
    {
        using (client)
        using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
        {
            try
            {
                // Read Request Line
                string? requestLine = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(requestLine)) return;

                var parts = requestLine.Split(' ');
                if (parts.Length < 2) return;

                string method = parts[0];
                string path = parts[1];

                // Read Headers to find Content-Length
                int contentLength = 0;
                string? line;
                while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
                {
                    if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(line.Substring(15).Trim(), out contentLength);
                    }
                }

                // Handle CORS preflight request
                if (method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("HTTP/1.1 200 OK");
                    await writer.WriteLineAsync("Access-Control-Allow-Origin: *");
                    await writer.WriteLineAsync("Access-Control-Allow-Methods: GET, POST, OPTIONS");
                    await writer.WriteLineAsync("Access-Control-Allow-Headers: Content-Type");
                    await writer.WriteLineAsync("Connection: close");
                    await writer.WriteLineAsync();
                    await writer.FlushAsync();
                    return;
                }

                if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && path.Equals("/send", StringComparison.OrdinalIgnoreCase))
                {
                    // Read Body
                    char[] buffer = new char[contentLength];
                    int read = 0;
                    while (read < contentLength)
                    {
                        int chunk = await reader.ReadAsync(buffer, read, contentLength - read);
                        if (chunk <= 0) break;
                        read += chunk;
                    }
                    string body = new string(buffer);

                    try
                    {
                        var payload = System.Text.Json.JsonSerializer.Deserialize<MessagePayload>(body, new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (payload != null && !string.IsNullOrWhiteSpace(payload.Message))
                        {
                            var msg = new CommunityMessage
                            {
                                Title = string.IsNullOrWhiteSpace(payload.Title) ? "Community Alert" : payload.Title.Trim(),
                                Text = payload.Message.Trim(),
                                Tag = (payload.Tag ?? "green").ToLowerInvariant()
                            };
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                ReceivedMessages.Insert(0, msg);
                            });
                            MessageReceived?.Invoke(this, new CommunityMessageEventArgs(msg));
                        }
                    }
                    catch { }

                    // Respond Success JSON
                    string responseJson = "{\"status\":\"success\"}";
                    byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);

                    await writer.WriteLineAsync("HTTP/1.1 200 OK");
                    await writer.WriteLineAsync("Content-Type: application/json");
                    await writer.WriteLineAsync("Access-Control-Allow-Origin: *");
                    await writer.WriteLineAsync("Access-Control-Allow-Headers: Content-Type");
                    await writer.WriteLineAsync($"Content-Length: {responseBytes.Length}");
                    await writer.WriteLineAsync("Connection: close");
                    await writer.WriteLineAsync();
                    await writer.FlushAsync();
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                }
                else if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) && path.Equals("/", StringComparison.OrdinalIgnoreCase))
                {
                    // Serve HTML Form
                    string html = SubmitPageHtml;
                    byte[] htmlBytes = Encoding.UTF8.GetBytes(html);

                    await writer.WriteLineAsync("HTTP/1.1 200 OK");
                    await writer.WriteLineAsync("Content-Type: text/html; charset=utf-8");
                    await writer.WriteLineAsync($"Content-Length: {htmlBytes.Length}");
                    await writer.WriteLineAsync("Connection: close");
                    await writer.WriteLineAsync();
                    await writer.FlushAsync();
                    await stream.WriteAsync(htmlBytes, 0, htmlBytes.Length);
                }
                else
                {
                    // Not Found
                    await writer.WriteLineAsync("HTTP/1.1 404 Not Found");
                    await writer.WriteLineAsync("Content-Length: 0");
                    await writer.WriteLineAsync("Connection: close");
                    await writer.WriteLineAsync();
                    await writer.FlushAsync();
                }
            }
            catch { }
        }
    }

    private string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (!IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }
            }
        }
        catch { }
        return "127.0.0.1";
    }

    private BitmapImage? GenerateQrCode(string url)
    {
        try
        {
            using (var qrGenerator = new QRCoder.QRCodeGenerator())
            using (var qrCodeData = qrGenerator.CreateQrCode(url, QRCoder.QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new QRCoder.PngByteQRCode(qrCodeData))
            {
                byte[] qrCodeBytes = qrCode.GetGraphic(20);
                return ConvertByteArrayToBitmapImage(qrCodeBytes);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to generate QR Code: {ex.Message}");
            return null;
        }
    }

    private BitmapImage? ConvertByteArrayToBitmapImage(byte[] imageBytes)
    {
        if (imageBytes == null || imageBytes.Length == 0) return null;
        try
        {
            using (var mem = new MemoryStream(imageBytes))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = mem;
                image.EndInit();
                image.Freeze();
                return image;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to convert bytes to BitmapImage: {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        StopServer();
    }

    private class MessagePayload
    {
        public string Title { get; set; } = "Community Alert";
        public string Message { get; set; } = string.Empty;
        public string Tag { get; set; } = "green";
    }

    private static readonly string SubmitPageHtml = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>ChurchXtra Live - Send Message</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif;
            background: linear-gradient(135deg, #0F172A 0%, #1E1B4B 100%);
            color: #F8FAFC;
            margin: 0;
            padding: 20px;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            box-sizing: border-box;
        }
        .card {
            background: rgba(30, 41, 59, 0.7);
            backdrop-filter: blur(16px);
            border: 1px solid rgba(255, 255, 255, 0.1);
            border-radius: 16px;
            padding: 30px;
            width: 100%;
            max-width: 450px;
            box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.3), 0 8px 10px -6px rgba(0, 0, 0, 0.3);
        }
        h1 {
            font-size: 24px;
            font-weight: 700;
            margin-top: 0;
            margin-bottom: 8px;
            text-align: center;
            background: linear-gradient(to right, #38BDF8, #818CF8);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
        }
        p.subtitle {
            font-size: 14px;
            color: #94A3B8;
            text-align: center;
            margin-top: 0;
            margin-bottom: 25px;
        }
        .form-group {
            margin-bottom: 20px;
        }
        label {
            display: block;
            font-size: 13px;
            font-weight: 600;
            margin-bottom: 8px;
            color: #E2E8F0;
        }
        textarea {
            width: 100%;
            height: 120px;
            background: rgba(15, 23, 42, 0.6);
            border: 1px solid #334155;
            border-radius: 8px;
            padding: 12px;
            color: #F8FAFC;
            font-size: 14px;
            font-family: inherit;
            resize: none;
            box-sizing: border-box;
            outline: none;
            transition: border-color 0.2s;
        }
        textarea:focus {
            border-color: #6366F1;
        }
        input[type=""text""] {
            width: 100%;
            height: 40px;
            background: rgba(15, 23, 42, 0.6);
            border: 1px solid #334155;
            border-radius: 8px;
            padding: 10px 12px;
            color: #F8FAFC;
            font-size: 14px;
            font-family: inherit;
            box-sizing: border-box;
            outline: none;
            transition: border-color 0.2s;
            margin-bottom: 5px;
        }
        input[type=""text""]:focus {
            border-color: #6366F1;
        }
        .tags-container {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 10px;
        }
        .tag-option {
            position: relative;
        }
        .tag-option input {
            position: absolute;
            opacity: 0;
            width: 0;
            height: 0;
        }
        .tag-label {
            display: block;
            text-align: center;
            padding: 10px;
            border-radius: 8px;
            font-size: 12px;
            font-weight: 700;
            cursor: pointer;
            border: 2px solid transparent;
            transition: all 0.2s;
        }
        .tag-red { background-color: rgba(239, 68, 68, 0.15); color: #FCA5A5; }
        .tag-yellow { background-color: rgba(234, 179, 8, 0.15); color: #FDE047; }
        .tag-green { background-color: rgba(16, 185, 129, 0.15); color: #6EE7B7; }

        .tag-option input:checked + .tag-red { border-color: #EF4444; background-color: rgba(239, 68, 68, 0.3); }
        .tag-option input:checked + .tag-yellow { border-color: #EAB308; background-color: rgba(234, 179, 8, 0.3); }
        .tag-option input:checked + .tag-green { border-color: #10B981; background-color: rgba(16, 185, 129, 0.3); }

        button {
            width: 100%;
            background: linear-gradient(to right, #4F46E5, #06B6D4);
            color: white;
            border: none;
            border-radius: 8px;
            padding: 14px;
            font-size: 15px;
            font-weight: 700;
            cursor: pointer;
            margin-top: 10px;
            transition: opacity 0.2s;
        }
        button:hover {
            opacity: 0.9;
        }
        #status-message {
            margin-top: 15px;
            padding: 12px;
            border-radius: 8px;
            font-size: 14px;
            font-weight: 600;
            text-align: center;
            display: none;
        }
        .success { background-color: rgba(16, 185, 129, 0.15); color: #34D399; }
        .error { background-color: rgba(239, 68, 68, 0.15); color: #F87171; }
    </style>
</head>
<body>
    <div class=""card"">
        <h1>ChurchXtra Link</h1>
        <p class=""subtitle"">Type your message and send it to the control screen</p>
        
        <div class=""form-group"">
            <label for=""header"">MESSAGE HEADER / SENDER</label>
            <input type=""text"" id=""header"" placeholder=""e.g. Announcement, Alert, Pastor..."" value=""Community Alert"">
        </div>

        <div class=""form-group"">
            <label for=""message"">MESSAGE CONTENT</label>
            <textarea id=""message"" placeholder=""Type something to project..."" required></textarea>
        </div>

        <div class=""form-group"">
            <label>PRIORITY TAG</label>
            <div class=""tags-container"">
                <div class=""tag-option"">
                    <input type=""radio"" id=""tag-red"" name=""tag"" value=""red"">
                    <label for=""tag-red"" class=""tag-label tag-red"">Project<br>Now</label>
                </div>
                <div class=""tag-option"">
                    <input type=""radio"" id=""tag-yellow"" name=""tag"" value=""yellow"">
                    <label for=""tag-yellow"" class=""tag-label tag-yellow"">Project<br>Later</label>
                </div>
                <div class=""tag-option"">
                    <input type=""radio"" id=""tag-green"" name=""tag"" value=""green"" checked>
                    <label for=""tag-green"" class=""tag-label tag-green"">Project<br>Soon</label>
                </div>
            </div>
        </div>

        <button onclick=""sendMessage()"">Send Message</button>
        
        <div id=""status-message""></div>
    </div>

    <script>
        async function sendMessage() {
            const headerText = document.getElementById('header').value.trim();
            const messageText = document.getElementById('message').value.trim();
            const tagValue = document.querySelector('input[name=""tag""]:checked').value;
            const statusDiv = document.getElementById('status-message');

            if (!messageText) {
                statusDiv.className = 'error';
                statusDiv.innerText = 'Please enter a message!';
                statusDiv.style.display = 'block';
                return;
            }

            try {
                const response = await fetch('/send', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        title: headerText || ""Community Alert"",
                        message: messageText,
                        tag: tagValue
                    })
                });

                if (response.ok) {
                    statusDiv.className = 'success';
                    statusDiv.innerText = 'Message sent successfully!';
                    document.getElementById('message').value = '';
                    document.getElementById('header').value = 'Community Alert';
                } else {
                    statusDiv.className = 'error';
                    statusDiv.innerText = 'Failed to send message.';
                }
            } catch (err) {
                statusDiv.className = 'error';
                statusDiv.innerText = 'Network error: ' + err.message;
            }
            statusDiv.style.display = 'block';
        }
    </script>
</body>
</html>";
}
