using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ChurchAI.App.Services.Interfaces;
using NAudio.Wave;
using Whisper.net;

namespace ChurchAI.App.Services;

public class SpeechRecognitionService : ISpeechRecognitionService, IDisposable
{
    public event EventHandler<SpeechRecognizedEventArgs>? SpeechRecognized;
    public event EventHandler<AudioLevelUpdatedEventArgs>? AudioLevelUpdated;

    private readonly ISettingsService _settings;
    private WaveInEvent? _waveIn;
    private WhisperFactory? _whisperFactory;
    private WhisperProcessor? _processor;
    
    private readonly List<float> _audioBuffer = new();
    private DateTime _lastVoiceTime = DateTime.Now;
    private DateTime _lastProcessTime = DateTime.Now;
    private bool _isSpeaking = false;
    private volatile bool _isProcessing = false;
    private readonly object _bufferLock = new object();

    public SpeechRecognitionService(ISettingsService settings)
    {
        _settings = settings;
        // Pre-load Whisper model in the background to avoid cold start lag on first speech trigger
        Task.Run(async () =>
        {
            try
            {
                string modelPath = GetModelPath();
                await EnsureModelDownloadedAsync(modelPath);
                lock (_bufferLock)
                {
                    if (_whisperFactory == null)
                    {
                        _whisperFactory = WhisperFactory.FromPath(modelPath);
                        _processor = _whisperFactory.CreateBuilder()
                            .WithLanguage("en")
                            .WithPrompt("Genesis, Exodus, Leviticus, Numbers, Deuteronomy, Joshua, Judges, Ruth, Samuel, Kings, Chronicles, Ezra, Nehemiah, Esther, Job, Psalms, Proverbs, Ecclesiastes, Song of Solomon, Isaiah, Jeremiah, Lamentations, Ezekiel, Daniel, Hosea, Joel, Amos, Obadiah, Jonah, Micah, Nahum, Habakkuk, Zephaniah, Haggai, Zechariah, Malachi, Matthew, Mark, Luke, John, Acts, Romans, Corinthians, Galatians, Ephesians, Philippians, Colossians, Thessalonians, Timothy, Titus, Philemon, Hebrews, James, Peter, John, Jude, Revelation, chapter, verse.")
                            .Build();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to background pre-load Whisper model: {ex.Message}");
            }
        });
    }

    public bool IsListening => _waveIn != null;

    public async Task StartListeningAsync()
    {
        if (_waveIn != null) return; // Already listening

        try
        {
            string modelPath = GetModelPath();
            await EnsureModelDownloadedAsync(modelPath);

            lock (_bufferLock)
            {
                if (_whisperFactory == null)
                {
                    _whisperFactory = WhisperFactory.FromPath(modelPath);
                    _processor = _whisperFactory.CreateBuilder()
                        .WithLanguage("en")
                        .WithPrompt("Genesis, Exodus, Leviticus, Numbers, Deuteronomy, Joshua, Judges, Ruth, Samuel, Kings, Chronicles, Ezra, Nehemiah, Esther, Job, Psalms, Proverbs, Ecclesiastes, Song of Solomon, Isaiah, Jeremiah, Lamentations, Ezekiel, Daniel, Hosea, Joel, Amos, Obadiah, Jonah, Micah, Nahum, Habakkuk, Zephaniah, Haggai, Zechariah, Malachi, Matthew, Mark, Luke, John, Acts, Romans, Corinthians, Galatians, Ephesians, Philippians, Colossians, Thessalonians, Timothy, Titus, Philemon, Hebrews, James, Peter, John, Jude, Revelation, chapter, verse.")
                        .Build();
                }
            }

            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 16, 1), // Whisper expects 16kHz, 16-bit, Mono
                BufferMilliseconds = 40
            };

            _waveIn.DataAvailable += WaveIn_DataAvailable;
            _waveIn.StartRecording();
            
            SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs("[Microphone active. Whisper AI is listening!]", true));
        }
        catch (Exception ex)
        {
            SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs($"[Error starting Whisper: {ex.Message}]", true));
            Dispose();
        }
    }

    public Task StopListeningAsync()
    {
        if (_waveIn != null)
        {
            _waveIn.StopRecording();
            _waveIn.DataAvailable -= WaveIn_DataAvailable;
            _waveIn.Dispose();
            _waveIn = null;

            lock (_bufferLock)
            {
                _audioBuffer.Clear();
                _isSpeaking = false;
                _isProcessing = false;
            }
        }
        return Task.CompletedTask;
    }

    private string GetModelPath()
    {
        string filename = "ggml-base.en.bin";

        // Check 1: Executable directory
        string path1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
        if (File.Exists(path1)) return path1;

        // Check 2: Current working directory
        string path2 = Path.Combine(Directory.GetCurrentDirectory(), filename);
        if (File.Exists(path2)) return path2;

        // Check 3: Project folder relative to working directory (useful for dotnet run from root)
        string path3 = Path.Combine(Directory.GetCurrentDirectory(), "ChurchAI.App", filename);
        if (File.Exists(path3)) return path3;

        // Check 4: Base directory ancestors (useful for Visual Studio test/runner directories)
        string path4 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", filename);
        if (File.Exists(path4)) return path4;

        string path5 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "ChurchAI.App", filename);
        if (File.Exists(path5)) return path5;

        // Fallback default: Executable directory
        return path1;
    }

    private async Task EnsureModelDownloadedAsync(string modelPath)
    {
        if (!File.Exists(modelPath))
        {
            SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs("[Downloading Whisper AI model (140MB). This only happens once...]", true));
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.en.bin", HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            
            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1;
            
            // Ensure parent directory exists
            var parentDir = Path.GetDirectoryName(modelPath);
            if (!string.IsNullOrEmpty(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(modelPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
            
            var totalRead = 0L;
            var buffer = new byte[8192];
            var isMoreToRead = true;
            int lastReportedPercent = 0;
            
            do
            {
                var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    isMoreToRead = false;
                }
                else
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    totalRead += read;
                    
                    if (canReportProgress)
                    {
                        var percent = (int)((totalRead * 100) / totalBytes);
                        if (percent >= lastReportedPercent + 5) // Report every 5%
                        {
                            lastReportedPercent = percent;
                            SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs($"[Downloading model: {percent}%]", true));
                        }
                    }
                }
            } while (isMoreToRead);
            
            SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs("[Download complete! Initializing AI...]", true));
        }
    }

    private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
    {
        lock (_bufferLock)
        {
            double sumSquare = 0;
            float gain = (float)_settings.AudioGain;
            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                short sample = (short)(e.Buffer[i] | (e.Buffer[i + 1] << 8));
                float sample32 = (sample / 32768f) * gain;
                sample32 = Math.Max(-1.0f, Math.Min(1.0f, sample32));
                _audioBuffer.Add(sample32);
                
                sumSquare += sample32 * sample32;

                // Voice Activity Detection Threshold (Ignore room noise/hiss)
                if (Math.Abs(sample32) > 0.015f) 
                {
                    _lastVoiceTime = DateTime.Now;
                    _isSpeaking = true;
                }
            }

            int sampleCount = e.BytesRecorded / 2;
            double rms = sampleCount > 0 ? Math.Sqrt(sumSquare / sampleCount) : 0;
            AudioLevelUpdated?.Invoke(this, new AudioLevelUpdatedEventArgs(rms));
            
            DateTime now = DateTime.Now;
            
            // Process if silence for 1.2 seconds (Final)
            if (_isSpeaking && (now - _lastVoiceTime).TotalSeconds > 1.2)
            {
                if (_audioBuffer.Count > 16000 * 0.2 && !_isProcessing)
                {
                    _isProcessing = true;
                    var bufferToProcess = _audioBuffer.ToArray();
                    _audioBuffer.Clear();
                    _isSpeaking = false;
                    
                    Task.Run(() => ProcessAudioBuffer(bufferToProcess, true));
                }
                else if (!_isProcessing)
                {
                    _audioBuffer.Clear();
                    _isSpeaking = false;
                }
            }

            
            // Max buffer cutoff (e.g. 15 seconds) to prevent infinite memory growth
            if (_audioBuffer.Count > 16000 * 15 && !_isProcessing)
            {
                _isProcessing = true;
                var bufferToProcess = _audioBuffer.ToArray();
                _audioBuffer.Clear();
                _lastVoiceTime = now;
                _lastProcessTime = now;
                
                Task.Run(() => ProcessAudioBuffer(bufferToProcess, true));
            }
        }
    }

    private async Task ProcessAudioBuffer(float[] audioData, bool isFinal)
    {
        try
        {
            if (_processor == null) return;

            using var waveStream = CreateWaveStream(audioData);
            
            await foreach (var result in _processor.ProcessAsync(waveStream))
            {
                var text = result.Text.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    // Filter out common hallucinations Whisper does on pure noise
                    var lower = text.ToLowerInvariant();
                    if (lower.Contains("thanks for watching") || lower.Contains("subscribe") || lower.Contains("blank_audio") || lower.Contains("[ pause ]") || lower.Contains("[ silence ]"))
                    {
                        continue;
                    }
                    
                    SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs(text, isFinal));
                }
            }
        }
        catch (Exception ex)
        {
            SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs($"[Whisper Processing Error: {ex.Message}]", true));
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private MemoryStream CreateWaveStream(float[] samples)
    {
        var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true);
        
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + samples.Length * 2);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16); // Subchunk1Size
        writer.Write((short)1); // AudioFormat (PCM)
        writer.Write((short)1); // NumChannels
        writer.Write(16000); // SampleRate
        writer.Write(16000 * 2); // ByteRate
        writer.Write((short)2); // BlockAlign
        writer.Write((short)16); // BitsPerSample
        
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(samples.Length * 2);
        
        foreach (var sample in samples)
        {
            short val = (short)(Math.Clamp(sample, -1f, 1f) * 32767);
            writer.Write(val);
        }
        
        ms.Position = 0;
        return ms;
    }

    public void Dispose()
    {
        StopListeningAsync().Wait();
        _processor?.Dispose();
        _whisperFactory?.Dispose();
    }
}
