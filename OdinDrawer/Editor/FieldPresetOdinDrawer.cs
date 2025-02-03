using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace OdinDraw {
    public class FieldPresetOdinDrawer : OdinValueDrawer<FieldPreset> {
        InspectorProperty _fieldSize;
        InspectorProperty _fields;
        InspectorProperty _enemies;
        InspectorProperty _traps;

        LocalPersistentContext<bool> _fastCellChange;
        LocalPersistentContext<FieldProperty> _clickedField;
        FieldProperty clickedFieldProp => _clickedField.Value;

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
                    Debug.Log("TextureIsNull");
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

        const float FIELD_CELL_SIZE = 20;

        bool _isExpanded;
        FieldType _cellChangeType;

        protected override void Initialize() {
            _fieldSize = Property.Children["fieldSize"];
            _fields = Property.Children["fields"];
            var fields = (FieldProperty[])_fields.ValueEntry.WeakSmartValue;
            CheckFieldsForNull(fields);
            InspectorProperty parent = TryGetRootParent(Property);
            
            _enemies = parent.Children["enemies"];
            _traps = parent.Children["traps"];

            _fastCellChange = this.GetPersistentValue<bool>("FieldPresetOdinDrawer._fastCellChange");
            _clickedField = this.GetPersistentValue<FieldProperty>("FieldPresetOdinDrawer._clickedField");
        }
        void CheckFieldsForNull(FieldProperty[] fields) {
            for (int i = 0; i < fields.Length; i++) {
                if (fields[i].IsUnityNull())
                    fields[i] = new();
            }
        }
        InspectorProperty TryGetRootParent(InspectorProperty property) {
            var parent = property.Parent;

            if (!parent.IsUnityNull())
                return parent.Name != $"$ROOT" ? TryGetRootParent(parent) : parent;

            Debug.LogError($"property '{property.Name}' is $ROOT");
            return null;
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            Rect position = EditorGUILayout.GetControlRect();

            if (label == null) label = new GUIContent("NULL");

            _isExpanded = SirenixEditorGUI.Foldout(position, _isExpanded, label);
            position.y += EditorGUIUtility.singleLineHeight + 2;

            if (!_isExpanded) return;

            _fieldSize.Draw();
            position.y += EditorGUIUtility.singleLineHeight + 2;

            // Array Size change
            List<FieldProperty> fields = ((FieldProperty[])_fields.ValueEntry.WeakSmartValue).ToList();
            var oldSize = fields.Count;
            Vector2Int fieldSize = (Vector2Int)_fieldSize.ValueEntry.WeakSmartValue;
            fieldSize.x = fieldSize.x <= 0 ? 1 : fieldSize.x;
            fieldSize.y = fieldSize.y <= 0 ? 1 : fieldSize.y;
            fields.SetLength(fieldSize.x * fieldSize.y);
            if (oldSize != fields.Count) _fields.ValueEntry.WeakSmartValue = fields.ToArray();
            SirenixEditorGUI.InfoMessageBox($"Array Size: {fields.Count}");
            position.y += EditorGUIUtility.singleLineHeight + 2;

            _fastCellChange.Value =
                SirenixEditorGUI.ToolbarToggle(_fastCellChange.Value, "OneClick Change");
            position.y += EditorGUIUtility.singleLineHeight + 2;

            if (_fastCellChange.Value) {
                _cellChangeType = (FieldType)SirenixEditorFields.EnumDropdown(GUIContent.none, _cellChangeType);

                position.y += EditorGUIUtility.singleLineHeight + 2;
            }

            if (oldSize == fields.Count) {
                DrawField();
            } else {
                CheckFieldsForNull(_fields.ValueEntry.WeakSmartValue as FieldProperty[]);
            }
        }

        void DrawField() {
            FieldProperty[] fields = (FieldProperty[])_fields.ValueEntry.WeakSmartValue;
            Vector2Int size = (Vector2Int)_fieldSize.ValueEntry.WeakSmartValue;

            if (size.x < 1 || size.y < 1) return;

            Rect position = EditorGUILayout.GetControlRect(false, size.y * 21);
            var fieldWidth = size.x * (FIELD_CELL_SIZE + 1);
            var infoRect = new Rect(position.x + fieldWidth + 2f, position.y, position.width - fieldWidth, position.height);
            var fieldBottom = new Rect(position.x, position.y + 21 * (size.y - 1), position.width, position.height);

            GUIContent content = new();
            content.image = grayTexture;
            GUIStyle style = new();
            style.active.background = yellowTexture;
            style.normal.textColor = Color.white;

            // Field building
            for (int y = size.y - 1; y >= 0; y--) {
                for (int x = size.x - 1; x >= 0; x--) {
                    var element = fields[x + y * size.x];
                    var elementPos = element.cords = new(x, y);
                    var fieldType = element.fieldType;
                    var enemyType = element.enemyType;
                    var enemyId = "";
                    int fieldTypeId = (int)fieldType;
                    int enemyTypeId = (int)enemyType;

                    var texture = fieldTypeId switch {
                        0 => grayTexture,
                        1 => GetTexture(element.trap.IsUnityNull() ? "NULL" : element.trap.name) ?? purpleTexture,
                        2 => GetTexture(element.enemy?.name ?? "NULL") ?? _redTexture,
                        3 => blackTexture,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    style.normal.background = texture;

                    string text = elementPos == clickedFieldProp?.cords ? "*" : "";
                    text += fieldTypeId == 2
                        ? enemyTypeId switch {
                            0 => $"M{enemyId}",
                            1 => $"R{enemyId}",
                            2 => $"S{enemyId}",
                            _ => $"?{enemyId}"
                        }
                        : "";

                    if (fieldTypeId == 2) {
                        if (texture != redTexture) text = "";
                        if (!clickedFieldProp.IsUnityNull() && clickedFieldProp == element) text = "*";
                    }

                    if (GUI.Button(new(position.x + x * (FIELD_CELL_SIZE + 1), fieldBottom.y - y * (FIELD_CELL_SIZE + 1), FIELD_CELL_SIZE, FIELD_CELL_SIZE), text, style)) {
                        _clickedField.Value = element;

                        if (_fastCellChange.Value) clickedFieldProp.fieldType = _cellChangeType;
                    }
                }
            }

            if (clickedFieldProp.IsUnityNull()) return;

            DrawCellSettings(infoRect);
        }

        // Clicked field stats
        void DrawCellSettings(Rect infoPosition) {
            infoPosition.height = EditorGUIUtility.singleLineHeight;
            Rect textField = infoPosition;
            var style = new GUIStyle();

            // Coords
            EditorGUIUtility.labelWidth = style.CalcSize("Coords:").x + 5f;
            EditorGUI.Vector2Field(textField, "Coords:", clickedFieldProp.cords);
            infoPosition.y += EditorGUIUtility.singleLineHeight + 2;

            // Field Type
            clickedFieldProp.fieldType = (FieldType)SirenixEditorFields.EnumDropdown(infoPosition, clickedFieldProp.fieldType);
            infoPosition.y += EditorGUIUtility.singleLineHeight + 2;

            switch (clickedFieldProp.fieldType) {
                case FieldType.Enemy:
                    // Enemy Type
                    clickedFieldProp.enemyType = (EnemyType)SirenixEditorFields.EnumDropdown(infoPosition, clickedFieldProp.enemyType);
                    infoPosition.y += EditorGUIUtility.singleLineHeight + 2;

                    // Issues 
                    if (!((Dictionary<EnemyType, List<GameObject>>)_enemies.ValueEntry.WeakSmartValue).TryGetValue(clickedFieldProp.enemyType, out var enemies)) {
                        infoPosition.height *= 2;
                        infoPosition.x += 15;
                        infoPosition.width -= 15;
                        EditorGUI.HelpBox(infoPosition, $"'{clickedFieldProp.enemyType}' enemy was not present in the dictionary", MessageType.Warning);
                        clickedFieldProp.enemy = null;
                        return;
                    }
                    if (enemies.Count == 0) {
                        infoPosition.height *= 2;
                        infoPosition.x += 15;
                        infoPosition.width -= 15;
                        EditorGUI.HelpBox(infoPosition, $"Not enough enemies variants", MessageType.Warning);
                        clickedFieldProp.enemy = null;
                        return;
                    }

                    List<string> names = new();
                    enemies.ForEach(x => names.Add(x.name));

                    int id = 0;
                    if (!clickedFieldProp.enemy.IsUnityNull()) {
                        id = enemies.Contains(clickedFieldProp.enemy) ? enemies.IndexOf(clickedFieldProp.enemy) : 0;
                    }

                    // Enemy variants
                    id = EditorGUI.Popup(infoPosition, id, names.ToArray());
                    clickedFieldProp.enemy = enemies[id];

                    return;
                case FieldType.Trap:
                    List<GameObject> traps = _traps.ValueEntry.WeakSmartValue as List<GameObject>;
                    
                    if(traps.IsUnityNull()) return;
                    
                    // Issues 
                    if (traps.Count < 1) {
                        infoPosition.height *= 2;
                        infoPosition.x += 15;
                        infoPosition.width -= 15;
                        EditorGUI.HelpBox(infoPosition, $"traps was not present in the dictionary", MessageType.Warning);
                        clickedFieldProp.trap = null;
                        return;
                    }
                    if (traps.Any(x=>x.IsUnityNull())) {
                        infoPosition.height *= 2;
                        infoPosition.x += 15;
                        infoPosition.width -= 15;
                        EditorGUI.HelpBox(infoPosition, $"Some traps is null", MessageType.Warning);
                        clickedFieldProp.trap = null;
                        return;
                    }

                    // Trap variants
                    List<string> trapNames = new();
                    traps.ForEach(x => {
                        if (!x.IsUnityNull())
                            trapNames.Add(x.name);
                    });

                    int trapId = 0;
                    if (!clickedFieldProp.trap.IsUnityNull()) {
                        trapId = traps.Contains(clickedFieldProp.trap) ? traps.IndexOf(clickedFieldProp.trap) : 0;
                    }

                    trapId = EditorGUI.Popup(infoPosition, trapId, trapNames.ToArray());
                    infoPosition.y += EditorGUIUtility.singleLineHeight + 2;
                    clickedFieldProp.trap = traps[trapId];

                    return;
            }
        }
        
        Texture2D GetTexture(string name) {
            Texture2D icon = Resources.Load<Texture2D>($"{name}Icon");

            return icon;
        }
    }
}
