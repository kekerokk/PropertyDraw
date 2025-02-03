using System;
using UnityEngine;

namespace OdinDraw {
    [Serializable]
    public class FieldPreset {
        [SerializeField] public Vector2Int fieldSize = new Vector2Int(11, 15);
        [SerializeField] public FieldProperty[] fields = new FieldProperty[400];
    }
}