#if UNITY_EDITOR
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Services.Transport;
using UnityEditor;
using UnityEngine;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

[InitializeOnLoad]
public static class McpBridgeBootstrap
{
    private const string LocalHttpUrl = "http://127.0.0.1:8080";
    private const int LocalHttpPort = 8080;
    private static bool _startRequested;
    private static Process _localMcpServerProcess;

    static McpBridgeBootstrap()
    {
        Initialize();
    }

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        Debug.Log("[McpBridgeBootstrap] Initialize hook fired");
        EditorApplication.update += StartBridgeOnceOnUpdate;
    }

    private static void StartBridgeOnceOnUpdate()
    {
        EditorApplication.update -= StartBridgeOnceOnUpdate;
        StartBridge();
    }

    private static async void StartBridge()
    {
        Debug.Log("[McpBridgeBootstrap] StartBridge invoked");

        if (_startRequested)
        {
            Debug.Log("[McpBridgeBootstrap] StartBridge skipped (already requested)");
            return;
        }

        _startRequested = true;

        try
        {
            EditorConfigurationCache.Instance.SetUseHttpTransport(true);
            EditorConfigurationCache.Instance.SetHttpTransportScope("local");
            HttpEndpointUtility.SaveLocalBaseUrl(LocalHttpUrl);

            try
            {
                MCPServiceLocator.TransportManager.ForceStop(TransportMode.Stdio);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[McpBridgeBootstrap] Failed to stop stdio transport: {ex.Message}");
            }

            await EnsureLocalServerRunningAsync();

            bool started = false;
            const int maxAttempts = 8;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                Debug.Log($"[McpBridgeBootstrap] Starting HTTP bridge (attempt {attempt}/{maxAttempts})...");
                started = await MCPServiceLocator.Bridge.StartAsync();
                Debug.Log($"[McpBridgeBootstrap] HTTP bridge start result (attempt {attempt}): {started}");
                if (started)
                {
                    break;
                }

                await Task.Delay(1000);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[McpBridgeBootstrap] Failed to start bridge: {ex}");
        }
    }

    private static async Task EnsureLocalServerRunningAsync()
    {
        if (IsPortListening(LocalHttpPort))
        {
            return;
        }

        try
        {
            string uvxPath = Environment.GetEnvironmentVariable("UVX_PATH_OVERRIDE");
            if (string.IsNullOrWhiteSpace(uvxPath))
            {
                uvxPath = "/Users/gabrieldelcarmen/.local/bin/uvx";
            }

            string args = "--from mcpforunityserver mcp-for-unity --transport http --http-url http://127.0.0.1:8080 --project-scoped-tools";
            ProcessStartInfo psi = new()
            {
                FileName = uvxPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _localMcpServerProcess = Process.Start(psi);
            Debug.Log($"[McpBridgeBootstrap] Launched local MCP server process pid={_localMcpServerProcess?.Id ?? 0}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[McpBridgeBootstrap] Failed to launch local MCP server: {ex.Message}");
        }

        for (int i = 0; i < 10; i++)
        {
            if (IsPortListening(LocalHttpPort))
            {
                return;
            }

            await Task.Delay(500);
        }
    }

    private static bool IsPortListening(int port)
    {
        try
        {
            using TcpClient client = new();
            var connectTask = client.ConnectAsync("127.0.0.1", port);
            bool completed = connectTask.Wait(TimeSpan.FromMilliseconds(200));
            return completed && client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
#endif
