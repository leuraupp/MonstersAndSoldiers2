using System;
using System.Collections.Generic;
using System.Linq;
using MonstersAndSoldiers.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gera os 4 níveis do tutorial (GDD 6.1) com o gerador da Fase 2, procurando seeds que ensinem a regra
/// de cada nível, e liga os níveis ao LevelManager da cena da partida. O resultado fica salvo como
/// LevelData, então os níveis não mudam se o gerador mudar depois.
/// Menu: Tools > Phase 3 > Build Tutorial Levels. Batchmode: -executeMethod TutorialLevelBuilder.BuildBatch.
/// </summary>
public static class TutorialLevelBuilder {
    private const string Folder = "Assets/ScriptableObjects/Levels/Tutorial";
    private const string MatchScenePath = "Assets/Scenes/Gameplay/LevelTest/level_01_test.unity";
    private const int MaxSeed = 20000;

    private class Spec {
        public string Name;
        public int Rows, Cols;
        public MonsterType[] Types;
        public bool Clues;
        public string Hint;
        public Func<GeneratedPuzzle, bool> Teaches;
    }

    // Tamanhos em col x lin, como na GDD.
    private static readonly Spec[] Specs = {
        new Spec {
            Name = "T1", Cols = 4, Rows = 4, Types = new[] { MonsterType.Slime }, Clues = false,
            Hint = "A vila está cercada! Cada Slime precisa de 1 soldado numa das 8 casas ao redor.\n" +
                   "Soldados não podem se encostar, nem na diagonal. Clique numa casa para pôr um soldado.",
            Teaches = p => p.Monsters.Count >= 3,
        },
        new Spec {
            Name = "T2", Cols = 4, Rows = 5, Types = new[] { MonsterType.Slime }, Clues = true,
            Hint = "Os números nas bordas dizem quantos soldados há em cada linha e coluna.\n" +
                   "Onde o número é 0, marque grama com o botão direito: ali não vai soldado.",
            Teaches = p => p.Monsters.Count >= 3 && p.RowClues.Concat(p.ColClues).Count(c => c == 0) >= 2,
        },
        new Spec {
            Name = "T3", Cols = 5, Rows = 5, Types = new[] { MonsterType.Goblin, MonsterType.Slime }, Clues = true,
            Hint = "O Goblin é mais forte: precisa de 2 soldados ao redor.",
            Teaches = p => Count(p, MonsterType.Goblin) >= 1 && Count(p, MonsterType.Goblin) <= 2,
        },
        new Spec {
            Name = "T4", Cols = 6, Rows = 5, Types = new[] { MonsterType.Orc, MonsterType.Slime, MonsterType.Goblin }, Clues = true,
            Hint = "O Orc precisa de 3 soldados. Um soldado encostado em dois monstros conta só para um deles.\n" +
                   "Quando terminar, aperte Atacar!",
            Teaches = p => Count(p, MonsterType.Orc) == 1 && SoldierNextToTwoMonsters(p),
        },
    };

    [MenuItem("Tools/Phase 3/Build Tutorial Levels")]
    private static void BuildFromMenu() {
        Debug.Log(Build());
    }

    public static void BuildBatch() {
        try {
            Debug.Log(Build());
            Debug.Log(Phase3UISetup.BuildMatchScene());
            EditorApplication.Exit(0);
        } catch (Exception e) {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }

    public static string Build() {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/ScriptableObjects/Levels", "Tutorial");

        var levels = new List<LevelData>();
        var report = new List<string>();
        for (int i = 0; i < Specs.Length; i++) {
            Spec spec = Specs[i];
            GeneratedPuzzle puzzle = Find(spec, out int seed);
            LevelData level = Save(spec, puzzle, i);
            levels.Add(level);
            report.Add($"{spec.Name} {spec.Cols}x{spec.Rows} seed {seed}: {puzzle.Monsters.Count} monstros, " +
                       $"{puzzle.Solution.Count} soldados");
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AssignToScene(levels.Select(AssetDatabase.GetAssetPath).ToList());
        return "Tutorial gerado: " + string.Join("; ", report);
    }

    private static GeneratedPuzzle Find(Spec spec, out int seed) {
        var settings = new PuzzleSettings(spec.Rows, spec.Cols, spec.Types);
        for (seed = 1; seed <= MaxSeed; seed++) {
            GeneratedPuzzle p = PuzzleGenerator.Generate(settings, seed);
            if (!p.IsUnique || !spec.Teaches(p)) continue;
            return p;
        }
        throw new InvalidOperationException($"Nenhuma seed até {MaxSeed} serviu para {spec.Name}.");
    }

    private static LevelData Save(Spec spec, GeneratedPuzzle puzzle, int index) {
        string path = $"{Folder}/{spec.Name}.asset";
        var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        if (level == null) {
            level = ScriptableObject.CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(level, path);
        }
        level.levelName = $"Tutorial {index + 1}";
        level.levelID = index + 1;
        level.rows = puzzle.Rows;
        level.columns = puzzle.Cols;
        level.monsterPlacements = puzzle.Monsters
            .OrderBy(m => m.Key.Row).ThenBy(m => m.Key.Col)
            .Select(m => new MonsterPlacement { monsterType = m.Value, gridPosition = new Vector2Int(m.Key.Row, m.Key.Col) })
            .ToList();
        level.rowClues = spec.Clues ? puzzle.RowClues.ToArray() : new int[0];
        level.columnClues = spec.Clues ? puzzle.ColClues.ToArray() : new int[0];
        level.tutorialHint = spec.Hint;
        level.solution = puzzle.Solution.Select(s => new Vector2Int(s.Row, s.Col)).ToList();
        EditorUtility.SetDirty(level);
        return level;
    }

    private static void AssignToScene(List<string> levelPaths) {
        Scene scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
        // Recarrega depois de abrir a cena: abrir em modo Single descarrega os objetos recém-criados.
        List<LevelData> levels = levelPaths.Select(AssetDatabase.LoadAssetAtPath<LevelData>).ToList();
        LevelManager manager = UnityEngine.Object.FindAnyObjectByType<LevelManager>(FindObjectsInactive.Include);
        var so = new SerializedObject(manager);
        SerializedProperty list = so.FindProperty("allLevels");
        list.arraySize = levels.Count;
        for (int i = 0; i < levels.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
        so.FindProperty("mode").enumValueIndex = (int)LevelManager.LevelMode.HandMade;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static int Count(GeneratedPuzzle p, MonsterType type) => p.Monsters.Values.Count(t => t == type);

    // T4 ensina o pareamento 1:1: pelo menos um soldado da solução encosta em dois monstros.
    private static bool SoldierNextToTwoMonsters(GeneratedPuzzle p) {
        return p.Solution.Any(s => s.Neighbors8().Count(n => p.Monsters.ContainsKey(n)) >= 2);
    }
}
