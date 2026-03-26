using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class TTSService : MonoBehaviour
{
    private static TTSService _instance;
    public static TTSService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TTSService>();
                if (_instance == null)
                {
                    var go = new GameObject("TTSService");
                    _instance = go.AddComponent<TTSService>();
                }
            }
            return _instance;
        }
    }

    [Header("Server")]
    public string serverUrl = "http://localhost:7860";

    [Header("Settings")]
    public bool enabled = true;

    private AudioSource _audioSource;
    private Coroutine _currentRequest;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
    }

    /// <summary>
    /// Speak the given text. Stops any currently playing speech.
    /// </summary>
    public void Speak(string text)
    {
        if (!enabled || string.IsNullOrWhiteSpace(text)) return;

        if (_currentRequest != null)
            StopCoroutine(_currentRequest);

        _audioSource.Stop();
        _currentRequest = StartCoroutine(FetchAndPlay(text));
    }

    /// <summary>
    /// Speak text with a callback when done.
    /// </summary>
    public void Speak(string text, Action onComplete)
    {
        if (!enabled || string.IsNullOrWhiteSpace(text))
        {
            onComplete?.Invoke();
            return;
        }

        if (_currentRequest != null)
            StopCoroutine(_currentRequest);

        _audioSource.Stop();
        _currentRequest = StartCoroutine(FetchAndPlay(text, onComplete));
    }

    /// <summary>
    /// Stop any currently playing speech.
    /// </summary>
    public void Stop()
    {
        if (_currentRequest != null)
            StopCoroutine(_currentRequest);
        _audioSource.Stop();
    }

    public bool IsSpeaking => _audioSource != null && _audioSource.isPlaying;

    private IEnumerator FetchAndPlay(string text, Action onComplete = null)
    {
        string url = $"{serverUrl}/tts?text={UnityWebRequest.EscapeURL(text)}";
        Debug.Log("[TTS] Requesting: " + url);

        using (var request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
        {
            request.timeout = 60; // Model inference can take 5-20s
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[TTS] Request failed: " + request.error + " (HTTP " + request.responseCode + ")");
                onComplete?.Invoke();
                yield break;
            }
            Debug.Log("[TTS] Audio received, " + request.downloadedBytes + " bytes");

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip == null || clip.length == 0)
            {
                Debug.LogWarning("[TTS] Received empty audio clip");
                onComplete?.Invoke();
                yield break;
            }

            _audioSource.clip = clip;
            _audioSource.Play();

            if (onComplete != null)
            {
                yield return new WaitWhile(() => _audioSource.isPlaying);
                onComplete.Invoke();
            }
        }

        _currentRequest = null;
    }

    /// <summary>
    /// Check if the TTS server is reachable.
    /// </summary>
    public void CheckHealth(Action<bool> callback)
    {
        StartCoroutine(HealthCheck(callback));
    }

    private IEnumerator HealthCheck(Action<bool> callback)
    {
        using (var request = UnityWebRequest.Get($"{serverUrl}/health"))
        {
            request.timeout = 3;
            yield return request.SendWebRequest();
            callback?.Invoke(request.result == UnityWebRequest.Result.Success);
        }
    }
}
