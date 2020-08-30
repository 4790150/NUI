using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace NUI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NInputField), true)]
    public class NInputFieldEditor : UnityEditor.UI.TextEditor
    {
        #region NText
        private SerializedProperty m_GradientColor;
        private SerializedProperty m_BottomColor;

        private SerializedProperty m_Outline;
        private SerializedProperty m_OutlineSize;
        private SerializedProperty m_OutlineColor;

        private SerializedProperty m_SpritePackage;
        private SerializedProperty m_Sprites;
        private SerializedProperty m_DefaultAnimLength;
        private SerializedProperty m_DefaultAnimFrame;
        private SerializedProperty m_DefaultSpriteScale;
        private SerializedProperty m_DefaultSpriteAlign;
        private SerializedProperty m_ParagraphIndent;
        private SerializedProperty m_OverflowEllipsis;
        private SerializedProperty m_MaxOverflowWidth;
        #endregion

        #region NInputField
        SerializedProperty m_ContentType;
        SerializedProperty m_LineType;
        SerializedProperty m_InputType;
        SerializedProperty m_CharacterValidation;
        SerializedProperty m_KeyboardType;
        SerializedProperty m_CharacterLimit;
        SerializedProperty m_CaretBlinkRate;
        SerializedProperty m_CaretWidth;
        SerializedProperty m_CaretColor;
        SerializedProperty m_CustomCaretColor;
        SerializedProperty m_SelectionColor;
        SerializedProperty m_HideMobileInput;
        SerializedProperty m_Placeholder;
        SerializedProperty m_OnValueChanged;
        SerializedProperty m_OnEndEdit;
        SerializedProperty m_ReadOnly;

        AnimBool m_CustomColor;
        #endregion

        protected override void OnEnable()
        {
            base.OnEnable();

            #region NText
            m_GradientColor = serializedObject.FindProperty("_gradientColor");
            m_BottomColor = serializedObject.FindProperty("_bottomColor");

            m_Outline = serializedObject.FindProperty("_outline");
            m_OutlineSize = serializedObject.FindProperty("_outlineSize");
            m_OutlineColor = serializedObject.FindProperty("_outlineColor");

            m_SpritePackage = serializedObject.FindProperty("_spritePackage");
            m_Sprites = serializedObject.FindProperty("_sprites");
            m_DefaultAnimLength = serializedObject.FindProperty("_defaultAnimLength");
            m_DefaultAnimFrame = serializedObject.FindProperty("_defaultAnimFrame");
            m_DefaultSpriteScale = serializedObject.FindProperty("_defaultSpriteScale");
            m_DefaultSpriteAlign = serializedObject.FindProperty("_defaultSpriteAlign");
            m_ParagraphIndent = serializedObject.FindProperty("_paragraphIndent");
            m_OverflowEllipsis = serializedObject.FindProperty("_overflowEllipsis");
            m_MaxOverflowWidth = serializedObject.FindProperty("_maxOverflowWidth");
            #endregion

            #region NInputField
            m_ContentType = serializedObject.FindProperty("m_ContentType");
            m_LineType = serializedObject.FindProperty("m_LineType");
            m_InputType = serializedObject.FindProperty("m_InputType");
            m_CharacterValidation = serializedObject.FindProperty("m_CharacterValidation");
            m_KeyboardType = serializedObject.FindProperty("m_KeyboardType");
            m_CharacterLimit = serializedObject.FindProperty("m_CharacterLimit");
            m_CaretBlinkRate = serializedObject.FindProperty("m_CaretBlinkRate");
            m_CaretWidth = serializedObject.FindProperty("m_CaretWidth");
            m_CaretColor = serializedObject.FindProperty("m_CaretColor");
            m_CustomCaretColor = serializedObject.FindProperty("m_CustomCaretColor");
            m_SelectionColor = serializedObject.FindProperty("m_SelectionColor");
            m_HideMobileInput = serializedObject.FindProperty("m_HideMobileInput");
            m_Placeholder = serializedObject.FindProperty("m_Placeholder");
            m_OnValueChanged = serializedObject.FindProperty("m_OnValueChanged");
            m_OnEndEdit = serializedObject.FindProperty("m_OnEndEdit");
            m_ReadOnly = serializedObject.FindProperty("m_ReadOnly");

            m_CustomColor = new AnimBool(m_CustomCaretColor.boolValue);
            m_CustomColor.valueChanged.AddListener(Repaint);
            #endregion
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            m_CustomColor.valueChanged.RemoveListener(Repaint);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            base.OnInspectorGUI();

            EditorGUILayout.Space();

            #region NText
            EditorGUILayout.PropertyField(m_SpritePackage, true);
            if (null == m_SpritePackage.objectReferenceValue)
            {
                EditorGUILayout.PropertyField(m_Sprites, true);
                EditorGUILayout.PropertyField(m_DefaultAnimLength);
                EditorGUILayout.PropertyField(m_DefaultAnimFrame);
            }
            EditorGUILayout.PropertyField(m_DefaultSpriteScale, true);
            EditorGUILayout.PropertyField(m_DefaultSpriteAlign, true);
            EditorGUILayout.PropertyField(m_ParagraphIndent);
            EditorGUILayout.PropertyField(m_OverflowEllipsis);
            if (((NText)target).horizontalOverflow == HorizontalWrapMode.Overflow)
                EditorGUILayout.PropertyField(m_MaxOverflowWidth);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_GradientColor);
            if (m_GradientColor.boolValue)
            {
                EditorGUILayout.PropertyField(m_BottomColor);
            }

            EditorGUILayout.PropertyField(m_Outline);
            if (m_Outline.boolValue)
            {
                EditorGUILayout.PropertyField(m_OutlineSize);
                EditorGUILayout.PropertyField(m_OutlineColor);
            }
            #endregion

            EditorGUILayout.Separator();

            #region NInputField
            EditorGUILayout.PropertyField(m_CharacterLimit);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_ContentType);
            if (!m_ContentType.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;

                if (m_ContentType.enumValueIndex == (int)InputField.ContentType.Standard ||
                    m_ContentType.enumValueIndex == (int)InputField.ContentType.Autocorrected ||
                    m_ContentType.enumValueIndex == (int)InputField.ContentType.Custom)
                    EditorGUILayout.PropertyField(m_LineType);

                if (m_ContentType.enumValueIndex == (int)InputField.ContentType.Custom)
                {
                    EditorGUILayout.PropertyField(m_InputType);
                    EditorGUILayout.PropertyField(m_KeyboardType);
                    EditorGUILayout.PropertyField(m_CharacterValidation);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_Placeholder);
            EditorGUILayout.PropertyField(m_CaretBlinkRate);
            EditorGUILayout.PropertyField(m_CaretWidth);

            EditorGUILayout.PropertyField(m_CustomCaretColor);
            m_CustomColor.target = m_CustomCaretColor.boolValue;
            if (EditorGUILayout.BeginFadeGroup(m_CustomColor.faded))
            {
                EditorGUILayout.PropertyField(m_CaretColor);
            }
            EditorGUILayout.EndFadeGroup();
            
            EditorGUILayout.PropertyField(m_SelectionColor);
            EditorGUILayout.PropertyField(m_HideMobileInput);
            EditorGUILayout.PropertyField(m_ReadOnly);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_OnValueChanged);
            EditorGUILayout.PropertyField(m_OnEndEdit);

            #endregion

            serializedObject.ApplyModifiedProperties();
        }
    }
}