using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AssetAutoCheck
{
    public class TextureCheckWindow : EditorWindow
    {
        private Dictionary<string, string> issues = new Dictionary<string, string>();
        private Vector2 scrollPosition;

        private int itemHeightUnit = 4;
        // 为每个问题项存储独立的滚动位置
        private Dictionary<string, Vector2> itemScrollPositions = new Dictionary<string, Vector2>();

        public static void ShowWindow(string title, Dictionary<string, string> textureIssues)
        {
            var window = GetWindow<TextureCheckWindow>("贴图检查警告");
            window.titleContent = new GUIContent(title);
            window.issues = new Dictionary<string, string>(textureIssues);
            // 初始化每个问题项的滚动位置
            window.itemScrollPositions = new Dictionary<string, Vector2>();
            foreach (var issue in textureIssues)
            {
                window.itemScrollPositions[issue.Key] = Vector2.zero;
            }
            window.minSize = new Vector2(500, 300);
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"警告：检测到 {issues.Count} 个问题贴图", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            var helpBoxStyle = new GUIStyle(EditorStyles.helpBox);
            helpBoxStyle.richText = true;
            
            foreach (var issue in issues)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                    EditorGUILayout.BeginHorizontal();

                    // 左侧按钮
                    if (GUILayout.Button("在Project中定位", GUILayout.Width(100), GUILayout.Height(EditorGUIUtility.singleLineHeight * itemHeightUnit)))
                    {
                        // 定位到贴图
                        var obj = AssetDatabase.LoadAssetAtPath<Object>(issue.Key);
                        Selection.activeObject = obj;
                        EditorGUIUtility.PingObject(obj);
                    }

                    GUILayout.Space(5); // 添加一些间距
                    
                    // 创建一个固定高度的ScrollView来容纳SelectableLabel
                    float height = EditorGUIUtility.singleLineHeight * itemHeightUnit;
                        EditorGUILayout.BeginVertical(GUILayout.Height(height), GUILayout.ExpandWidth(true));
                        
                        // 使用存储的滚动位置
                        itemScrollPositions[issue.Key] = EditorGUILayout.BeginScrollView(
                            itemScrollPositions[issue.Key], 
                            GUILayout.Height(height)
                        );
                        
                        var style = new GUIStyle(EditorStyles.textArea);
                        style.wordWrap = true; // 启用自动换行
                        // 计算文本实际需要的高度
                        float textHeight = style.CalcHeight(new GUIContent(issue.Value), EditorGUIUtility.currentViewWidth - 150);
                        var rect = EditorGUILayout.GetControlRect(GUILayout.Height(textHeight));
                        EditorGUI.SelectableLabel(rect, issue.Value, style);
                        
                        EditorGUILayout.EndScrollView();
                        EditorGUILayout.EndVertical();
                    
                    EditorGUILayout.EndHorizontal();
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