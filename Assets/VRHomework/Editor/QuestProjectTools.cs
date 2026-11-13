using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using Unity.XR.Oculus;

namespace QuestCounter.Editor
{
    [InitializeOnLoad]
    public static class QuestProjectTools
    {
        public const string RequiredVersion = "2022.3.62f3";
        private const string ScenePath = "Assets/VRHomework/Scenes/Main.unity";
        private const string SetupMarker = "ProjectSettings/QuestCounterSetup.json";
        static QuestProjectTools() { EditorApplication.delayCall += FirstImport; }

        private static void FirstImport()
        {
            if (Application.isBatchMode || File.Exists(SetupMarker)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += FirstImport; return; }
            try { Configure(); }
            catch (Exception e) { Debug.LogException(e); }
        }

        [MenuItem("VR Homework/1 - Configure Quest project")]
        public static void Configure()
        {
            if (Application.unityVersion != RequiredVersion)
                Debug.LogWarning("Group version is " + RequiredVersion + "; current editor is " + Application.unityVersion + ". Android build requires the agreed version.");
            Directory.CreateDirectory("Assets/XR/Settings"); AssetDatabase.Refresh();
            XRGeneralSettingsPerBuildTarget perTarget;
            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out perTarget) || perTarget == null)
            {
                const string settingsPath = "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset";
                perTarget = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(settingsPath);
                if (perTarget == null)
                { perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>(); AssetDatabase.CreateAsset(perTarget,settingsPath); }
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey,perTarget,true);
            }
            XRGeneralSettings android = EnsureSettings(perTarget,BuildTargetGroup.Android);
            android.InitManagerOnStart = true;
            if (!XRPackageMetadataStore.AssignLoader(android.Manager,"Unity.XR.Oculus.OculusLoader",BuildTargetGroup.Android))
                throw new BuildFailedException("Could not assign the Oculus Android loader. Check Package Manager errors.");
            XRGeneralSettings desktop = EnsureSettings(perTarget,BuildTargetGroup.Standalone);
            // A new project starts in mouse mode; Quest Link can be enabled explicitly from the menu.
            if (!File.Exists(SetupMarker)) desktop.InitManagerOnStart = false;
            OculusSettings oculus;
            const string oculusKey = "Unity.XR.Oculus.Settings";
            if (!EditorBuildSettings.TryGetConfigObject(oculusKey,out oculus) || oculus == null)
            {
                const string oculusPath = "Assets/XR/Settings/OculusSettings.asset";
                oculus = AssetDatabase.LoadAssetAtPath<OculusSettings>(oculusPath);
                if (oculus == null)
                { oculus = ScriptableObject.CreateInstance<OculusSettings>(); AssetDatabase.CreateAsset(oculus,oculusPath); }
                EditorBuildSettings.AddConfigObject(oculusKey,oculus,true);
            }
            oculus.TargetQuest2 = true;
            oculus.m_StereoRenderingModeAndroid = OculusSettings.StereoRenderingModeAndroid.Multiview;
            oculus.m_StereoRenderingModeDesktop = OculusSettings.StereoRenderingModeDesktop.SinglePassInstanced;
            PlayerSettings.companyName = "VR Homework"; PlayerSettings.productName = "Quest Ray Counter";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"edu.vr.homework3.raycounter");
            PlayerSettings.bundleVersion = "1.0"; PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[] { GraphicsDeviceType.Direct3D11 });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            QualitySettings.antiAliasing = 4; QualitySettings.vSyncCount = 0; QualitySettings.shadows = ShadowQuality.Disable;
            if (!File.Exists(ScenePath)) throw new BuildFailedException("Missing Main.unity; restore the supplied scene.");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath,true) };
            EditorUtility.SetDirty(android); EditorUtility.SetDirty(desktop); EditorUtility.SetDirty(perTarget); EditorUtility.SetDirty(oculus);
            AssetDatabase.SaveAssets();
            File.WriteAllText(SetupMarker,"{\"version\":\"" + RequiredVersion + "\",\"androidProvider\":\"Oculus\"}");
            Debug.Log("Quest project configured. Desktop mouse preview is available; Android runtime has Oculus enabled.");
        }
        private static XRGeneralSettings EnsureSettings(XRGeneralSettingsPerBuildTarget perTarget, BuildTargetGroup group)
        {
            if (!perTarget.HasSettingsForBuildTarget(group)) perTarget.CreateDefaultSettingsForBuildTarget(group);
            if (!perTarget.HasManagerSettingsForBuildTarget(group)) perTarget.CreateDefaultManagerSettingsForBuildTarget(group);
            var settings = perTarget.SettingsForBuildTarget(group);
            settings.Manager.automaticLoading = true; settings.Manager.automaticRunning = true;
            EditorUtility.SetDirty(settings.Manager); return settings;
        }

        [MenuItem("VR Homework/2 - Open demo scene")]
        public static void OpenDemo()
        { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath); }

        [MenuItem("VR Homework/Preview - Mouse (no headset required)")]
        public static void UseMousePreview()
        {
            Configure();
            var desktop = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            desktop.InitManagerOnStart = false; EditorUtility.SetDirty(desktop); AssetDatabase.SaveAssets();
            Debug.Log("Mouse preview enabled. Stop Play mode before changing preview modes.");
        }

        [MenuItem("VR Homework/Preview - Quest Link (Meta app and headset required)")]
        public static void UseQuestLink()
        {
            Configure();
            var desktop = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (!XRPackageMetadataStore.AssignLoader(desktop.Manager,"Unity.XR.Oculus.OculusLoader",BuildTargetGroup.Standalone))
                throw new BuildFailedException("Could not assign the Windows Oculus loader.");
            desktop.InitManagerOnStart = true; EditorUtility.SetDirty(desktop); EditorUtility.SetDirty(desktop.Manager); AssetDatabase.SaveAssets();
            Debug.Log("Quest Link preview enabled. Connect Quest Link in the headset, then press Play.");
        }

        [MenuItem("VR Homework/3 - Switch build platform to Android")]
        public static void SwitchToAndroid()
        {
            Configure();
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))
                throw new BuildFailedException("Install Android Build Support, Android SDK & NDK Tools, and OpenJDK for " + RequiredVersion + " in Unity Hub.");
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android,BuildTarget.Android))
                throw new BuildFailedException("Android platform switch failed.");
        }

        [MenuItem("VR Homework/4 - Build Quest APK")]
        public static void BuildQuestApk()
        {
            if (Application.unityVersion != RequiredVersion) throw new BuildFailedException("Use Unity " + RequiredVersion + " exactly.");
            string projectPath = Path.GetFullPath(".");
            foreach (char c in projectPath)
                if (c > 127) throw new BuildFailedException("Android build needs an ASCII workspace. Run Tools/Build-Quest.ps1; it creates a clean copy at D:/VRHomework3/QuestRayCounter.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new BuildFailedException("Switch to Android first, or use Tools/Build-Quest.ps1.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))
                throw new BuildFailedException("Android Build Support is missing for this editor.");
            Configure(); Directory.CreateDirectory("Builds");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = "Builds/QuestRayCounter.apk",
                target = BuildTarget.Android, options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("APK build failed: " + report.summary.result + "; errors=" + report.summary.totalErrors);
            Debug.Log("APK BUILD SUCCEEDED: " + Path.GetFullPath("Builds/QuestRayCounter.apk"));
        }
    }
}
