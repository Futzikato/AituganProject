#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Aitugan.EditorTools
{
    /// <summary>
    /// Configures the project for WebGL (itch.io friendly) and provides
    /// a one-click build that drops a ready-to-upload zip in the project root.
    ///
    /// Menu items:
    ///   Aitugan/Web (itch.io)/Configure WebGL Settings
    ///   Aitugan/Web (itch.io)/Build WebGL (itch.io)
    ///   Aitugan/Web (itch.io)/Build + Zip for itch.io
    ///
    /// CLI usage (no UI):
    ///   Unity -batchmode -nographics -projectPath . \
    ///         -executeMethod Aitugan.EditorTools.WebGLBuildConfig.BuildFromCli -quit
    /// </summary>
    public static class WebGLBuildConfig
    {
        const string BuildDirName = "Build_WebGL";
        const string ZipName      = "AituganWoY_WebGL.zip";

        // ---------------------------------------------------------------
        //  Menu entry points
        // ---------------------------------------------------------------

        [MenuItem("Aitugan/Web (itch.io)/Configure WebGL Settings")]
        public static void ConfigureMenu()
        {
            EnsureWebGLTarget();
            ApplyWebGLSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[Aitugan] WebGL settings applied. Ready for itch.io build.");
        }

        [MenuItem("Aitugan/Web (itch.io)/Build WebGL (itch.io)")]
        public static void BuildMenu()
        {
            if (!CheckProjectPathForCommas()) return;
            EnsureWebGLTarget();
            ApplyWebGLSettings();
            var path = AbsoluteBuildPath();
            var report = RunBuild(path);
            ShowResult(report, path, zipped: false);
        }

        [MenuItem("Aitugan/Web (itch.io)/Build + Zip for itch.io")]
        public static void BuildAndZipMenu()
        {
            if (!CheckProjectPathForCommas()) return;
            EnsureWebGLTarget();
            ApplyWebGLSettings();
            var path = AbsoluteBuildPath();
            var report = RunBuild(path);
            if (report.summary.result == BuildResult.Succeeded)
            {
                var zipPath = ZipBuild(path);
                EditorUtility.RevealInFinder(zipPath);
            }
            ShowResult(report, path, zipped: true);
        }

        // Entry point for headless CLI builds.
        // Returns non-zero exit code on failure so CI / shell scripts can detect it.
        public static void BuildFromCli()
        {
            try
            {
                EnsureWebGLTarget();
                ApplyWebGLSettings();
                var path = AbsoluteBuildPath();
                var report = RunBuild(path);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError($"[Aitugan] WebGL build failed: {report.summary.result}");
                    if (Application.isBatchMode) EditorApplication.Exit(1);
                    return;
                }
                var zipPath = ZipBuild(path);
                Debug.Log($"[Aitugan] WebGL build OK. Zip ready at: {zipPath}");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Aitugan] WebGL build threw: {ex}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        // ---------------------------------------------------------------
        //  Preflight
        // ---------------------------------------------------------------

        /// <summary>
        /// UnityLinker (used during WebGL ManagedStripped step) splits its
        /// argument list on commas. If the project path contains a comma, the
        /// build fails ~3s in with:
        ///   System.IO.FileNotFoundException: Could not find file '...&lt;truncated at comma&gt;'
        /// Detect that up front and refuse to build with a useful message.
        /// </summary>
        static bool CheckProjectPathForCommas()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            if (!projectRoot.Contains(","))
                return true;

            var msg =
                "The project path contains a comma:\n\n" +
                projectRoot + "\n\n" +
                "Unity's WebGL build (UnityLinker) cannot handle commas in paths " +
                "and will fail at the ManagedStripped step.\n\n" +
                "Fix: quit Unity, then run fix_comma_path.command in the project " +
                "root to rename the folder (e.g. 'AituganWoY,HoT' -> 'AituganWoY_HoT'). " +
                "Reopen Unity from the renamed folder and build again.";
            Debug.LogError("[Aitugan] " + msg);
            EditorUtility.DisplayDialog("Aitugan - WebGL Build", msg, "OK");
            return false;
        }

        // ---------------------------------------------------------------
        //  Build target + Player settings
        // ---------------------------------------------------------------

        static void EnsureWebGLTarget()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                Debug.Log("[Aitugan] Switching active build target to WebGL...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            }
        }

        static void ApplyWebGLSettings()
        {
            var webgl = NamedBuildTarget.WebGL;

            // Identity (kept in sync with the iOS config)
            PlayerSettings.companyName = "SteppeChronicles";
            PlayerSettings.productName = "Aitugan";
            PlayerSettings.SetApplicationIdentifier(webgl, "com.SteppeChronicles.Aitugan");

            // Scripting backend on WebGL is IL2CPP (only option), but make it explicit.
            PlayerSettings.SetScriptingBackend(webgl, ScriptingImplementation.IL2CPP);

            // itch.io recommended: disable compression so files are served directly
            // without needing a server-side Content-Encoding header.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

            // Decompression fallback off too (paired with the line above).
            PlayerSettings.WebGL.decompressionFallback = false;

            // Larger default memory so the runtime doesn't crash on Safari/iOS.
            PlayerSettings.WebGL.memorySize = 512;

            // Faster exceptions / smaller build than "Explicitly Thrown..." with stack.
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;

            // Don't strip data caching - itch.io serves the build folder as-is.
            PlayerSettings.WebGL.dataCaching = true;

            // Use the default WebGL template (works fine inside an iframe on itch.io).
            PlayerSettings.WebGL.template = "APPLICATION:Default";

            // Code optimization: ship as Release/Master to keep size down.
#if UNITY_2022_2_OR_NEWER
            PlayerSettings.SetIl2CppCodeGeneration(webgl, Il2CppCodeGeneration.OptimizeSize);
#endif
            EditorUserBuildSettings.development = false;

            // Disable managed code stripping. UnityLinker on WebGL frequently fails on
            // reflection-heavy packages (Newtonsoft.Json, Input System, etc.) and aborts
            // at the "ManagedStripped" step. Disabling stripping reliably produces a
            // working build at a small size cost. A link.xml at Assets/link.xml is also
            // provided as defense-in-depth if you raise the stripping level later.
            PlayerSettings.SetManagedStrippingLevel(webgl, ManagedStrippingLevel.Disabled);

            // Quality/Graphics: leave whatever the project picked; nothing WebGL-specific to force.
        }

        // ---------------------------------------------------------------
        //  Build + zip
        // ---------------------------------------------------------------

        static string AbsoluteBuildPath()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, BuildDirName);
        }

        static string[] EnabledScenes()
        {
            var scenes = EditorBuildSettings.scenes;
            var list = new System.Collections.Generic.List<string>(scenes.Length);
            foreach (var s in scenes) if (s.enabled) list.Add(s.path);
            if (list.Count == 0)
            {
                // Fallback: include the sample scene if nothing's in Build Settings.
                list.Add("Assets/Scenes/SampleScene.unity");
            }
            return list.ToArray();
        }

        static BuildReport RunBuild(string outputPath)
        {
            if (Directory.Exists(outputPath))
            {
                try { Directory.Delete(outputPath, true); } catch { /* keep going */ }
            }
            Directory.CreateDirectory(outputPath);

            var options = new BuildPlayerOptions
            {
                scenes           = EnabledScenes(),
                locationPathName = outputPath,
                target           = BuildTarget.WebGL,
                targetGroup      = BuildTargetGroup.WebGL,
                options          = BuildOptions.None,
            };

            Debug.Log($"[Aitugan] Building WebGL to: {outputPath}");
            return BuildPipeline.BuildPlayer(options);
        }

        static string ZipBuild(string buildPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var zipPath = Path.Combine(projectRoot, ZipName);
            if (File.Exists(zipPath)) File.Delete(zipPath);

            // Use System.IO.Compression for a portable zip (no shell required).
            System.IO.Compression.ZipFile.CreateFromDirectory(
                buildPath,
                zipPath,
                System.IO.Compression.CompressionLevel.Optimal,
                includeBaseDirectory: false);

            Debug.Log($"[Aitugan] Zipped build ready for itch.io: {zipPath}");
            return zipPath;
        }

        static void ShowResult(BuildReport report, string buildPath, bool zipped)
        {
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                var msg = $"WebGL build succeeded.\n\nOutput: {buildPath}\nSize: {summary.totalSize / (1024 * 1024)} MB";
                if (zipped) msg += $"\n\nZip: {Path.Combine(Directory.GetParent(Application.dataPath).FullName, ZipName)}";
                Debug.Log("[Aitugan] " + msg.Replace("\n\n", "  "));
                EditorUtility.DisplayDialog("Aitugan - WebGL Build", msg, "OK");
            }
            else
            {
                var errorPath = DumpFailureReport(report);
                var msg = $"WebGL build {summary.result}.\n\nFull error written to:\n{errorPath}\n\nOpen that file (and Editor.log) to see the real UnityLinker output — the Console truncates it.";
                Debug.LogError("[Aitugan] " + msg);
                EditorUtility.DisplayDialog("Aitugan - WebGL Build", msg, "OK");
            }
        }

        static string DumpFailureReport(BuildReport report)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var path = Path.Combine(projectRoot, "Build_WebGL_LastError.txt");
            try
            {
                using (var w = new StreamWriter(path, false))
                {
                    w.WriteLine($"WebGL build result: {report.summary.result}");
                    w.WriteLine($"When:               {DateTime.Now:O}");
                    w.WriteLine($"Errors:             {report.summary.totalErrors}");
                    w.WriteLine($"Warnings:           {report.summary.totalWarnings}");
                    w.WriteLine();
                    foreach (var step in report.steps)
                    {
                        var hasError = false;
                        foreach (var m in step.messages)
                            if (m.type == LogType.Error || m.type == LogType.Exception) { hasError = true; break; }
                        if (!hasError) continue;
                        w.WriteLine($"--- STEP: {step.name} ({step.duration}) ---");
                        foreach (var m in step.messages)
                            if (m.type == LogType.Error || m.type == LogType.Exception)
                                w.WriteLine($"[{m.type}] {m.content}");
                        w.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Aitugan] Could not write failure report: {ex.Message}");
            }
            return path;
        }
    }
}
#endif
