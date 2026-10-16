using MonstersAndSoldiers.Core;
using UnityEngine;

[System.Serializable]
public class MonsterPlacement {
    public MonsterType monsterType;

    [Tooltip("x = linha (row), y = coluna (col)")]
    public Vector2Int gridPosition;
}
