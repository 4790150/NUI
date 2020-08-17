using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace NUI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NInputField), true)]
    public class NInputFieldEditor : SelectableEditor
    {
        SerializedProperty m_TextComponent;
        SerializedProperty m_Text;
        SerializedProperty m_ContentType;
        SerializedProperty m_LineType;
        SerializedProperty m_InputType;
        SerializedProperty m_CharacterValidation;
        SerializedProperty m_KeyboardType;
        SerializedProperty m_CharacterLimit;
        SerializedProperty m_CaretBlinkRate;
        SerializedProperty m_SelectionColor;
        SerializedProperty m_HideMobileInput;
        SerializedProperty m_Placeholder;
        SerializedProperty m_OnValueChanged;
        SerializedProperty m_OnEndEdit;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_TextComponent = serializedObject.FindProperty("m_TextComponent");
            m_Text = serializedObject.FindProperty("m_Text");
            m_ContentType = serializedObject.FindProperty("m_ContentType");
            m_LineType = serializedObject.FindProperty("m_LineType");
            m_InputType = serializedObject.FindProperty("m_InputType");
            m_CharacterValidation = serializedObject.FindProperty("m_CharacterValidation");
            m_KeyboardType = serializedObject.FindProperty("m_KeyboardType");
            m_CharacterLimit = serializedObject.FindProperty("m_CharacterLimit");
            m_CaretBlinkRate = serializedObject.FindProperty("m_CaretBlinkRate");
            m_SelectionColor = serializedObject.FindProperty("m_SelectionColor");
            m_HideMobileInput = serializedObject.FindProperty("m_HideMobileInput");
            m_Placeholder = serializedObject.FindProperty("m_Placeholder");
            m_OnValueChanged = serializedObject.FindProperty("m_OnValueChanged");
            m_OnEndEdit = serializedObject.FindProperty("m_OnEndEdit");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            base.OnInspectorGUI();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_TextComponent);

            EditorGUI.BeginDisabledGroup(m_TextComponent == null || m_TextComponent.objectReferenceValue == null);

            EditorGUILayout.PropertyField(m_Text);
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
            EditorGUILayout.PropertyField(m_SelectionColor);
            EditorGUILayout.PropertyField(m_HideMobileInput);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_OnValueChanged);
            EditorGUILayout.PropertyField(m_OnEndEdit);

            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}