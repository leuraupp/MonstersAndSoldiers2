// Scripts/Data/LevelData.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevel", menuName = "Battle Grid/Level Data")]
public class LevelData : ScriptableObject {
    [Header("Basic Info")]
    public string levelName;
    public int levelID;

    [Header("Grid Configuration")]
    [Tooltip("Number of rows in the grid")]
    public int rows = 8;

    [Tooltip("Number of columns in the grid")]
    public int columns = 8;

    [Header("Monsters")]
    public List<MonsterPlacement> monsterPlacements = new List<MonsterPlacement>();

    [Header("Visuals")]
    public Sprite backgroundSprite;
    public Color backgroundColor = Color.white;
}