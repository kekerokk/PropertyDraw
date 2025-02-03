using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace UnityDraw {
    [CustomPropertyDrawer(typeof(FieldPreset))]
    public class FieldPresetDrawer : PropertyDrawer {
        SerializedProperty _fieldSize;
        SerializedProperty _fields;

        SerializedProperty _clickedFieldProperty, _clickedFieldPos, _clickedFieldType, _clickedEnemyType, _clickedEnemy, _clickedTrap;

        Texture2D _yellowTexture, _redTexture, _grayTexture, _purpleTexture, _blackTexture;
        Texture2D yellowTexture {
            get {
                if (_yellowTexture.IsUnityNull()) {
                    _yellowTexture = Resources.Load<Texture2D>("YellowTexture");
                }
                return _yellowTexture;
            }
        }
        Texture2D redTexture {
            get {
                if (_redTexture.IsUnityNull()) {
                    _redTexture = Resources.Load<Texture2D>("RedTexture");
                }
                return _redTexture;
            }
        }
        Texture2D purpleTexture {
            get {
                if (_purpleTexture.IsUnityNull()) {
                    _purpleTexture = Resources.Load<Texture2D>("PurpleTexture");
                }
                return _purpleTexture;
            }
        }
        Texture2D grayTexture {
            get {
                if (_grayTexture.IsUnityNull()) {
                    _grayTexture = Resources.Load<Texture2D>("GrayTexture");
                }
                return _grayTexture;
            }
        }
        Texture2D blackTexture {
            get {
                if (_blackTexture.IsUnityNull()) {
                    _blackTexture = Resources.Load<Texture2D>("BlackTexture");
                }
                return _blackTexture;
            }
        }

        const float HEIGHT_PER_CELL = 22, Y_MARGIN = 2f;
        float _fieldsCount = 4;

        bool _fastCellChange;
        FieldType _cellChangeType;

        SerializedProperty GetRelative(SerializedProperty property, string name) => property.FindPropertyRelative(name);
        SerializedProperty GetObjectRelative(SerializedProperty property, string name) => property.serializedObject.FindProperty(name);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            Rect foldoutBox = new(position.x, position.y, position.size.x, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutBox, property.isExpanded, label);
            position.y += EditorGUIUtility.singleLineHeight + Y_MARGIN;

            if (!property.isExpanded) return;

            _fieldSize = GetRelative(property, "fieldSize");
            _fields = GetRelative(property, "fields");

            Vector2Int oldSize = DrawFieldSize(ref position);

            DrawFastChangeToggle(ref position);

            if (oldSize == _fieldSize.vector2IntValue) DrawField(position, property);
        }
        Vector2Int DrawFieldSize(ref Rect position) {
            var oldSize = _fieldSize.vector2IntValue;

            EditorGUI.BeginChangeCheck();

            _fieldSize.vector2IntValue = EditorGUI.Vector2IntField(position, "Field Size", _fieldSize.vector2IntValue);
            position.y += EditorGUIUtility.singleLineHeight + Y_MARGIN;

            if (EditorGUI.EndChangeCheck()) {
                Vector2Int size = _fieldSize.vector2IntValue;
                if (size is { x: > -1, y: > -1 })
                    _fields.arraySize = size.x * size.y;
            }

            EditorGUIUtility.labelWidth = 80;
            
            EditorGUI.LabelField(new Rect(position.x, position.y, 150, EditorGUIUtility.singleLineHeight), $"Array Size: {_fields.arraySize}");
            position.y += EditorGUIUtility.singleLineHeight + Y_MARGIN;

            return oldSize;
        }
        void DrawFastChangeToggle(ref Rect position) {
            EditorGUIUtility.labelWidth = 120;

            _fastCellChange = EditorGUI.Toggle(new(position.x, position.y, 20, 20), "OneClick Change", _fastCellChange);
            position.y += EditorGUIUtility.singleLineHeight + Y_MARGIN;

            if (_fastCellChange) {
                _cellChangeType = (FieldType)EditorGUI.EnumPopup(new(position.x, position.y, 200, EditorGUIUtility.singleLineHeight), _cellChangeType);
                position.y += EditorGUIUtility.singleLineHeight + Y_MARGIN;
            }
        }

        void DrawField(Rect position, SerializedProperty property) {
            var size = _fieldSize.vector2IntValue;
            if (size.x < 1 || size.y < 1) return;
            
            var infoRect = new Rect(position.x + HEIGHT_PER_CELL * size.x, position.y, position.width - HEIGHT_PER_CELL * size.x - 10, position.height);
            var fieldBottom = new Rect(position.x, position.y + 21 * (size.y - 1), position.width, position.height);

            GUIContent content = new();
            content.image = grayTexture;
            GUIStyle style = new();
            style.active.background = yellowTexture;
            style.normal.textColor = Color.white;

            // Field building
            for (int y = size.y - 1; y >= 0; y--) {
                for (int x = size.x - 1; x >= 0; x--) {

                    var element = _fields.GetArrayElementAtIndex(x + y * size.x);
                    var elementPos = GetRelative(element, "cords");
                    elementPos.vector2IntValue = new(x, y);
                    var fieldType = GetRelative(element, "fieldType");
                    var enemyType = GetRelative(element, "enemyType");
                    var enemy = GetRelative(element, "enemy");
                    var trap = GetRelative(element, "trap");
                    var fieldTypeId = fieldType.enumValueIndex;

                    style.normal.background = fieldTypeId switch {
                        0 => grayTexture,
                        1 => GetTexture(trap.objectReferenceValue?.name ?? "NULL") ?? purpleTexture,
                        2 => GetTexture(enemy.objectReferenceValue?.name ?? "NULL") ?? redTexture,
                        3 => blackTexture,
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    string text = elementPos.vector2IntValue == _clickedFieldPos?.vector2IntValue ? "*" : "";

                    if (GUI.Button(new(position.x + (x * 21), fieldBottom.y - (y * 21), HEIGHT_PER_CELL - 2, HEIGHT_PER_CELL - 2), text, style)) {
                        _clickedFieldProperty = element;
                        _clickedFieldPos = elementPos;
                        _clickedFieldType = fieldType;
                        _clickedEnemyType = enemyType;
                        _clickedEnemy = enemy;

                        if (_fastCellChange) _clickedFieldType.enumValueIndex = (int)_cellChangeType;
                    }
                }
            }

            // Clicked field stats
            if (_clickedFieldProperty.IsUnityNull()) return;

            DrawCellSettings(infoRect, property);
        }

        void DrawCellSettings(Rect infoPosition, SerializedProperty property) {
            EditorGUIUtility.labelWidth = 70;

            EditorGUI.Vector2IntField(infoPosition, "pos", _clickedFieldPos.vector2IntValue);
            infoPosition.y += EditorGUIUtility.singleLineHeight + Y_MARGIN;

            EditorGUI.PropertyField(infoPosition, _clickedFieldType, new($"Field Type"), true);
            infoPosition.y += EditorGUIUtility.singleLineHeight + Y_MARGIN;
            
            var targetObject = property.serializedObject.targetObject;
            var targetObjectClassType = targetObject.GetType();

            switch (_clickedFieldType.enumValueIndex) {
                case (int)FieldType.Enemy: {
                    EditorGUI.PropertyField(infoPosition, GetRelative(_clickedFieldProperty, "enemyType"), new("Enemy Type"), true);
                    infoPosition.y += EditorGUIUtility.singleLineHeight + Y_MARGIN;
                    
                    var field = targetObjectClassType
                        .GetField("enemies");

                    if (field == null) {
                        Debug.LogError("Dont have enemies");
                        return;
                    }

                    Dictionary<EnemyType, List<GameObject>> enemiesDict = (Dictionary<EnemyType, List<GameObject>>)field.GetValue(targetObject);
                    GameObject enemy = GetRelative(_clickedFieldProperty, "enemy").objectReferenceValue as GameObject;
                    EnemyType enemyType = (EnemyType)_clickedEnemyType.enumValueIndex;

                    if (!enemiesDict.TryGetValue(enemyType, out var enemies)) {
                        EditorGUI.HelpBox(new Rect(infoPosition.x, infoPosition.y, infoPosition.width, EditorGUIUtility.singleLineHeight * 2),
                            $"'{enemyType}' enemy was not present in the dictionary", MessageType.Warning);
                        // clickedFieldProp.enemy = null;
                        return;
                    }
                    if (enemies.Count == 0) {
                        EditorGUI.HelpBox(new Rect(infoPosition.x, infoPosition.y, infoPosition.width, EditorGUIUtility.singleLineHeight * 2),
                            $"Not enough enemies variants", MessageType.Warning);
                        // clickedFieldProp.enemy = null;
                        return;
                    }
                    if (enemies.Any(x => x.IsUnityNull())) {
                        Debug.LogError($"Some enemy is null");
                        return;
                    }

                    List<string> names = new();
                    enemies.ForEach(x => names.Add(x.name));

                    int id = 0;
                    if (enemy) {
                        id = enemies.Contains(enemy) ? enemies.IndexOf(enemy) : 0;
                    }

                    // Enemy variants

                    id = EditorGUI.Popup(
                        new Rect(infoPosition.x, infoPosition.y, infoPosition.width, EditorGUIUtility.singleLineHeight),
                        id,
                        names.ToArray());
                    _clickedEnemy.objectReferenceValue = enemies[id];
                    
                    break;
                }
                case (int)FieldType.Trap:
                    List<GameObject> traps = targetObjectClassType.GetField("traps").GetValue(targetObject) as List<GameObject>;

                    if (traps.IsUnityNull()) {
                        Debug.LogError("Traps is null");
                        return;
                    }
                    
                    // Issues 
                    if (traps.Count < 1) {
                        EditorGUI.HelpBox(new Rect(infoPosition.x, infoPosition.y, infoPosition.width, EditorGUIUtility.singleLineHeight * 2),
                            $"traps was not present in the dictionary", MessageType.Warning);
                        return;
                    }
                    if (traps.Any(x=>x.IsUnityNull())) {
                        EditorGUI.HelpBox(new Rect(infoPosition.x, infoPosition.y, infoPosition.width, EditorGUIUtility.singleLineHeight * 2),
                            $"Some traps is null", MessageType.Warning);
                        return;
                    }

                    // Trap variants
                    List<string> trapNames = new();
                    traps.ForEach(x => { trapNames.Add(x.name); });

                    _clickedTrap = GetRelative(_clickedFieldProperty, "trap");
                    GameObject trap = _clickedTrap.objectReferenceValue as GameObject;
                    int trapId = 0;
                    if (trap) {
                        trapId = traps.Contains(trap) ? traps.IndexOf(trap) : 0;
                    }

                    trapId = EditorGUI.Popup(infoPosition, trapId, trapNames.ToArray());
                    infoPosition.y += EditorGUIUtility.singleLineHeight + 2;
                    
                    _clickedTrap.objectReferenceValue = traps[trapId];
                    
                    break;
            }
        }

        Texture2D GetTexture(string name) {
            Texture2D icon = Resources.Load<Texture2D>($"{name}Icon");

            return icon;
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            var size = _fieldSize?.vector2IntValue ?? Vector2Int.zero;

            if (size.x <= 1 || size.y <= 1) return 200;
            if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;
            return size.y * (HEIGHT_PER_CELL + Y_MARGIN) + (EditorGUIUtility.singleLineHeight + Y_MARGIN) * _fieldsCount;
        }
    }
}
