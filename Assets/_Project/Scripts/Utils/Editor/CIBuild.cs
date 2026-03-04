#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace DontLetThemIn.Utils.Editor
{
    public static class CIBuild
    {
        public static void BuildStandaloneOSX()
        {
            Stage1ProjectSetupEditor.EnsureProjectSetup();

            BuildPlayerOptions options = new()
            {
                scenes = Stage1ProjectSetupEditor.GetScenePaths().ToArray(),
                locationPathName = "Builds/StandaloneOSX/DontLetThemIn.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.StrictMode
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"StandaloneOSX build failed: {report.summary.result}");
            }
        }
    }
}
#endif
