using UnityEngine;

[System.Serializable]
public class MonsterPlacement {
    public MonsterType monsterType; // Slime, Goblin, Orc
    public Vector2Int gridPosition; // Coordenadas (x,y) no grid
}

public enum MonsterType {
    Slime,
    Goblin,
    Orc
}