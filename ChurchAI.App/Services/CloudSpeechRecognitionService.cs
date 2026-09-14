using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using ChurchAI.App.Services.Interfaces;
using NAudio.Wave;

namespace ChurchAI.App.Services;

public class CloudSpeechRecognitionService : ISpeechRecognitionService, IDisposable
{
    public event EventHandler<SpeechRecognizedEventArgs>? SpeechRecognized;
    public event EventHandler<AudioLevelUpdatedEventArgs>? AudioLevelUpdated;

    private readonly ISettingsService _settings;
    private static readonly HttpClient _httpClient = new HttpClient();
    private WaveInEvent? _waveIn;
    private readonly MemoryStream _rawPCMBuffer = new();
    private DateTime _lastVoiceTime = DateTime.Now;
    private DateTime _lastIntermediateTime = DateTime.Now;
    private bool _isSpeaking = false;
    private volatile bool _isProcessing = false;
    private int _currentUtteranceId = 0;
    private bool _isFinalProcessedForCurrentUtterance = false;
    private readonly object _bufferLock = new object();
    private System.Threading.CancellationTokenSource? _cts;

    public CloudSpeechRecognitionService(ISettingsService settings)
    {
        _settings = settings;
    }

    public bool IsListening => _waveIn != null;

    public Task StartListeningAsync()
    {
        if (_waveIn != null) return Task.CompletedTask;
        if (string.IsNullOrWhiteSpace(_settings.CloudApiKey))
        {
            SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs("[Error: Cloud API Key is missing! Please add it in settings.]", true));
            return Task.CompletedTask;
        }

        try
        {
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 16, 1) // 16kHz Mono
            };

            _rawPCMBuffer.SetLength(0);
            _rawPCMBuffer.Position = 0;
            _lastIntermediateTime = DateTime.Now;

            _waveIn.DataAvailable += WaveIn_DataAvailable;
            _waveIn.StartRecording();
            
            string providerName = GetProviderName();
            SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs($"[Cloud Microphone active. {providerName} is listening!]", true));
        }
        catch (Exception ex)
        {
            SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs($"[Error starting mic: {ex.Message}]", true));
            Dispose();
        }
        
        return Task.CompletedTask;
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
                try
                {
                    _cts?.Cancel();
                    _cts?.Dispose();
                    _cts = null;
                }
                catch {}
                _rawPCMBuffer.SetLength(0);
                _rawPCMBuffer.Position = 0;
                _isSpeaking = false;
                _isProcessing = false;
                _isFinalProcessedForCurrentUtterance = false;
            }
        }
        return Task.CompletedTask;
    }

    private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
    {
        lock (_bufferLock)
        {
            if (_waveIn == null) return;
            
            bool hasVoice = false;
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
                if (Math.Abs(normalized) > 0.015f) 
                {
                    hasVoice = true;
                }
            }

            int sampleCount = e.BytesRecorded / 2;
            double rms = sampleCount > 0 ? Math.Sqrt(sumSquare / sampleCount) : 0;
            AudioLevelUpdated?.Invoke(this, new AudioLevelUpdatedEventArgs(rms));

            if (hasVoice || _isSpeaking)
            {
                _rawPCMBuffer.Write(boostedBuffer, 0, e.BytesRecorded);
            }

            if (hasVoice)
            {
                _lastVoiceTime = DateTime.Now;
                if (!_isSpeaking)
                {
                    _isSpeaking = true;
                    _currentUtteranceId++;
                    _isFinalProcessedForCurrentUtterance = false;
                    _isProcessing = false; // Reset intermediate lock for the new utterance
                    _lastIntermediateTime = DateTime.Now;
                }
            }

            // 1. Force finalize if maximum voice duration exceeded (e.g. continuous loud noise/speaking > 8s)
            if (_isSpeaking && _rawPCMBuffer.Length > 16000 * 2 * 8)
            {
                byte[] audioData;
                int utteranceId = 0;
                
                lock (_bufferLock)
                {
                    using (var tempStream = new MemoryStream())
                    {
                        using (var tempWriter = new WaveFileWriter(tempStream, _waveIn.WaveFormat))
                        {
                            var pcmData = _rawPCMBuffer.ToArray();
                            tempWriter.Write(pcmData, 0, pcmData.Length);
                        }
                        audioData = tempStream.ToArray();
                    }

                    _rawPCMBuffer.SetLength(0);
                    _rawPCMBuffer.Position = 0;
                    _isSpeaking = false;
                    _isFinalProcessedForCurrentUtterance = true;
                    utteranceId = _currentUtteranceId;
                }
                
                Task.Run(() => ProcessAudioBufferAsync(audioData, true, utteranceId));
            }
            // 2. Silence detected: finalize and reset buffer immediately
            else if (_isSpeaking && (DateTime.Now - _lastVoiceTime).TotalSeconds > 1.2)
            {
                byte[]? audioData = null;
                int utteranceId = 0;
                
                lock (_bufferLock)
                {
                    if (_rawPCMBuffer.Length > 16000 * 2 * 0.2)
                    {
                        using (var tempStream = new MemoryStream())
                        {
                            using (var tempWriter = new WaveFileWriter(tempStream, _waveIn.WaveFormat))
                            {
                                var pcmData = _rawPCMBuffer.ToArray();
                                tempWriter.Write(pcmData, 0, pcmData.Length);
                            }
                            audioData = tempStream.ToArray();
                        }
                    }

                    _rawPCMBuffer.SetLength(0);
                    _rawPCMBuffer.Position = 0;
                    _isSpeaking = false;
                    _isFinalProcessedForCurrentUtterance = true;
                    utteranceId = _currentUtteranceId;
                }

                if (audioData != null)
                {
                    Task.Run(() => ProcessAudioBufferAsync(audioData, true, utteranceId));
                }
            }
            // 3. Intermediate: check transcript periodically (every 1.5 seconds) while speaking
            else if (_isSpeaking && !_isProcessing && (DateTime.Now - _lastIntermediateTime).TotalSeconds > 1.5)
            {
                _lastIntermediateTime = DateTime.Now;
                
                if (_rawPCMBuffer.Length > 16000 * 2 * 0.5)
                {
                    byte[] audioData;
                    int utteranceId = 0;
                    
                    lock (_bufferLock)
                    {
                        using (var tempStream = new MemoryStream())
                        {
                            using (var tempWriter = new WaveFileWriter(tempStream, _waveIn.WaveFormat))
                            {
                                var pcmData = _rawPCMBuffer.ToArray();
                                tempWriter.Write(pcmData, 0, pcmData.Length);
                            }
                            audioData = tempStream.ToArray();
                        }
                        
                        _isProcessing = true;
                        utteranceId = _currentUtteranceId;
                    }
                    
                    Task.Run(() => ProcessAudioBufferAsync(audioData, false, utteranceId));
                }
            }
        }
    }

    private async Task ProcessAudioBufferAsync(byte[] audioData, bool isFinal, int utteranceId)
    {
        System.Threading.CancellationToken token;
        lock (_bufferLock)
        {
            try
            {
                _cts?.Cancel();
                _cts?.Dispose();
            }
            catch {}
            _cts = new System.Threading.CancellationTokenSource();
            token = _cts.Token;
        }

        try
        {
            using var content = new MultipartFormDataContent();
            
            var audioContent = new ByteArrayContent(audioData);
            audioContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
            content.Add(audioContent, "file", "audio.wav");
            content.Add(new StringContent(_settings.SpeechApiModel), "model");
            content.Add(new StringContent("en"), "language");
            
            // Guide the model to favor biblical book names and reference formats
            string biblePrompt = "Scripture references: Genesis, Exodus, Leviticus, Numbers, Deuteronomy, Joshua, Judges, Ruth, Samuel, Kings, Chronicles, Ezra, Nehemiah, Esther, Job, Psalms, Proverbs, Ecclesiastes, Isaiah, Jeremiah, Lamentations, Ezekiel, Daniel, Hosea, Joel, Amos, Obadiah, Jonah, Micah, Nahum, Habakkuk, Zephaniah, Haggai, Zechariah, Malachi, Matthew, Mark, Luke, John, Acts, Romans, Corinthians, Galatians, Ephesians, Philippians, Colossians, Thessalonians, Timothy, Titus, Philemon, Hebrews, James, Peter, Jude, Revelation. Chapter, verse.";
            content.Add(new StringContent(biblePrompt), "prompt");
            
            var request = new HttpRequestMessage(HttpMethod.Post, _settings.SpeechApiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.CloudApiKey);
            request.Content = content;
 
            var response = await _httpClient.SendAsync(request, token);
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(responseBody);
                var text = document.RootElement.GetProperty("text").GetString()?.Trim();
                
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var lower = text.ToLowerInvariant();
                    if (!lower.Contains("thanks for watching") && !lower.Contains("subscribe") && !lower.Contains("blank_audio"))
                    {
                        lock (_bufferLock)
                        {
                            // Discard if the user started a new utterance since this request was sent (never discard final results)
                            if (utteranceId != _currentUtteranceId && !isFinal) return;

                            // Discard intermediate transcripts if the final transcript has already been triggered
                            if (!isFinal && _isFinalProcessedForCurrentUtterance) return;
                        }

                        SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs(text, isFinal));
                    }
                }
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                string providerName = GetProviderName();
                SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs($"[{providerName} API Error: {(int)response.StatusCode} {response.ReasonPhrase}. Response: {errorBody}]", isFinal));
            }
        }
        catch (System.OperationCanceledException)
        {
            // Silently ignore cancelled requests
        }
        catch (Exception ex)
        {
            string providerName = GetProviderName();
            SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs($"[{providerName} Error: {ex.Message}]", isFinal));
        }
        finally
        {
            if (!isFinal)
            {
                _isProcessing = false;
            }
        }
    }

    private string GetProviderName()
    {
        var url = _settings.SpeechApiUrl?.ToLowerInvariant() ?? "";
        if (url.Contains("openai.com")) return "OpenAI Whisper";
        if (url.Contains("groq.com")) return "Groq Whisper";
        if (url.Contains("aquavoice.com")) return "Aqua Voice";
        return "Cloud Whisper";
    }

    public void Dispose()
    {
        StopListeningAsync().Wait();
        _rawPCMBuffer?.Dispose();
        lock (_bufferLock)
        {
            try
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
            }
            catch {}
        }
    }
}
