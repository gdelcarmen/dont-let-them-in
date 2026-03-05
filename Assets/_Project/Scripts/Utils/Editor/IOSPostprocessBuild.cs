#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace DontLetThemIn.Utils.Editor
{
    public static class IOSPostprocessBuild
    {
        [PostProcessBuild(1000)]
        public static void ApplyInfoPlistOverrides(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            string infoPlistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            if (!File.Exists(infoPlistPath))
            {
                Debug.LogWarning($"[IOSPostprocessBuild] Info.plist not found at {infoPlistPath}");
                return;
            }

            XDocument document = XDocument.Load(infoPlistPath);
            XElement rootDict = document.Root?.Element("dict");
            if (rootDict == null)
            {
                Debug.LogWarning("[IOSPostprocessBuild] Could not parse root dict in Info.plist.");
                return;
            }

            // Requirement: iPhone portrait only, iPad landscape left/right.
            SetStringArray(rootDict, "UISupportedInterfaceOrientations", new[]
            {
                "UIInterfaceOrientationPortrait"
            });

            SetStringArray(rootDict, "UISupportedInterfaceOrientations~ipad", new[]
            {
                "UIInterfaceOrientationLandscapeLeft",
                "UIInterfaceOrientationLandscapeRight"
            });

            document.Save(infoPlistPath);
            NormalizePlistDoctype(infoPlistPath);
            ApplyCustomLaunchStoryboard(pathToBuiltProject);
            Debug.Log("[IOSPostprocessBuild] Applied orientation overrides to Info.plist.");
        }

        private static void SetStringArray(XElement dict, string key, IReadOnlyList<string> values)
        {
            XElement[] children = dict.Elements().ToArray();
            for (int i = 0; i < children.Length; i++)
            {
                XElement node = children[i];
                if (node.Name != "key" || node.Value != key)
                {
                    continue;
                }

                XElement existingValue = i + 1 < children.Length ? children[i + 1] : null;
                existingValue?.Remove();
                node.AddAfterSelf(BuildArrayElement(values));
                return;
            }

            dict.Add(new XElement("key", key));
            dict.Add(BuildArrayElement(values));
        }

        private static XElement BuildArrayElement(IReadOnlyList<string> values)
        {
            XElement array = new("array");
            foreach (string value in values)
            {
                array.Add(new XElement("string", value));
            }

            return array;
        }

        private static void NormalizePlistDoctype(string infoPlistPath)
        {
            string contents = File.ReadAllText(infoPlistPath);
            const string invalid = "\"http://www.apple.com/DTDs/PropertyList-1.0.dtd\"[]>";
            const string valid = "\"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">";
            if (contents.Contains(invalid))
            {
                contents = contents.Replace(invalid, valid);
                File.WriteAllText(infoPlistPath, contents);
            }
        }

        private static void ApplyCustomLaunchStoryboard(string pathToBuiltProject)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string sourceStoryboard = Path.Combine(projectRoot, "Assets/_Project/iOS/LaunchScreen.storyboard");
            if (!File.Exists(sourceStoryboard))
            {
                Debug.LogWarning($"[IOSPostprocessBuild] Launch storyboard source not found: {sourceStoryboard}");
                return;
            }

            string content = File.ReadAllText(sourceStoryboard);
            string iphoneStoryboard = Path.Combine(pathToBuiltProject, "LaunchScreen-iPhone.storyboard");
            string ipadStoryboard = Path.Combine(pathToBuiltProject, "LaunchScreen-iPad.storyboard");

            File.WriteAllText(iphoneStoryboard, content);
            File.WriteAllText(ipadStoryboard, content);
        }
    }
}
#endif
