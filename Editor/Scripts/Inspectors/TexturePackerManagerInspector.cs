/*
 *	Created by:  Peter @sHTiF Stefcek
 */
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TexturePacker.Editor
{
    [CustomEditor(typeof(TexturePackerManager))]
    public class TexturePackerManagerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("packOnStart"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxWidth"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxHeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("padding"));
            var clearTexture = serializedObject.FindProperty("clearTexture");
            EditorGUILayout.PropertyField(clearTexture);

            if (clearTexture.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("clearColor"));
            }
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("packDisabled"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("removeUnused"));

            if (Application.isPlaying)
            {
                GUILayout.Button("Pack");
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("previewer"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif