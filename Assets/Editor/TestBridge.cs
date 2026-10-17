using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>
/// Roda os testes EditMode no Editor aberto a pedido do terminal (Tools/run-tests.ps1), sem cópia do projeto.
/// O terminal escreve Temp/TestBridge/request.txt (conteúdo = filtro regex opcional); a ponte dá Refresh para
/// compilar o que mudou fora do Editor, roda os testes e escreve result.txt e results.xml na mesma pasta.
/// </summary>
[InitializeOnLoad]
public static class TestBridge {
    private const string PendingKey = "TestBridge.Pending";   // "|filtro" esperando a compilação terminar
    private const string RunningKey = "TestBridge.Running";
    private const string FailuresKey = "TestBridge.Failures"; // sobrevive ao domain reload dos testes que entram em Play Mode

    private static readonly string Dir = Path.GetFullPath("Temp/TestBridge");
    private static readonly List<string> compileErrors = new List<string>();
    private static double nextPoll;

    static TestBridge() {
        // Os callbacks precisam ser registrados de novo a cada domain reload.
        ScriptableObject.CreateInstance<TestRunnerApi>().RegisterCallbacks(new Callbacks());
        CompilationPipeline.assemblyCompilationFinished += (_, messages) => {
            foreach (CompilerMessage m in messages) {
                if (m.type == CompilerMessageType.Error) compileErrors.Add(m.message);
            }
        };
        EditorApplication.update += Poll;
    }

    private static string RequestPath => Path.Combine(Dir, "request.txt");

    private static void Poll() {
        if (EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + 0.5;

        if (SessionState.GetBool(RunningKey, false)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;

        // Chave ausente volta como "": pedidos de verdade sempre começam com "|".
        string pending = SessionState.GetString(PendingKey, "");
        if (pending.StartsWith("|")) {
            SessionState.EraseString(PendingKey);
            if (EditorUtility.scriptCompilationFailed) {
                WriteResult("compile-error", string.Join("\n", compileErrors));
            } else {
                Run(pending.Substring(1));
            }
            return;
        }

        if (!File.Exists(RequestPath)) return;
        string filter = File.ReadAllText(RequestPath).Trim();
        File.Delete(RequestPath);

        compileErrors.Clear();
        SessionState.SetString(PendingKey, "|" + filter);
        WriteResult("compiling", "");
        AssetDatabase.Refresh();
    }

    private static void Run(string filterText) {
        SessionState.SetBool(RunningKey, true);
        SessionState.SetString(FailuresKey, "");
        WriteResult("running", "");

        var filter = new Filter { testMode = TestMode.EditMode };
        if (filterText.Length > 0) filter.groupNames = new[] { filterText };
        ScriptableObject.CreateInstance<TestRunnerApi>().Execute(new ExecutionSettings(filter));
    }

    private static void WriteResult(string status, string body) {
        Directory.CreateDirectory(Dir);
        string path = Path.Combine(Dir, "result.txt");
        File.WriteAllText(path + ".tmp", $"status: {status}\n{body}", Encoding.UTF8);
        File.Copy(path + ".tmp", path, true);
        File.Delete(path + ".tmp");
    }

    private class Callbacks : ICallbacks {
        public void RunStarted(ITestAdaptor testsToRun) { }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result) {
            if (!SessionState.GetBool(RunningKey, false) || result.HasChildren || result.TestStatus != TestStatus.Failed) return;
            string line = $"FALHOU {result.FullName}: {result.Message?.Trim()}\n";
            SessionState.SetString(FailuresKey, SessionState.GetString(FailuresKey, "") + line);
        }

        public void RunFinished(ITestResultAdaptor result) {
            if (!SessionState.GetBool(RunningKey, false)) return;
            SessionState.EraseBool(RunningKey);

            Directory.CreateDirectory(Dir);
            File.WriteAllText(Path.Combine(Dir, "results.xml"), result.ToXml().OuterXml, Encoding.UTF8);

            int total = result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount;
            string summary = $"Testes: {total} total, {result.PassCount} passaram, {result.FailCount} falharam\n" +
                             SessionState.GetString(FailuresKey, "");
            WriteResult(result.FailCount == 0 ? "passed" : "failed", summary);
        }
    }
}
