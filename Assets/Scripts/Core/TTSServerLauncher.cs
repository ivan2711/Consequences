using UnityEngine;
using System.Diagnostics;
using System.IO;

/// <summary>
/// Automatically launches the TTS server on app start and kills it on quit.
/// The server executable and model files are expected in StreamingAssets/TTS/.
/// </summary>
public class TTSServerLauncher : MonoBehaviour
{
    private static TTSServerLauncher _instance;
    private Process _serverProcess;
    private bool _serverReady;

    // TTS disabled for NAS build — re-enable when backend is ready
    // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    // static void AutoCreate()
    // {
    //     if (_instance != null) return;
    //     var go = new GameObject("TTSServerLauncher");
    //     _instance = go.AddComponent<TTSServerLauncher>();
    //     DontDestroyOnLoad(go);
    // }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        LaunchServer();
    }

    void LaunchServer()
    {
        // Kill any leftover server from a previous session
        try
        {
            var killPsi = new ProcessStartInfo
            {
                FileName = "/bin/sh",
                Arguments = "-c \"lsof -ti :7860 | xargs kill -9 2>/dev/null\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            var p = Process.Start(killPsi);
            p.WaitForExit(2000);
        }
        catch { }

        string ttsDir = Path.Combine(Application.streamingAssetsPath, "TTS");
        string serverPath;

#if UNITY_EDITOR
        // In editor, run the Python server directly
        ttsDir = Path.Combine(Application.dataPath, "..", "TTS");
        string venvPython = Path.Combine(ttsDir, "venv", "bin", "python");
        if (!File.Exists(venvPython))
            venvPython = Path.Combine(ttsDir, "venv", "Scripts", "python.exe"); // Windows
        serverPath = Path.Combine(ttsDir, "server.py");

        if (!File.Exists(serverPath))
        {
            UnityEngine.Debug.LogWarning("[TTS] server.py not found at " + serverPath);
            return;
        }

        StartProcess(venvPython, "\"" + serverPath + "\"", ttsDir);
#else
        // In build, run the bundled executable
        if (Application.platform == RuntimePlatform.WindowsPlayer)
            serverPath = Path.Combine(ttsDir, "tts_server.exe");
        else
            serverPath = Path.Combine(ttsDir, "tts_server");

        if (!File.Exists(serverPath))
        {
            UnityEngine.Debug.LogWarning("[TTS] Server executable not found at " + serverPath);
            return;
        }

        StartProcess(serverPath, "", ttsDir);
#endif
    }

    void StartProcess(string fileName, string arguments, string workingDir)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
            };

            // Ensure sox and other tools are findable
            string path = System.Environment.GetEnvironmentVariable("PATH") ?? "";
            psi.EnvironmentVariables["PATH"] = "/opt/homebrew/bin:/usr/local/bin:" + path;

            _serverProcess = Process.Start(psi);
            UnityEngine.Debug.Log("[TTS] Server launched PID=" + _serverProcess.Id + ": " + fileName + " " + arguments);
            StartCoroutine(MonitorServer());
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("[TTS] Failed to launch server: " + e.Message);
        }
    }

    System.Collections.IEnumerator MonitorServer()
    {
        yield return new WaitForSeconds(2f);
        if (_serverProcess == null)
        {
            UnityEngine.Debug.LogError("[TTS] Server process is null after launch");
            yield break;
        }
        if (_serverProcess.HasExited)
        {
            UnityEngine.Debug.LogError("[TTS] Server exited immediately with code: " + _serverProcess.ExitCode);
        }
        else
        {
            UnityEngine.Debug.Log("[TTS] Server is running (PID=" + _serverProcess.Id + ")");
        }
    }

    void OnApplicationQuit()
    {
        KillServer();
    }

    void OnDestroy()
    {
        KillServer();
    }

    void KillServer()
    {
        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            try
            {
                int pid = _serverProcess.Id;
                // Kill entire process tree (children too)
                var killPsi = new ProcessStartInfo
                {
                    FileName = "/bin/kill",
                    Arguments = "-9 -" + pid, // negative PID kills process group
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                try { Process.Start(killPsi); } catch { }

                _serverProcess.Kill();
                _serverProcess.WaitForExit(3000);
                UnityEngine.Debug.Log("[TTS] Server stopped.");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning("[TTS] Error stopping server: " + e.Message);
            }
            _serverProcess = null;
        }

        // Also kill anything still on port 7860
        try
        {
            var lsofPsi = new ProcessStartInfo
            {
                FileName = "/bin/sh",
                Arguments = "-c \"lsof -ti :7860 | xargs kill -9 2>/dev/null\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            Process.Start(lsofPsi);
        }
        catch { }
    }
}
