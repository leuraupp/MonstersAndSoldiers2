using System.Collections.Generic;
using System.Linq;
using MonstersAndSoldiers.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>Os 4 níveis do tutorial (GDD 6.1) existem, têm o tamanho certo e são resolvíveis por lógica.</summary>
public class TutorialLevelTests {
    private static readonly (string name, int cols, int rows, bool clues, MonsterType newMonster)[] Expected = {
        ("T1", 4, 4, false, MonsterType.Slime),
        ("T2", 4, 5, true, MonsterType.Slime),
        ("T3", 5, 5, true, MonsterType.Goblin),
        ("T4", 6, 5, true, MonsterType.Orc),
    };

    private static LevelData Load(string name) {
        var level = AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/ScriptableObjects/Levels/Tutorial/{name}.asset");
        Assert.IsNotNull(level, $"{name} não existe.");
        return level;
    }

    [Test]
    public void Levels_HaveSizeCluesHintAndNewMonster() {
        foreach (var e in Expected) {
            LevelData level = Load(e.name);
            Assert.AreEqual(e.cols, level.columns, e.name);
            Assert.AreEqual(e.rows, level.rows, e.name);
            Assert.AreEqual(e.clues, level.HasClues, e.name);
            Assert.IsFalse(string.IsNullOrWhiteSpace(level.tutorialHint), $"{e.name} sem balão.");
            Assert.IsTrue(level.monsterPlacements.Any(m => m.monsterType == e.newMonster), $"{e.name} sem {e.newMonster}.");
        }
    }

    [Test]
    public void StoredSolutions_AreValid() {
        foreach (var e in Expected) {
            LevelData level = Load(e.name);
            BoardModel board = level.CreateBoard();
            foreach (Vector2Int s in level.solution) Assert.IsTrue(board.SetState(new GridPos(s.x, s.y), CellState.Soldier), e.name);
            Assert.IsTrue(RuleValidator.Validate(board).IsValid, $"Solução salva de {e.name} não vale.");
        }
    }

    [Test]
    public void LevelsWithClues_HaveUniqueSolution() {
        foreach (var e in Expected.Where(x => x.clues)) {
            Assert.IsTrue(PuzzleSolver.Solve(Load(e.name).CreateBoard()).IsUnique, $"{e.name} tem mais de uma solução.");
        }
    }

    [Test]
    public void MatchScene_StartsWithTheTutorial() {
        // Cena de preview: não mexe no que estiver aberto no Editor.
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(
            "Assets/Scenes/Gameplay/LevelTest/level_01_test.unity");
        try {
            LevelManager manager = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<LevelManager>(true)).First();
            var levels = (LevelData[])typeof(LevelManager)
                .GetField("allLevels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(manager);
            CollectionAssert.AreEqual(Expected.Select(e => e.name).ToArray(), levels.Select(l => l.name).ToArray());
        } finally {
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
