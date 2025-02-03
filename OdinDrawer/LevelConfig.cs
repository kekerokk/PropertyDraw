using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OdinDraw {
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Scriptable Objects/LevelConfig")]
    public class LevelConfig : SerializedScriptableObject {
        [field: SerializeField] public string levelName { get; private set; }
        [SerializeField] public List<FieldPreset> fieldPresets = new();
        [SerializeField] public Dictionary<EnemyType, List<GameObject>> enemies = new() {
            { EnemyType.Melee, new() },
            { EnemyType.Range, new() },
            { EnemyType.Struct, new() },
            { EnemyType.Boss, new() }
        };
        [SerializeField] public List<GameObject> traps = new();

        #if UNITY_EDITOR
        [SerializeField] string _savePath = "/LevelsSaves/";
        string filePath => Application.dataPath + _savePath + $"{levelName}.json";

        [Button]
        void SaveFieldPresets() {
            FieldPresets fields = new FieldPresets(fieldPresets);
            var json = JsonUtility.ToJson(fields, true);
            if (!System.IO.Directory.Exists(Application.dataPath + _savePath))
                System.IO.Directory.CreateDirectory(Application.dataPath + _savePath);
            if (!System.IO.File.Exists(filePath))
                System.IO.File.Create(filePath);
            System.IO.File.WriteAllText(filePath, json);
        }

        [Button]
        void LoadFieldPresets() {
            if (!System.IO.File.Exists(filePath)) {
                Debug.LogError($"Field Presets of '{levelName}' doesnt found");
                return;
            }

            var presetsText = System.IO.File.ReadAllText(filePath);
            var presets = JsonUtility.FromJson<FieldPresets>(presetsText);
            fieldPresets = presets.fieldPresets;
        }

        class FieldPresets {
            public List<FieldPreset> fieldPresets;

            public FieldPresets(List<FieldPreset> presets) {
                fieldPresets = presets;
            }
        }
        
        #endif
    }
}