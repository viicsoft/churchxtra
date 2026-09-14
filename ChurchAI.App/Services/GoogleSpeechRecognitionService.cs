using System;
using System.IO;
using System.Threading.Tasks;
using ChurchAI.App.Services.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Speech.V1;
using Grpc.Auth;
using NAudio.Wave;

namespace ChurchAI.App.Services;

public class GoogleSpeechRecognitionService : ISpeechRecognitionService, IDisposable
{
    public event EventHandler<SpeechRecognizedEventArgs>? SpeechRecognized;
    public event EventHandler<AudioLevelUpdatedEventArgs>? AudioLevelUpdated;

    private readonly ISettingsService _settings;
    private WaveInEvent? _waveIn;
    private SpeechClient? _speechClient;
    private SpeechClient.StreamingRecognizeStream? _streamingCall;
    private readonly object _streamLock = new object();
    private bool _isListening = false;

    private DateTime _streamStartTime;
    private readonly object _rotationLock = new object();
    private bool _isRotating = false;

    public GoogleSpeechRecognitionService(ISettingsService settings)
    {
        _settings = settings;
        // Pre-initialize Google SpeechClient in background thread to avoid cold start latency on first trigger
        Task.Run(() =>
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_settings.GoogleCredentialsPath) && File.Exists(_settings.GoogleCredentialsPath))
                {
                    var credential = GoogleCredential.FromFile(_settings.GoogleCredentialsPath);
                    lock (_streamLock)
                    {
                        if (_speechClient == null)
                        {
                            _speechClient = new SpeechClientBuilder
                            {
                                ChannelCredentials = credential.ToChannelCredentials()
                            }.Build();
                        }
                    }
                }
            }
            catch { }
        });
    }

    public bool IsListening => _waveIn != null && _isListening;

    public async Task StartListeningAsync()
    {
        if (_waveIn != null) return; // Already listening

        if (string.IsNullOrWhiteSpace(_settings.GoogleCredentialsPath) || !File.Exists(_settings.GoogleCredentialsPath))
        {
            SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs("[Error: Google JSON Credentials Path is missing or file does not exist! Check settings.]", true));
            return;
        }

        try
        {
            // 1. Build speech client using credentials loaded from local JSON file if not already initialized
            lock (_streamLock)
            {
                if (_speechClient == null)
                {
                    var credential = GoogleCredential.FromFile(_settings.GoogleCredentialsPath);
                    _speechClient = new SpeechClientBuilder
                    {
                        ChannelCredentials = credential.ToChannelCredentials()
                    }.Build();
                }
            }

            // 2. Open bidirectional streaming call
            _streamingCall = _speechClient.StreamingRecognize();
            _streamStartTime = DateTime.UtcNow;
            _isRotating = false;

            // 3. Send streaming config
            await _streamingCall.WriteAsync(GetStreamingConfigRequest());

            // 4. Initialize NAudio recording (16kHz Mono)
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 16, 1),
                BufferMilliseconds = 40
            };

            _waveIn.DataAvailable += WaveIn_DataAvailable;
            _waveIn.StartRecording();
            _isListening = true;

            SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs("[Cloud microphone active. Google Streaming Speech is listening...]", true));

            // 5. Start response reading loop
            _ = Task.Run(() => ReadResponsesAsync(_streamingCall));
        }
        catch (Exception ex)
        {
            SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs($"[Error starting Google Speech: {ex.Message}]", true));
            Dispose();
        }
    }

    public async Task StopListeningAsync()
    {
        if (_waveIn != null)
        {
            _waveIn.StopRecording();
            _waveIn.DataAvailable -= WaveIn_DataAvailable;
            _waveIn.Dispose();
            _waveIn = null;
        }

        _isListening = false;
        _speechClient = null;

        SpeechClient.StreamingRecognizeStream? streamToClose = null;
        lock (_streamLock)
        {
            streamToClose = _streamingCall;
            _streamingCall = null;
        }

        if (streamToClose != null)
        {
            try
            {
                await streamToClose.WriteCompleteAsync();
            }
            catch {}
        }
    }

    private StreamingRecognizeRequest GetStreamingConfigRequest()
    {
        return new StreamingRecognizeRequest
        {
            StreamingConfig = new StreamingRecognitionConfig
            {
                Config = new RecognitionConfig
                {
                    Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                    SampleRateHertz = 16000,
                    LanguageCode = "en-US",
                    SpeechContexts = {
                        new SpeechContext {
                            Phrases = {
                                "Genesis", "Exodus", "Leviticus", "Numbers", "Deuteronomy",
                                "Joshua", "Judges", "Ruth", "Samuel", "Kings", "Chronicles",
                                "Ezra", "Nehemiah", "Esther", "Job", "Psalms", "Proverbs",
                                "Ecclesiastes", "Isaiah", "Jeremiah", "Lamentations", "Ezekiel",
                                "Daniel", "Hosea", "Joel", "Amos", "Obadiah", "Jonah", "Micah",
                                "Nahum", "Habakkuk", "Zephaniah", "Haggai", "Zechariah",
                                "Malachi", "Matthew", "Mark", "Luke", "John", "Acts", "Romans",
                                "Corinthians", "Galatians", "Ephesians", "Philippians",
                                "Colossians", "Thessalonians", "Timothy", "Titus", "Philemon",
                                "Hebrews", "James", "Peter", "Jude", "Revelation",
                                "chapter", "verse", "verse 1", "verse 2", "verse 3", "verse 4", "verse 5"
                            }
                        }
                    }
                },
                InterimResults = true
            }
        };
    }

    private async Task RotateStreamAsync()
    {
        if (!_isListening || _speechClient == null) return;

        lock (_rotationLock)
        {
            if (_isRotating) return;
            _isRotating = true;
        }

        try
        {
            // 1. Establish new stream in background
            var newStreamingCall = _speechClient.StreamingRecognize();
            await newStreamingCall.WriteAsync(GetStreamingConfigRequest());

            SpeechClient.StreamingRecognizeStream? oldStreamingCall = null;
            lock (_streamLock)
            {
                oldStreamingCall = _streamingCall;
                _streamingCall = newStreamingCall;
                _streamStartTime = DateTime.UtcNow;
            }

            // 2. Start reader loop for the new stream
            _ = Task.Run(() => ReadResponsesAsync(newStreamingCall));

            // 3. Clean up the old stream asynchronously in a non-blocking way
            if (oldStreamingCall != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Wait slightly to let any final data buffer write and finalize
                        await Task.Delay(2000);
                        await oldStreamingCall.WriteCompleteAsync();
                    }
                    catch { }
                });
            }

            SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs("[Google Speech stream rotated successfully]", true));
        }
        catch (Exception ex)
        {
            SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs($"[Warning: Google Speech stream rotation failed: {ex.Message}]", true));
        }
        finally
        {
            lock (_rotationLock)
            {
                _isRotating = false;
            }
        }
    }

    private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
    {
        // 1. Calculate and publish audio visual levels
        double sumSquare = 0;
        double gain = _settings.AudioGain;
        byte[] boostedBuffer = new byte[e.BytesRecorded];

        for (int i = 0; i < e.BytesRecorded; i += 2)
        {
            short sample = (short)(e.Buffer[i] | (e.Buffer[i + 1] << 8));
            double normalized = (sample / 32768f) * gain;
            normalized = Math.Max(-1.0, Math.Min(1.0, normalized));

            short boostedSample = (short)(normalized * 32767);
            boostedBuffer[i] = (byte)(boostedSample & 0xFF);
            boostedBuffer[i + 1] = (byte)((boostedSample >> 8) & 0xFF);

            sumSquare += normalized * normalized;
        }

        int sampleCount = e.BytesRecorded / 2;
        double rms = sampleCount > 0 ? Math.Sqrt(sumSquare / sampleCount) : 0;
        AudioLevelUpdated?.Invoke(this, new AudioLevelUpdatedEventArgs(rms));

        // 2. Stream audio bytes directly to Google Cloud
        SpeechClient.StreamingRecognizeStream? stream = null;
        double elapsedSeconds = 0;
        lock (_streamLock)
        {
            stream = _streamingCall;
            if (stream != null)
            {
                elapsedSeconds = (DateTime.UtcNow - _streamStartTime).TotalSeconds;
            }
        }

        // Fallback rotation: if stream has run for > 300 seconds (long silence), rotate anyway
        if (elapsedSeconds > 300)
        {
            _ = RotateStreamAsync();
        }

        if (stream != null)
        {
            try
            {
                // Write synchronously on background thread to prevent thread-safety out-of-order issues
                stream.WriteAsync(new StreamingRecognizeRequest
                {
                    AudioContent = Google.Protobuf.ByteString.CopyFrom(boostedBuffer)
                }).GetAwaiter().GetResult();
            }
            catch
            {
                // Stream might have completed or errored out
            }
        }
    }

    private async Task ReadResponsesAsync(SpeechClient.StreamingRecognizeStream stream)
    {
        try
        {
            while (await stream.GrpcCall.ResponseStream.MoveNext(default))
            {
                var response = stream.GrpcCall.ResponseStream.Current;
                foreach (var result in response.Results)
                {
                    if (result.Alternatives.Count > 0)
                    {
                        var transcript = result.Alternatives[0].Transcript;
                        var isFinal = result.IsFinal;

                        SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs(transcript, isFinal));

                        // Smart Rotation check on final speech chunks
                        if (isFinal)
                        {
                            double elapsed = 0;
                            lock (_streamLock)
                            {
                                // Only trigger if this is the active stream
                                if (stream == _streamingCall)
                                {
                                    elapsed = (DateTime.UtcNow - _streamStartTime).TotalSeconds;
                                }
                            }

                            if (elapsed > 280) // 4 minutes and 40 seconds
                            {
                                _ = RotateStreamAsync();
                            }
                        }
                    }
                }
            }
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled)
        {
            // Ignore normal stream cancellation when microphone is stopped
        }
        catch (Exception ex)
        {
            // Only report connection errors if this is the active stream and we're not actively rotating it
            bool isActive = false;
            lock (_streamLock)
            {
                isActive = (stream == _streamingCall);
            }

            if (isActive)
            {
                SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs($"[Google Speech Connection Error: {ex.Message}]", true));
                _ = StopListeningAsync();
            }
        }
    }

    public void Dispose()
    {
        StopListeningAsync().Wait();
    }
}
