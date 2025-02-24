#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        LevelData levelData = (LevelData)target;

        foreach (var monster in levelData.monsterPlacements) {
            if (monster.gridPosition.x >= levelData.columns ||
                monster.gridPosition.y >= levelData.rows) {
                EditorGUILayout.HelpBox($"Monstro em posição inválida: {monster.gridPosition}", MessageType.Error);
            }
        }
    }
}
#endif