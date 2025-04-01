using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AssetAutoCheck
{
    [InitializeOnLoad]
    public class TextureHighlighter
    {
        private static readonly string ProblemTexturesKey = "AssetAutoCheck_ProblemTextures";
        private static readonly string ProblemMessagesKey = "AssetAutoCheck_ProblemMessages";
        private static HashSet<string> problemTextures = new HashSet<string>();
        private static Dictionary<string, string> problemMessages = new Dictionary<string, string>();

        static TextureHighlighter()
        {
            LoadProblemTextures();
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
        }

        public static void MarkTexture(string path, string message)
        {
            problemTextures.Add(path);
            problemMessages[path] = message;
            SaveProblemTextures();
        }

        public static void RemoveTexture(string path)
        {
            if (problemTextures.Remove(path))
            {
                problemMessages.Remove(path);
                SaveProblemTextures();
            }
        }

        public static void UpdateTexturePath(string oldPath, string newPath)
        {
            if (problemTextures.Remove(oldPath))
            {
                problemTextures.Add(newPath);
                if (problemMessages.TryGetValue(oldPath, out string message))
                {
                    problemMessages.Remove(oldPath);
                    problemMessages[newPath] = message;
                }
                SaveProblemTextures();
            }
        }

        public static bool IsProblemTexture(string path)
        {
            return problemTextures.Contains(path);
        }

        public static string GetProblemMessage(string path)
        {
            return problemMessages.TryGetValue(path, out string message) ? message : string.Empty;
        }

        private static void LoadProblemTextures()
        {
            problemTextures.Clear();
            problemMessages.Clear();

            string pathsData = EditorPrefs.GetString(ProblemTexturesKey, "");
            string messagesData = EditorPrefs.GetString(ProblemMessagesKey, "");

            if (!string.IsNullOrEmpty(pathsData))
            {
                string[] paths = pathsData.Split(new char[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries);
                string[] messages = messagesData.Split(new char[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < paths.Length && i < messages.Length; i++)
                {
                    problemTextures.Add(paths[i]);
                    problemMessages[paths[i]] = messages[i].Replace("\\n", "\n");
                }
            }
        }

        private static void SaveProblemTextures()
        {
            string pathsData = string.Join("|", problemTextures);
            List<string> messages = new List<string>();
            foreach (var path in problemTextures)
            {
                if (problemMessages.TryGetValue(path, out string message))
                {
                    messages.Add(message.Replace("\n", "\\n"));
                }
                else
                {
                    messages.Add("");
                }
            }
            string messagesData = string.Join("|", messages);

            EditorPrefs.SetString(ProblemTexturesKey, pathsData);
            EditorPrefs.SetString(ProblemMessagesKey, messagesData);
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
                    problemMessages.Remove(path);
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