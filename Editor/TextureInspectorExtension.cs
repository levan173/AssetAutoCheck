using UnityEngine;
using UnityEditor;

namespace TAKit.AssetAutoCheck
{
    [InitializeOnLoad]
    public class TextureInspectorExtension
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            Editor.finishedDefaultHeaderGUI += OnPostHeaderGUI;
        }

        private static void OnPostHeaderGUI(Editor editor)
        {
            var settings = TextureCheckSettings.GetOrCreateSettings();
            if (!settings.enableCheck) return;

            if (editor.target is Texture2D || editor.target is TextureImporter)
            {
                string assetPath = AssetDatabase.GetAssetPath(editor.target);
                if (!string.IsNullOrEmpty(assetPath) && TextureHighlighter.IsProblemTexture(assetPath))
                {
                    EditorGUILayout.Space(10);
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.HelpBox(TextureHighlighter.GetProblemMessage(assetPath), MessageType.Warning);
                    }
                }
            }
        }
    }
}
