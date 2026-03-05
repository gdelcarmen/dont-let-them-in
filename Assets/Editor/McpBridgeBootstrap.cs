#if UNITY_EDITOR
using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Services.Transport;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class McpBridgeBootstrap
{
    private const string LocalHttpUrl = "http://127.0.0.1:8080";
    private static bool _startRequested;

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

            Debug.Log("[McpBridgeBootstrap] Starting HTTP bridge...");
            bool started = await MCPServiceLocator.Bridge.StartAsync();
            Debug.Log($"[McpBridgeBootstrap] HTTP bridge start result: {started}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[McpBridgeBootstrap] Failed to start bridge: {ex}");
        }
    }
}
#endif
