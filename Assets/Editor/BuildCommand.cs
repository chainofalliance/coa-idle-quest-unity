#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;

[InitializeOnLoad]
public static class BuildCommand
{
    /* BUILD SPECIFIC */
    private const string KEYSTORE_PASS = "KEYSTORE_PASS";
    private const string KEY_ALIAS_PASS = "KEY_ALIAS_PASS";
    private const string KEY_ALIAS_NAME = "KEY_ALIAS_NAME";
    private const string KEYSTORE = "keystore.keystore";
    private const string BUILD_OPTIONS_ENV_VAR = "BuildOptions";
    private const string ANDROID_BUNDLE_VERSION_CODE = "VERSION_BUILD_VAR";
    private const string ANDROID_APP_BUNDLE = "BUILD_APP_BUNDLE";
    private const string SCRIPTING_BACKEND_ENV_VAR = "SCRIPTING_BACKEND";
    private const string VERSION_NUMBER_VAR = "VERSION_NUMBER_VAR";
    private const string VERSION_iOS = "VERSION_BUILD_VAR";

    /* GAME SPECIFIC */
    private const string TEST_BUILD = "TEST_BUILD";
    private const string HOST_MODE = "HOST_MODE";
    private const string GAME_ENV = "GAME_ENV";
    private const string SERVER_BUILD = "SERVER_BUILD";

    private static bool isServerBuild = false;
    private static string defines = "";


    static string GetArgument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Contains(name))
            {
                return args[i + 1];
            }
        }
        return null;
    }

    static string[] GetEnabledScenes()
    {
        return (
            from scene in EditorBuildSettings.scenes
            where scene.enabled
            where !string.IsNullOrEmpty(scene.path)
            select scene.path
        ).ToArray();
    }

    static BuildTarget GetBuildTarget(string customBuildTarget = null)
    {
        string buildTargetName = customBuildTarget ?? GetArgument("customBuildTarget");
        Console.WriteLine(":: Received customBuildTarget " + buildTargetName);

        if (buildTargetName.ToLower() == "android")
        {
#if !UNITY_5_6_OR_NEWER
			// https://issuetracker.unity3d.com/issues/buildoptions-dot-acceptexternalmodificationstoplayer-causes-unityexception-unknown-project-type-0
			// Fixed in Unity 5.6.0
			// side effect to fix android build system:
			EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Internal;
#endif
        }

        if (buildTargetName.TryConvertToEnum(out BuildTarget target))
            return target;

        Console.WriteLine($":: {nameof(buildTargetName)} \"{buildTargetName}\" not defined on enum {nameof(BuildTarget)}, using {nameof(BuildTarget.NoTarget)} enum to build");

        return BuildTarget.NoTarget;
    }

    static string GetBuildPath(string customBuildPath = null)
    {
        string buildPath = customBuildPath ?? GetArgument("customBuildPath");
        Console.WriteLine(":: Received customBuildPath " + buildPath);
        if (buildPath == "")
        {
            throw new Exception("customBuildPath argument is missing");
        }
        return buildPath;
    }

    static string GetBuildName(string customBuildName = null)
    {
        string buildName = customBuildName ?? GetArgument("customBuildName");
        Console.WriteLine(":: Received customBuildName " + buildName);
        if (buildName == "")
        {
            throw new Exception("customBuildName argument is missing");
        }
        return buildName;
    }

    static string GetFixedBuildPath(BuildTarget buildTarget, string buildPath, string buildName)
    {
        if (buildTarget.ToString().ToLower().Contains("windows"))
        {
            buildName += ".exe";
        }
        else if (buildTarget == BuildTarget.Android)
        {
#if UNITY_2018_3_OR_NEWER
            buildName += EditorUserBuildSettings.buildAppBundle ? ".aab" : ".apk";
#else
            buildName += ".apk";
#endif
        }
        return buildPath + buildName;
    }

    static BuildOptions GetBuildOptions()
    {
        if (TryGetEnv(BUILD_OPTIONS_ENV_VAR, out string envVar))
        {
            string[] allOptionVars = envVar.Split(',');
            BuildOptions allOptions = BuildOptions.None;
            BuildOptions option;
            string optionVar;
            int length = allOptionVars.Length;

            Console.WriteLine($":: Detecting {BUILD_OPTIONS_ENV_VAR} env var with {length} elements ({envVar})");

            for (int i = 0; i < length; i++)
            {
                optionVar = allOptionVars[i];

                if (optionVar.TryConvertToEnum(out option))
                {
                    allOptions |= option;
                }
                else
                {
                    Console.WriteLine($":: Cannot convert {optionVar} to {nameof(BuildOptions)} enum, skipping it.");
                }
            }

            return allOptions;
        }

        return BuildOptions.None;
    }

    // https://stackoverflow.com/questions/1082532/how-to-tryparse-for-enum-value
    static bool TryConvertToEnum<TEnum>(this string strEnumValue, out TEnum value)
    {
        if (!Enum.IsDefined(typeof(TEnum), strEnumValue))
        {
            value = default;
            return false;
        }

        value = (TEnum)Enum.Parse(typeof(TEnum), strEnumValue);
        return true;
    }

    static bool TryGetEnv(string key, out string value)
    {
        value = Environment.GetEnvironmentVariable(key);
        return !string.IsNullOrEmpty(value);
    }

    static void SetEnv(string key, string value)
    {
        Environment.SetEnvironmentVariable(key, value);
    }

    static void SetScriptingBackendFromEnv(BuildTarget platform)
    {
        var targetGroup = BuildPipeline.GetBuildTargetGroup(platform);
        if (TryGetEnv(SCRIPTING_BACKEND_ENV_VAR, out string scriptingBackend))
        {
            if (scriptingBackend.TryConvertToEnum(out ScriptingImplementation backend))
            {
                Console.WriteLine($":: Setting ScriptingBackend to {backend} for {targetGroup}");
                PlayerSettings.SetScriptingBackend(targetGroup, backend);
            }
            else
            {
                string possibleValues = string.Join(", ", Enum.GetValues(typeof(ScriptingImplementation)).Cast<ScriptingImplementation>());
                throw new Exception($"Could not find '{scriptingBackend}' in ScriptingImplementation enum. Possible values are: {possibleValues}");
            }
        }
        else
        {
            var defaultBackend = PlayerSettings.GetDefaultScriptingBackend(targetGroup);
            Console.WriteLine($":: Using project's configured ScriptingBackend (should be {defaultBackend} for targetGroup {targetGroup}");
        }
    }

    static void PerformBuild() {
        PerformBuildInternal();
    }

    static async UniTask PerformBuildInternal(
        string customBuildTarget = null,
        string customBuildName = null,
        string customBuildPath = null
    )
    {
        HandleServerBuild();
        var buildTarget = GetBuildTarget(customBuildTarget);
        GetScriptingSymbols(buildTarget);

        HandleHostMode(buildTarget);
        HandleTestMode(buildTarget);
        HandleEnvironment();

        SaveScriptingSymbols(buildTarget);
        SetBuildTarget(buildTarget);

        if (TryGetEnv(VERSION_NUMBER_VAR, out var bundleVersionNumber))
        {
            if (buildTarget == BuildTarget.iOS)
            {
                bundleVersionNumber = GetIosVersion();
            }
            Console.WriteLine($":: Setting bundleVersionNumber to '{bundleVersionNumber}' (Length: {bundleVersionNumber.Length})");
            PlayerSettings.bundleVersion = bundleVersionNumber;
        }

        if (buildTarget == BuildTarget.Android)
        {
            HandleAndroidAppBundle();
            HandleAndroidBundleVersionCode();
            HandleAndroidKeystore();
        }

        var buildPath = GetBuildPath(customBuildPath);
        var buildName = GetBuildName(customBuildName);
        var buildOptions = GetBuildOptions();
        var fixedBuildPath = GetFixedBuildPath(buildTarget, buildPath, buildName);

        SetScriptingBackendFromEnv(buildTarget);

        // await WaitForCompiling();

        Console.WriteLine($":: Performing build\n\tScenes:{GetEnabledScenes().Aggregate("", (c, s) => c + $"{s}, ")}\n\tPath: {fixedBuildPath}\n\tTarget: {buildTarget}\n\tOptions: {buildOptions}");
        var buildReport = BuildPipeline.BuildPlayer(GetEnabledScenes(), fixedBuildPath, buildTarget, buildOptions);

        if (buildReport.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception($"Build ended with {buildReport.summary.result} status");

        Console.WriteLine(":: Done with build");
    }

    private static async UniTask WaitForCompiling()
    {
        UnityEngine.Debug.Log("Waiting...");
        await UniTask.WaitUntil(() => !EditorApplication.isCompiling && !EditorApplication.isUpdating);
        UnityEngine.Debug.Log("Done!");
    }

    private static void SetBuildTarget(BuildTarget buildTarget)
    {
        if (buildTarget.ToString().ToLower().Contains("windows"))
        {
            if (isServerBuild)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Server, BuildTarget.StandaloneWindows64);
            }
            else
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            }
            Console.WriteLine($":: Set build target to StandaloneWindows64 (isServer={isServerBuild})");
        }
        else if (buildTarget.ToString().ToLower().Contains("linux"))
        {
            if (isServerBuild)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Server, BuildTarget.StandaloneLinux64);
            }
            else
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64);
            }
            Console.WriteLine($":: Set build target to StandaloneLinux64 (isServer={isServerBuild})");
        }
        else if (buildTarget.ToString().ToLower().Contains("webgl"))
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            
            Console.WriteLine($":: Set build target to WebGL)");
        }
        else
        {
            if (isServerBuild)
            {
                Console.WriteLine($" :: Server build requires windows build target! Aborting...");
                EditorApplication.Exit(1);
            }
            else
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, buildTarget);
            }
            Console.WriteLine($":: Set build target to Android");
        }
    }

    private static void GetScriptingSymbols(BuildTarget buildTarget)
    {
        var targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
        defines = isServerBuild ? PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Server) : PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
    }

    private static void SetScriptingSymbol(string symbol, bool on)
    {
        if (on && Regex.IsMatch(defines, $"{symbol}(;|$)"))
            return;
        defines = on ? defines + $";{symbol}" : Regex.Replace(defines, $"{symbol}(;|$)", "");
    }

    private static void SaveScriptingSymbols(BuildTarget buildTarget)
    {
        var targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
        if (isServerBuild)
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Server, defines);
        else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defines);

        Console.WriteLine($":: Set scripting symbols for {buildTarget} (isServer={isServerBuild}): {defines}");
    }


    private static void HandleServerBuild()
    {
        if (TryGetEnv(SERVER_BUILD, out var value))
        {
            if (bool.TryParse(value, out bool serverBuild))
            {
                isServerBuild = serverBuild;
                Console.WriteLine($":: {SERVER_BUILD} env var detected, set \"isServerBuild\" to {value}.");
            }
            else
            {
                Console.WriteLine($":: {SERVER_BUILD} env var detected but the value \"{value}\" is not a boolean.");
            }
        }
    }

    private static void HandleHostMode(BuildTarget buildTarget)
    {
        if (TryGetEnv(HOST_MODE, out var value))
        {
            if (bool.TryParse(value, out bool hostMode))
            { 
                if (hostMode && (isServerBuild
                    || !buildTarget.ToString().ToLower().Contains("windows")
                    || (buildTarget == BuildTarget.Android && EditorUserBuildSettings.buildAppBundle)))
                {
                    Console.WriteLine($" :: HOST_MODE requires a windows client or android apk build! Aborting...");
                    EditorApplication.Exit(1);
                    return;
                }

                SetScriptingSymbol(HOST_MODE, hostMode);
                Console.WriteLine($":: {HOST_MODE} env var detected, set HOST_MODE to {value}.");
            }
            else
            {
                Console.WriteLine($":: {HOST_MODE} env var detected but the value \"{value}\" is not a boolean.");
            }
        }
    }

    private static void HandleTestMode(BuildTarget buildTarget)
    {
        if (TryGetEnv(TEST_BUILD, out var value))
        {
            if (bool.TryParse(value, out bool testMode))
            {
                SetScriptingSymbol(TEST_BUILD, testMode);
                Console.WriteLine($":: {TEST_BUILD} env var detected, set TEST_BUILD to {value}.");
            }
            else
            {
                Console.WriteLine($":: {TEST_BUILD} env var detected but the value \"{value}\" is not a boolean.");
            }
        }
    }

    private static void HandleEnvironment()
    {
        if (TryGetEnv(GAME_ENV, out var env))
        {
            var validEnvs = new List<string>() { "dev", "staging", "prod", "local", "devnet" };
            if (!validEnvs.Contains(env))
            {
                Console.WriteLine($" :: ENVIRONMENT \"{env}\" not known! Aborting...");
                EditorApplication.Exit(1);
                return;
            }

            // Set active env and add it to version
            SetScriptingSymbol($"COA_{env.ToUpper()}", true);

            // Unset all non active envs in case they are set
            validEnvs.Remove(env);
            validEnvs.ForEach(e => SetScriptingSymbol($"COA_{e.ToUpper()}", false));
        }
    }

    private static void HandleAndroidAppBundle()
    {
        if (TryGetEnv(ANDROID_APP_BUNDLE, out string value))
        {
#if UNITY_2018_3_OR_NEWER
            if (bool.TryParse(value, out bool buildAppBundle))
            {
                EditorUserBuildSettings.buildAppBundle = buildAppBundle;
                Console.WriteLine($":: {ANDROID_APP_BUNDLE} env var detected, set buildAppBundle to {value}.");
            }
            else
            {
                Console.WriteLine($":: {ANDROID_APP_BUNDLE} env var detected but the value \"{value}\" is not a boolean.");
            }
#else
            Console.WriteLine($":: {ANDROID_APP_BUNDLE} env var detected but does not work with lower Unity version than 2018.3");
#endif
        }
    }

    private static void HandleAndroidBundleVersionCode()
    {
        if (TryGetEnv(ANDROID_BUNDLE_VERSION_CODE, out string value))
        {
            if (int.TryParse(value, out int version))
            {
                PlayerSettings.Android.bundleVersionCode = version;
                Console.WriteLine($":: {ANDROID_BUNDLE_VERSION_CODE} env var detected, set the bundle version code to {value}.");
            }
            else
                Console.WriteLine($":: {ANDROID_BUNDLE_VERSION_CODE} env var detected but the version value \"{value}\" is not an integer.");
        }
    }

    private static string GetIosVersion()
    {
        if (TryGetEnv(VERSION_iOS, out string value))
        {
            if (int.TryParse(value, out int version))
            {
                Console.WriteLine($":: {VERSION_iOS} env var detected, set the version to {value}.");
                return version.ToString();
            }
            else
                Console.WriteLine($":: {VERSION_iOS} env var detected but the version value \"{value}\" is not an integer.");
        }

        throw new ArgumentNullException(nameof(value), $":: Error finding {VERSION_iOS} env var");
    }

    private static void HandleAndroidKeystore()
    {
#if UNITY_2019_1_OR_NEWER
        PlayerSettings.Android.useCustomKeystore = false;
#endif

        if (!File.Exists(KEYSTORE))
        {
            Console.WriteLine($":: {KEYSTORE} not found, skipping setup, using Unity's default keystore");
            return;
        }

        PlayerSettings.Android.keystoreName = KEYSTORE;

        string keystorePass;
        string keystoreAliasPass;

        if (TryGetEnv(KEY_ALIAS_NAME, out string keyaliasName))
        {
            PlayerSettings.Android.keyaliasName = keyaliasName;
            Console.WriteLine($":: using ${KEY_ALIAS_NAME} env var on PlayerSettings");
        }
        else
        {
            Console.WriteLine($":: ${KEY_ALIAS_NAME} env var not set, using Project's PlayerSettings");
        }

        if (!TryGetEnv(KEYSTORE_PASS, out keystorePass))
        {
            Console.WriteLine($":: ${KEYSTORE_PASS} env var not set, skipping setup, using Unity's default keystore");
            return;
        }

        if (!TryGetEnv(KEY_ALIAS_PASS, out keystoreAliasPass))
        {
            Console.WriteLine($":: ${KEY_ALIAS_PASS} env var not set, skipping setup, using Unity's default keystore");
            return;
        }
#if UNITY_2019_1_OR_NEWER
        PlayerSettings.Android.useCustomKeystore = true;
#endif
        PlayerSettings.Android.keystorePass = keystorePass;
        PlayerSettings.Android.keyaliasPass = keystoreAliasPass;
    }
}
#endif