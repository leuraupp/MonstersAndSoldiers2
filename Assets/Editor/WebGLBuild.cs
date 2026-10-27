using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Build WebGL pronto para o itch.io e para o portfólio (GitHub Pages).
/// Nenhum dos dois manda o cabeçalho Content-Encoding dos arquivos comprimidos, então o build usa
/// Gzip com Decompression Fallback: o loader descomprime no navegador e funciona em qualquer servidor.
/// Pelo terminal: Unity.exe -batchmode -quit -projectPath . -executeMethod WebGLBuild.Build
/// </summary>
public static class WebGLBuild {
    private const string OutputDir = "Builds/WebGL";
    private const string Template = "PROJECT:MonstersAndSoldiers";

    [MenuItem("Tools/Build/WebGL (itch.io e portfólio)")]
    public static void Build() {
        ApplySettings();

        var options = new BuildPlayerOptions {
            scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes),
            locationPathName = OutputDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"WebGLBuild: {report.summary.result}, {report.summary.totalSize / (1024f * 1024f):0.0} MB em {Path.GetFullPath(OutputDir)}");
        if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }

    private static void ApplySettings() {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.nameFilesAsHashes = false;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.template = Template;
        // O jogo é 16:9 (UI em 1920x1080); o template ajusta o canvas à janela.
        PlayerSettings.defaultWebScreenWidth = 960;
        PlayerSettings.defaultWebScreenHeight = 540;
        AssetDatabase.SaveAssets();
    }
}
