using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChurchAI.App.Services.Interfaces;

namespace ChurchAI.App.Services;

public class WebServerService : IWebServerService, IDisposable
{
    private readonly ISettingsService _settings;
    private Window? _captureWindow;
    private bool _isBroadcasting;
    private TcpListener? _listener;
    private CancellationTokenSource? _cancellationTokenSource;
    private System.Windows.Threading.DispatcherTimer? _timer;
    private bool _isServerRunning;

    private readonly ConcurrentDictionary<Guid, BlockingCollection<byte[]>> _clientQueues = new();

    public bool IsBroadcasting => _isBroadcasting;

    public WebServerService(ISettingsService settings)
    {
        _settings = settings;
        _settings.PropertyChanged += Settings_PropertyChanged;
        
        // Start the HTTP server listener immediately if enabled
        if (_settings.EnableWebServerOutput)
        {
            StartServer();
        }
    }

    private void Settings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ISettingsService.EnableWebServerOutput))
        {
            if (_settings.EnableWebServerOutput)
                StartServer();
            else
                StopServer();
        }
    }

    private void StartServer()
    {
        if (_isServerRunning) return;
        _isServerRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            _listener = new TcpListener(IPAddress.Any, 8080);
            _listener.Start();
            Task.Run(() => AcceptClientsAsync(_cancellationTokenSource.Token));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to start WebServer listener: {ex.Message}");
            _isServerRunning = false;
        }
    }

    private void StopServer()
    {
        if (!_isServerRunning) return;
        _isServerRunning = false;
        
        StopBroadcasting();
        
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        _listener?.Stop();
        _listener = null;
        
        foreach (var queue in _clientQueues.Values)
        {
            queue.CompleteAdding();
        }
    }

    public void StartBroadcasting(Window windowToCapture)
    {
        if (_isBroadcasting || windowToCapture == null) return;
        if (!_settings.EnableWebServerOutput) return;

        _captureWindow = windowToCapture;
        _isBroadcasting = true;

        if (!_isServerRunning)
        {
            StartServer();
        }

        try
        {
            _timer = new System.Windows.Threading.DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(33); // ~30 fps
            _timer.Tick += CaptureFrame;
            _timer.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to start WebServer timer: {ex.Message}");
            StopBroadcasting();
        }
    }

    private async Task AcceptClientsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener != null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(token);
                _ = Task.Run(() => HandleClientAsync(client, token));
            }
            catch
            {
                // Ignored
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        var clientId = Guid.NewGuid();
        var queue = new BlockingCollection<byte[]>(boundedCapacity: 1); // Only keep the most recent frame
        _clientQueues.TryAdd(clientId, queue);

        try
        {
            var stream = client.GetStream();
            
            // Read HTTP Request
            var reader = new StreamReader(stream, Encoding.ASCII);
            while (true)
            {
                var line = await reader.ReadLineAsync(token);
                if (string.IsNullOrEmpty(line)) break;
            }

            // Write HTTP Response headers for MJPEG
            var header = "HTTP/1.1 200 OK\r\n" +
                         "Content-Type: multipart/x-mixed-replace; boundary=--frameboundary\r\n" +
                         "Connection: keep-alive\r\n" +
                         "Cache-Control: no-cache, private\r\n" +
                         "Pragma: no-cache\r\n\r\n";
            var headerBytes = Encoding.ASCII.GetBytes(header);
            await stream.WriteAsync(headerBytes, token);
            await stream.FlushAsync(token);

            // Send frames in a loop
            while (!token.IsCancellationRequested && client.Connected)
            {
                // Take blocks until a new frame is available, or token is cancelled
                var frameToSend = queue.Take(token);

                var boundaryHeader = "--frameboundary\r\nContent-Type: image/jpeg\r\nContent-Length: " + frameToSend.Length + "\r\n\r\n";
                var boundaryBytes = Encoding.ASCII.GetBytes(boundaryHeader);
                var endBoundaryBytes = Encoding.ASCII.GetBytes("\r\n\r\n");

                await stream.WriteAsync(boundaryBytes, token);
                await stream.WriteAsync(frameToSend, token);
                await stream.WriteAsync(endBoundaryBytes, token);
                await stream.FlushAsync(token);
            }
        }
        catch
        {
            // Client disconnected, cancelled, or network error
        }
        finally
        {
            _clientQueues.TryRemove(clientId, out _);
            queue.Dispose();
            client.Close();
        }
    }

    private void CaptureFrame(object? sender, EventArgs e)
    {
        if (_captureWindow == null || !_isBroadcasting) return;

        try
        {
            int width = (int)_captureWindow.ActualWidth;
            int height = (int)_captureWindow.ActualHeight;

            if (width <= 0 || height <= 0) return;

            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(_captureWindow);

            var encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = 70;
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            byte[] imageBytes;
            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                imageBytes = ms.ToArray();
            }

            // Broadcast the frame to all connected clients
            foreach (var queue in _clientQueues.Values)
            {
                if (!queue.IsCompleted)
                {
                    // Remove any pending old frame to prevent latency buildup and unblock the queue
                    while (queue.TryTake(out _)) { }
                    queue.TryAdd(imageBytes);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebServer Capture Error: {ex.Message}");
        }
    }

    public void StopBroadcasting()
    {
        if (!_isBroadcasting) return;

        if (_timer != null)
        {
            _timer.Stop();
            _timer.Tick -= CaptureFrame;
            _timer = null;
        }
        
        _isBroadcasting = false;
        _captureWindow = null;
        
        // Push a blank black frame so connected phones don't freeze on the last verse
        byte[]? blankFrame = null;
        try
        {
            var rtb = new RenderTargetBitmap(1920, 1080, 96, 96, PixelFormats.Pbgra32);
            var visual = new DrawingVisual();
            using (var ctx = visual.RenderOpen())
            {
                ctx.DrawRectangle(Brushes.Black, null, new Rect(0, 0, 1920, 1080));
            }
            rtb.Render(visual);
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                blankFrame = ms.ToArray();
            }
        }
        catch { }

        if (blankFrame != null)
        {
            foreach (var queue in _clientQueues.Values)
            {
                if (!queue.IsCompleted)
                {
                    while (queue.TryTake(out _)) { }
                    queue.TryAdd(blankFrame);
                }
            }
        }
    }

    public void Dispose()
    {
        StopServer();
    }
}
