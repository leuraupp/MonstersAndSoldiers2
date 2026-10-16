#if UNITY_EDITOR
using System.Collections.Generic;
using MonstersAndSoldiers.Core;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        LevelData levelData = (LevelData)target;
        var seen = new HashSet<Vector2Int>();
        int requiredTotal = 0;

        foreach (MonsterPlacement monster in levelData.monsterPlacements) {
            Vector2Int p = monster.gridPosition;
            if (p.x < 0 || p.x >= levelData.rows || p.y < 0 || p.y >= levelData.columns) {
                EditorGUILayout.HelpBox($"Monstro em posição inválida: {p} (x = linha, y = coluna)", MessageType.Error);
            }
            if (!seen.Add(p)) {
                EditorGUILayout.HelpBox($"Dois monstros na mesma casa: {p}", MessageType.Error);
            }
            requiredTotal += MonsterRules.RequiredSoldiers(monster.monsterType);
        }

        bool anyClues = levelData.rowClues.Length > 0 || levelData.columnClues.Length > 0;
        if (anyClues && !levelData.HasClues) {
            EditorGUILayout.HelpBox("As pistas precisam ter uma entrada por linha e uma por coluna (ou ficar vazias).", MessageType.Warning);
        } else if (levelData.HasClues) {
            int rowSum = 0, colSum = 0;
            foreach (int v in levelData.rowClues) rowSum += v;
            foreach (int v in levelData.columnClues) colSum += v;
            if (rowSum != requiredTotal || colSum != requiredTotal) {
                EditorGUILayout.HelpBox($"Soma das pistas (linhas {rowSum}, colunas {colSum}) diferente do total exigido pelos monstros ({requiredTotal}).", MessageType.Warning);
            }
        }

        EditorGUILayout.HelpBox($"Soldados na solução: {requiredTotal}", MessageType.Info);
    }
}
#endif
