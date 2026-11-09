using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Builds the player that takes the UI screenshots (see Assets/Scripts/UiShots.cs and Tools/run_ui_shots.sh).
public static class UiShotsBuild
{
    public static void Build()
    {
        var args = System.Environment.GetCommandLineArgs();
        string output = args[System.Array.IndexOf(args, "-uishotsexe") + 1];
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/MovementTest.unity" },
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            extraScriptingDefines = new[] { "UI_SHOTS" },
            options = BuildOptions.None,
        });
        Debug.Log("UI_SHOTS_BUILD: " + report.summary.result);
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
