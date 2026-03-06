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
            camera.backgroundColor = new Color(0.03f, 0.04f, 0.06f);
            camera.allowHDR = true;
            camera.allowMSAA = false;
            camera.depthTextureMode = DepthTextureMode.Depth;
            camera.transparencySortMode = TransparencySortMode.CustomAxis;
            camera.transparencySortAxis = new Vector3(0f, 1f, 0f);
            camera.transform.position = new Vector3((graph.Width - 1) * 0.5f, (graph.Height - 1) * 0.5f - 0.8f, -10f);
            camera.transform.rotation = Quaternion.Euler(8f, 0f, 0f);

            float verticalSize = graph.Height * 0.55f;
            float horizontalSize = (graph.Width * 0.55f) / Mathf.Max(camera.aspect, 0.01f);
            camera.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
            return camera;
        }
    }
}
