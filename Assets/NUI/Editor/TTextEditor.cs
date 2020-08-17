using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace NUI
{
    [CustomEditor(typeof(NText))]
    [CanEditMultipleObjects]
    public class NTextEditor : UnityEditor.UI.TextEditor
    {
        private SerializedProperty m_GradientColor;
        private SerializedProperty m_BottomColor;

        private SerializedProperty m_Outline;
        private SerializedProperty m_OutlineSize;
        private SerializedProperty m_OutlineColor;

        //private SerializedProperty m_Shadow;
        //private SerializedProperty m_ShadowColor;
        //private SerializedProperty m_ShadowDistance;

        private SerializedProperty m_SpritePackage;
        private SerializedProperty m_Sprites;
        private SerializedProperty m_DefaultAnimLength;
        private SerializedProperty m_DefaultAnimFrame;
        private SerializedProperty m_DefaultSpriteScale;
        private SerializedProperty m_DefaultSpriteAlign;
        private SerializedProperty m_ParagraphIndent;
        private SerializedProperty m_OverflowEllipsis;
        private SerializedProperty m_MaxOverflowWidth;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_GradientColor = serializedObject.FindProperty("_gradientColor");
            m_BottomColor = serializedObject.FindProperty("_bottomColor");

            m_Outline = serializedObject.FindProperty("_outline");
            m_OutlineSize = serializedObject.FindProperty("_outlineSize");
            m_OutlineColor = serializedObject.FindProperty("_outlineColor");

            //m_Shadow = serializedObject.FindProperty("_shadow");
            //m_ShadowColor = serializedObject.FindProperty("_shadowColor");
            //m_ShadowDistance = serializedObject.FindProperty("_shadowDistance");

            m_SpritePackage = serializedObject.FindProperty("_spritePackage");
            m_Sprites = serializedObject.FindProperty("_sprites");
            m_DefaultAnimLength = serializedObject.FindProperty("_defaultAnimLength");
            m_DefaultAnimFrame = serializedObject.FindProperty("_defaultAnimFrame");
            m_DefaultSpriteScale = serializedObject.FindProperty("_defaultSpriteScale");
            m_DefaultSpriteAlign = serializedObject.FindProperty("_defaultSpriteAlign");
            m_ParagraphIndent = serializedObject.FindProperty("_paragraphIndent");
            m_OverflowEllipsis = serializedObject.FindProperty("_overflowEllipsis");
            m_MaxOverflowWidth = serializedObject.FindProperty("_maxOverflowWidth");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(@"<sprite index=0 scale = 1.0 align = ""top|middle|bottom""/>
<sprite index=10 animLength=5 animFrame=30 /> 
动画由第10张图片开始,共5张图片组成,每30帧换一张图片
<link=""param"">超链接</link>
<size=+10> 字体尺寸+10</size>
<size=-10> 字体尺寸-10</size>
<size=20> 字体尺寸20</size>
<b>粗体</b>
<i>斜体</i>
<u color=white>下划线</u>
<s color=white>删除线</s>
<color=#FFF>颜色文字</color>
<color=#FFF7>颜色文字</color>
<color=#FF00FF>颜色文字</color>
<color=#FF00FF00>颜色文字</color>
<color=red|blue|black|green|white|orange|purple|yellow>颜色文字</color>
<gradient top=white bottom=black> 渐变色
<noparse>不解析</noparse>", MessageType.None, true);

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

            //EditorGUILayout.PropertyField(m_Shadow);
            //if (m_Shadow.boolValue)
            //{
            //    EditorGUILayout.PropertyField(m_ShadowDistance);
            //    EditorGUILayout.PropertyField(m_ShadowColor);
            //}

            serializedObject.ApplyModifiedProperties();
        }
    }
}