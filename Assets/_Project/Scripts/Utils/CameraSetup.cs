using DontLetThemIn.Grid;
using UnityEngine;

namespace DontLetThemIn.Utils
{
    public static class CameraSetup
    {
        public static Camera EnsureTopDownCamera(NodeGraph graph)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.05f, 0.06f);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.depthTextureMode = DepthTextureMode.None;
            camera.transform.position = new Vector3((graph.Width - 1) * 0.5f, (graph.Height - 1) * 0.5f, -10f);
            camera.transform.rotation = Quaternion.identity;

            float verticalSize = graph.Height * 0.55f;
            float horizontalSize = (graph.Width * 0.55f) / Mathf.Max(camera.aspect, 0.01f);
            camera.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
            return camera;
        }
    }
}
