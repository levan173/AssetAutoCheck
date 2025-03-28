using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AssetAutoCheck
{
    [InitializeOnLoad]
    public class TextureHighlighter
    {
        private static readonly string ProblemTexturesKey = "AssetAutoCheck_ProblemTextures";
        private static HashSet<string> problemTextures = new HashSet<string>();

        static TextureHighlighter()
        {
            LoadProblemTextures();
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
        }

        public static void MarkTexture(string path)
        {
            problemTextures.Add(path);
            SaveProblemTextures();
        }

        public static void RemoveTexture(string path)
        {
            if (problemTextures.Remove(path))
            {
                SaveProblemTextures();
            }
        }

        public static void UpdateTexturePath(string oldPath, string newPath)
        {
            if (problemTextures.Remove(oldPath))
            {
                problemTextures.Add(newPath);
                SaveProblemTextures();
            }
        }

        private static void LoadProblemTextures()
        {
            problemTextures.Clear();
            string data = EditorPrefs.GetString(ProblemTexturesKey, "");
            if (!string.IsNullOrEmpty(data))
            {
                string[] paths = data.Split(new char[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (var path in paths)
                {
                    problemTextures.Add(path);
                }
            }
        }

        private static void SaveProblemTextures()
        {
            string data = string.Join("|", problemTextures);
            EditorPrefs.SetString(ProblemTexturesKey, data);
        }

        private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            // 如果检查被禁用，不显示标记
            var settings = TextureCheckSettings.GetOrCreateSettings();
            if (!settings.enableCheck)
            {
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (problemTextures.Contains(path))
            {
                Color originalColor = GUI.color;
                GUI.color = new Color(1f, 0.5f, 0.5f, 0.3f);
                GUI.DrawTexture(selectionRect, EditorGUIUtility.whiteTexture);
                GUI.color = originalColor;

                // 在图标旁边绘制警告标志
                Rect iconRect = new Rect(selectionRect.x - 4, selectionRect.y, 16, 16);
                GUI.Label(iconRect, EditorGUIUtility.IconContent("console.warnicon"));
            }
        }

        // 添加菜单项来清除高亮
        [MenuItem("Assets/清除贴图检查标记")]
        private static void ClearHighlight()
        {
            Object[] selection = Selection.objects;
            bool changed = false;

            foreach (Object obj in selection)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (problemTextures.Remove(path))
                {
                    changed = true;
                }
            }

            if (changed)
            {
                SaveProblemTextures();
                EditorApplication.RepaintProjectWindow();
            }
        }

        [MenuItem("Assets/清除贴图检查标记", true)]
        private static bool ValidateClearHighlight()
        {
            return Selection.objects.Length > 0;
        }
    }
} 