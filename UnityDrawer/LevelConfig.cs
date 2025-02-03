using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityDraw {
    [CreateAssetMenu(fileName = "LocationConfig", menuName = "Scriptable Objects/LocationConfig")]
    public class LevelConfig : ScriptableObject {
        [SerializeField] string _locationName;
        [SerializeField] string _savePath = "/LevelsSaves/";

        [SerializeField] public List<FieldPreset> fieldPresets;
        [SerializeField] MyDictionary<EnemyType, List<GameObject>> _enemies = new(new());
        public Dictionary<EnemyType, List<GameObject>> enemies = new();
        public List<GameObject> traps = new();

        void OnValidate() {
            if (_enemies.ContainEqualsKey()) {
                Debug.LogError("Equals keys!");
                return;
            }
            enemies = new Dictionary<EnemyType, List<GameObject>>(_enemies.GetDictionary());
        }

    #if UNITY_EDITOR

        string filePath => Application.dataPath + _savePath + $"{_locationName}.json";

        [ContextMenu(nameof(SaveFieldPresets))]
        void SaveFieldPresets() {
            var json = JsonUtility.ToJson(new FieldPresets(fieldPresets), true);
            if (!System.IO.Directory.Exists(Application.dataPath + _savePath))
                System.IO.Directory.CreateDirectory(Application.dataPath + _savePath);
            if (!System.IO.File.Exists(filePath))
                System.IO.File.Create(filePath);
            System.IO.File.WriteAllText(filePath, json);
        }
        
        [ContextMenu(nameof(LoadFieldPresets))]
        void LoadFieldPresets() {
            if (!System.IO.File.Exists(filePath)) {
                Debug.LogError($"Field Presets of '{_locationName}' doesnt found");
                return;
            }

            var presetsText = System.IO.File.ReadAllText(filePath);
            var presets = JsonUtility.FromJson<FieldPresets>(presetsText);
            fieldPresets = presets.fieldPresets;
        }

        class FieldPresets {
            public List<FieldPreset> fieldPresets;

            public FieldPresets(IEnumerable<FieldPreset> presets) {
                fieldPresets = presets.ToList();
            }
        }

    #endif
    }
}
