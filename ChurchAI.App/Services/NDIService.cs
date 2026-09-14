using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChurchAI.App.Services.Interfaces;
using NewTek;
using NewTek.NDI;

namespace ChurchAI.App.Services;

public class NDIService : INDIService, IDisposable
{
    private Sender? _ndiSender;
    private Window? _captureWindow;
    private bool _isBroadcasting;
    private readonly ISettingsService _settings;
    private CancellationTokenSource? _captureCts;

    public bool IsBroadcasting => _isBroadcasting;

    static NDIService()
    {
        EnsureNDIDllPathLoaded();
    }

    public NDIService(ISettingsService settings)
    {
        _settings = settings;
    }

    private static void LogDebug(string message)
    {
        try
        {
            var logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChurchAI", "ndi_debug.log");
            System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
        }
        catch { }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool SetDllDirectory(string lpPathName);

    private static void EnsureNDIConfig()
    {
        try
        {
            string configJson = @"{
  ""ndi"": {
    ""networks"": {
      ""ips"": ""127.0.0.1,192.168.0.0/16,10.0.0.0/8""
    },
    ""find"": {
      ""local"": true
    }
  }
}";
            string localPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NDI", "ndi-config.v1.json");
            string progDataPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "NDI", "ndi-config.v1.json");

            var localDir = System.IO.Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(localDir) && !System.IO.Directory.Exists(localDir))
            {
                System.IO.Directory.CreateDirectory(localDir);
            }
            if (!System.IO.File.Exists(localPath))
            {
                System.IO.File.WriteAllText(localPath, configJson);
            }

            var progDir = System.IO.Path.GetDirectoryName(progDataPath);
            if (!string.IsNullOrEmpty(progDir) && !System.IO.Directory.Exists(progDir))
            {
                System.IO.Directory.CreateDirectory(progDir);
            }
            if (!System.IO.File.Exists(progDataPath))
            {
                System.IO.File.WriteAllText(progDataPath, configJson);
            }
        }
        catch { }
    }

    private static bool _dllPathLoaded = false;

    private static void EnsureNDIDllPathLoaded()
    {
        if (_dllPathLoaded) return;

        EnsureNDIConfig();
        
        string[] candidates = new[]
        {
            @"C:\Program Files\NDI\NDI 6 Runtime\v6",
            @"C:\Program Files\NDI\NDI 6 Tools\Runtime",
            @"C:\Program Files\NDI\NDI 5 Runtime\v5",
            @"C:\Program Files\NewTek\NDI 5 Runtime\v5",
            Environment.GetEnvironmentVariable("NDI_RUNTIME_DIR_V6") ?? "",
            Environment.GetEnvironmentVariable("NDI_RUNTIME_DIR_V5") ?? "",
            AppDomain.CurrentDomain.BaseDirectory
        };

        foreach (var path in candidates)
        {
            if (!string.IsNullOrWhiteSpace(path) && System.IO.Directory.Exists(path))
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(path, "Processing.NDI.Lib.x64.dll")) || 
                    System.IO.File.Exists(System.IO.Path.Combine(path, "Processing.NDI.Lib.x86.dll")))
                {
                    SetDllDirectory(path);
                    LogDebug($"Loaded NDI DLL search path: {path}");
                    break;
                }
            }
        }

        try
        {
            bool initResult = NDIlib.initialize();
            LogDebug($"NDIlib.initialize() result: {initResult}");
        }
        catch (Exception ex)
        {
            LogDebug($"NDIlib.initialize() error: {ex.GetType().Name} - {ex.Message}");
        }

        _dllPathLoaded = true;
    }

    public void StartBroadcasting(Window windowToCapture)
    {
        LogDebug($"StartBroadcasting requested. Window null? {windowToCapture == null}. EnableNDIOutput: {_settings.EnableNDIOutput}");

        if ((_isBroadcasting && _ndiSender != null) || windowToCapture == null) return;
        
        // Only start if enabled in settings
        if (!_settings.EnableNDIOutput) return;

        EnsureNDIDllPathLoaded();

        _captureWindow = windowToCapture;
        _isBroadcasting = true;

        var sourceName = string.IsNullOrWhiteSpace(_settings.NDISourceName) 
            ? "ChurchXtra AI Projection" 
            : _settings.NDISourceName;

        LogDebug($"Attempting to create NDI Sender with SourceName: '{sourceName}'");

        try
        {
            // Create an NDI sender registered with explicit SourceName (clockVideo: false for WPF frame loop)
            _ndiSender = new Sender(sourceName, false, false);
            LogDebug($"NDI Sender created successfully with name '{sourceName}'! Object null? {_ndiSender == null}");
            
            // Start background capture loop
            _captureCts = new CancellationTokenSource();
            var token = _captureCts.Token;
            Task.Run(() => CaptureLoopAsync(token), token);
            LogDebug("Background loop started for CaptureFrame at ~30 FPS.");
        }
        catch (DllNotFoundException ex)
        {
            LogDebug($"NDI Runtime native DLL not found: {ex.Message}");
            StopBroadcasting();
        }
        catch (TypeInitializationException ex)
        {
            LogDebug($"NDI native initialization failed: {ex.Message}");
            StopBroadcasting();
        }
        catch (Exception ex)
        {
            LogDebug($"Failed to start NDI Sender: {ex.GetType().Name} - {ex.Message}");
            StopBroadcasting();
        }
    }

    public void StopBroadcasting()
    {
        LogDebug("StopBroadcasting called.");
        if (!_isBroadcasting) return;

        if (_captureCts != null)
        {
            try { _captureCts.Cancel(); } catch { }
            _captureCts = null;
        }

        _isBroadcasting = false;
        
        if (_ndiSender != null)
        {
            try
            {
                _ndiSender.Dispose();
            }
            catch (Exception ex)
            {
                LogDebug($"Error disposing Sender: {ex.Message}");
            }
            _ndiSender = null;
        }
        
        _captureWindow = null;
    }

    private bool _isSendingFrame = false;
    private int _frameCount = 0;
    private RenderTargetBitmap? _cachedRtb;
    private byte[]? _pixelBuffer;
    private int _cachedWidth = 0;
    private int _cachedHeight = 0;

    private async Task CaptureLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _isBroadcasting)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher != null && _captureWindow != null)
                {
                    await dispatcher.InvokeAsync(() =>
                    {
                        CaptureFrameOnUIThread();
                    }, System.Windows.Threading.DispatcherPriority.Render, token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LogDebug($"CaptureLoop Exception: {ex.Message}");
            }

            var elapsedMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            var delayMs = Math.Max(5, 33 - elapsedMs);

            try
            {
                await Task.Delay(delayMs, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void CaptureFrameOnUIThread()
    {
        if (_captureWindow == null || !_isBroadcasting) return;

        if (_ndiSender == null)
        {
            try
            {
                var sourceName = string.IsNullOrWhiteSpace(_settings.NDISourceName) 
                    ? "ChurchXtra AI Projection" 
                    : _settings.NDISourceName;
                EnsureNDIDllPathLoaded();
                _ndiSender = new Sender(sourceName, false, false);
                LogDebug("Re-created NDI Sender in CaptureFrame.");
            }
            catch (Exception ex)
            {
                LogDebug($"Failed to re-create NDI Sender: {ex.Message}");
                return;
            }
        }
        
        _frameCount++;
        if (_frameCount % 300 == 1) // Log every 10 seconds
        {
            try
            {
                LogDebug($"CaptureFrame tick #{_frameCount}. Sender connections: {_ndiSender.Connections}");
            }
            catch (Exception ex)
            {
                LogDebug($"Error reading Sender.Connections: {ex.Message}");
            }
        }
        
        if (_isSendingFrame) return; // Prevent frame backlog

        IntPtr frameBuffer = IntPtr.Zero;
        bool taskDispatched = false;

        try
        {
            int width = _captureWindow.ActualWidth > 0 ? (int)_captureWindow.ActualWidth : 1920;
            int height = _captureWindow.ActualHeight > 0 ? (int)_captureWindow.ActualHeight : 1080;

            var stride = width * 4;
            var bufferSize = stride * height;

            if (_cachedRtb == null || _cachedWidth != width || _cachedHeight != height || _pixelBuffer == null || _pixelBuffer.Length != bufferSize)
            {
                _cachedWidth = width;
                _cachedHeight = height;
                _cachedRtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                _pixelBuffer = new byte[bufferSize];
            }

            // Hide local laptop controls so NDI receives clean broadcast frame
            var localControls = _captureWindow.FindName("LocalControlsOverlay") as UIElement;
            var lineEditor = _captureWindow.FindName("LineEditorOverlay") as UIElement;

            var localVis = localControls?.Visibility ?? Visibility.Collapsed;
            var lineVis = lineEditor?.Visibility ?? Visibility.Collapsed;

            if (localControls != null) localControls.Visibility = Visibility.Collapsed;
            if (lineEditor != null) lineEditor.Visibility = Visibility.Collapsed;

            double currentX = 0, currentY = 0;
            double currentScaleX = 1.0, currentScaleY = 1.0;
            var ltTransform = _captureWindow.FindName("LtTranslateTransform") as TranslateTransform;
            var ltScale = _captureWindow.FindName("LtScaleTransform") as ScaleTransform;
            var projWin = _captureWindow as Views.ProjectionWindow;
            bool wasPendingCommit = projWin?.IsPositionPendingCommit ?? false;

            if (projWin != null && wasPendingCommit)
            {
                if (ltTransform != null)
                {
                    currentX = ltTransform.X;
                    currentY = ltTransform.Y;
                    ltTransform.X = projWin.CommittedX;
                    ltTransform.Y = projWin.CommittedY;
                }
                if (ltScale != null)
                {
                    currentScaleX = ltScale.ScaleX;
                    currentScaleY = ltScale.ScaleY;
                    ltScale.ScaleX = projWin.CommittedScaleX;
                    ltScale.ScaleY = projWin.CommittedScaleY;
                }
            }

            _cachedRtb.Clear();
            _cachedRtb.Render(_captureWindow);

            if (projWin != null && wasPendingCommit)
            {
                if (ltTransform != null)
                {
                    ltTransform.X = currentX;
                    ltTransform.Y = currentY;
                }
                if (ltScale != null)
                {
                    ltScale.ScaleX = currentScaleX;
                    ltScale.ScaleY = currentScaleY;
                }
            }

            if (localControls != null) localControls.Visibility = localVis;
            if (lineEditor != null) lineEditor.Visibility = lineVis;

            _cachedRtb.CopyPixels(new Int32Rect(0, 0, width, height), _pixelBuffer, stride, 0);

            frameBuffer = Marshal.AllocHGlobal(bufferSize);
            Marshal.Copy(_pixelBuffer, 0, frameBuffer, bufferSize);

            _isSendingFrame = true;
            var currentSender = _ndiSender;

            Task.Run(() =>
            {
                try
                {
                    using (var videoFrame = new VideoFrame(frameBuffer, width, height, stride, NDIlib.FourCC_type_e.FourCC_type_BGRA, (float)width / height, 30000, 1000, NDIlib.frame_format_type_e.frame_format_type_progressive))
                    {
                        currentSender?.Send(videoFrame);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"NDI Send Error: {ex.Message}");
                }
                finally
                {
                    if (frameBuffer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(frameBuffer);
                    }
                    _isSendingFrame = false;
                }
            });

            taskDispatched = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NDI Capture Error: {ex.Message}");
        }
        finally
        {
            if (!taskDispatched && frameBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(frameBuffer);
            }
        }
    }

    public void Dispose()
    {
        StopBroadcasting();
    }
}
