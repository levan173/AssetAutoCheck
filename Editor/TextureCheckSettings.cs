using UnityEngine;
using UnityEditor;

namespace AssetAutoCheck
{
    public class TextureCheckSettings : ScriptableObject
    {
        private const string DEFAULT_SETTINGS_PATH = "Packages/com.levan.assetautocheck/Editor/TextureCheckSettings.asset";

        [Tooltip("是否启用贴图检查")]
        public bool enableCheck = true;

        [Tooltip("贴图最大尺寸")]
        public int maxTextureWidth = 2048;
        
        [Tooltip("贴图最大文件大小（MB）")]
        public float maxFileSize = 20f;

        public static TextureCheckSettings GetOrCreateSettings()
        {
            var settings = GetSettings();
            if (settings == null)
            {
                settings = CreateInstance<TextureCheckSettings>();
                if (!AssetDatabase.IsValidFolder("Assets/AssetAutoCheck"))
                {
                    AssetDatabase.CreateFolder("Assets", "AssetAutoCheck");
                }
                AssetDatabase.CreateAsset(settings, DEFAULT_SETTINGS_PATH);
                AssetDatabase.SaveAssets();
                EditorPrefs.SetString("TextureCheckSettingsPath", DEFAULT_SETTINGS_PATH);
            }
            return settings;
        }

        private static TextureCheckSettings GetSettings()
        {
            string path = EditorPrefs.GetString("TextureCheckSettingsPath", DEFAULT_SETTINGS_PATH);
            return AssetDatabase.LoadAssetAtPath<TextureCheckSettings>(path);
        }

        public static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
    }

    static class TextureCheckSettingsIMGUIRegister
    {
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Project/Texture Check Settings", SettingsScope.Project)
            {
                label = "贴图检查设置",
                guiHandler = (searchContext) =>
                {
                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField("设置文件", EditorStyles.boldLabel);
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var settings = TextureCheckSettings.GetOrCreateSettings();
                        var path = EditorPrefs.GetString("TextureCheckSettingsPath", "");
                        var currentSettings = AssetDatabase.LoadAssetAtPath<TextureCheckSettings>(path);
                        var newSettings = EditorGUILayout.ObjectField("当前设置文件", currentSettings, typeof(TextureCheckSettings), false) as TextureCheckSettings;
                        
                        if (newSettings != currentSettings && newSettings != null)
                        {
                            string newPath = AssetDatabase.GetAssetPath(newSettings);
                            EditorPrefs.SetString("TextureCheckSettingsPath", newPath);
                        }
                        
                        if (GUILayout.Button("创建新设置", GUILayout.Width(100)))
                        {
                            string newPath = EditorUtility.SaveFilePanel("创建新设置文件", "Assets", "TextureCheckSettings", "asset");
                            if (!string.IsNullOrEmpty(newPath))
                            {
                                newPath = "Assets" + newPath.Substring(Application.dataPath.Length);
                                var newSettingsObj = ScriptableObject.CreateInstance<TextureCheckSettings>();
                                AssetDatabase.CreateAsset(newSettingsObj, newPath);
                                AssetDatabase.SaveAssets();
                                EditorPrefs.SetString("TextureCheckSettingsPath", newPath);
                            }
                        }
                    }

                    EditorGUILayout.Space(20);
                    var serializedSettings = TextureCheckSettings.GetSerializedSettings();
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty("enableCheck"), new GUIContent("启用贴图检查"));
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty("maxTextureWidth"), new GUIContent("最大尺寸"));
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty("maxFileSize"), new GUIContent("最大文件大小(MB)"));
                    serializedSettings.ApplyModifiedProperties();
                },
                keywords = new System.Collections.Generic.HashSet<string>(new[] { "Texture", "Check", "Size", "Width", "Height" })
            };
            return provider;
        }
    }
} 