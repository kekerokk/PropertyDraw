using System;
using UnityEngine;

[Serializable]
public class FieldProperty { // TODO add some interface implementations for separate
    public Vector2Int cords = new(-1, -1);
    public FieldType fieldType;
    public EnemyType enemyType;
    public GameObject enemy;
    public GameObject trap;
}