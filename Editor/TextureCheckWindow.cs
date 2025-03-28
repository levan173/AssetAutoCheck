using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AssetAutoCheck
{
    public class TextureCheckWindow : EditorWindow
    {
        private Dictionary<string, string> issues = new Dictionary<string, string>();
        private Vector2 scrollPosition;

        public static void ShowWindow(string title, Dictionary<string, string> textureIssues)
        {
            var window = GetWindow<TextureCheckWindow>("贴图检查警告");
            window.titleContent = new GUIContent(title);
            window.issues = new Dictionary<string, string>(textureIssues);
            window.minSize = new Vector2(500, 300);
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"警告：检测到 {issues.Count} 个问题贴图", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            var style = new GUIStyle(EditorStyles.helpBox);
            style.richText = true;
            
            foreach (var issue in issues)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                if (GUILayout.Button(issue.Value, style))
                {
                    // 定位到贴图
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(issue.Key);
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(10);
            if (GUILayout.Button("关闭"))
            {
                Close();
            }
        }
    }
} 