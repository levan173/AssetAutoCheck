using UnityEngine;
using UnityEditor;

namespace AssetAutoCheck
{
    [System.Serializable]
    public struct PlatformTextureFormat
    {
        public TextureImporterFormat androidFormat;
        public TextureImporterFormat iosFormat;
        public TextureImporterFormat windowsFormat;
        public TextureImporterFormat webGLFormat;
        public TextureImporterFormat hmiAndroidFormat;

        public static PlatformTextureFormat CreateDefault()
        {
            return new PlatformTextureFormat
            {
                androidFormat = TextureImporterFormat.ASTC_6x6,
                iosFormat = TextureImporterFormat.ASTC_6x6,
                windowsFormat = TextureImporterFormat.DXT5,
                webGLFormat = TextureImporterFormat.DXT5,
                hmiAndroidFormat = TextureImporterFormat.ETC2_RGBA8
            };
        }

        public TextureImporterFormat GetCurrentPlatformFormat()
        {
            #if UNITY_ANDROID
                // 检查是否是HMI Android平台
                if (UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup == UnityEditor.BuildTargetGroup.Android &&
                    UnityEditor.EditorPrefs.GetString("BuildTarget", "") == "HMI")
                {
                    return hmiAndroidFormat;
                }
                return androidFormat;
            #elif UNITY_IOS
                return iosFormat;
            #elif UNITY_WEBGL
                return webGLFormat;
            #else
                return windowsFormat;
            #endif
        }
    }

    [System.Serializable]
    public struct PlatformTextureSize
    {
        public int androidMaxSize;
        public int iosMaxSize;
        public int windowsMaxSize;
        public int webGLMaxSize;
        public int hmiAndroidMaxSize;

        public static PlatformTextureSize CreateDefault()
        {
            return new PlatformTextureSize
            {
                androidMaxSize = 1024,
                iosMaxSize = 1024,
                windowsMaxSize = 2048,
                webGLMaxSize = 1024,
                hmiAndroidMaxSize = 1024  // HMI Android默认也设置为1024
            };
        }

        public int GetCurrentPlatformSize()
        {
            #if UNITY_ANDROID
                // 检查是否是HMI Android平台
                if (UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup == UnityEditor.BuildTargetGroup.Android &&
                    UnityEditor.EditorPrefs.GetString("BuildTarget", "") == "HMI")
                {
                    return hmiAndroidMaxSize;
                }
                return androidMaxSize;
            #elif UNITY_IOS
                return iosMaxSize;
            #elif UNITY_WEBGL
                return webGLMaxSize;
            #else
                return windowsMaxSize;
            #endif
        }
    }

    public class TextureCheckSettings : ScriptableObject
    {
        private const string DEFAULT_SETTINGS_PATH = "Packages/com.levan.assetautocheck/TextureCheckSettings.asset";

        [Tooltip("是否启用贴图检查")]
        public bool enableCheck = true;

        [Tooltip("各平台贴图最大尺寸")]
        public PlatformTextureSize maxTextureSize = PlatformTextureSize.CreateDefault();

        [Tooltip("各平台贴图压缩格式")]
        public PlatformTextureFormat textureFormat = PlatformTextureFormat.CreateDefault();
        
        [Tooltip("贴图最大文件大小（MB）")]
        public float maxFileSize = 20f;

        [Tooltip("自定义提示信息")]
        [TextArea(3, 10)]
        public string customMessage = "请注意遵循项目的贴图规范。";

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
                    
                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField("平台特定的贴图尺寸限制", EditorStyles.boldLabel);
                    
                    SerializedProperty maxSizeProp = serializedSettings.FindProperty("maxTextureSize");
                    EditorGUILayout.PropertyField(maxSizeProp.FindPropertyRelative("androidMaxSize"), new GUIContent("Android最大尺寸"));
                    EditorGUILayout.PropertyField(maxSizeProp.FindPropertyRelative("hmiAndroidMaxSize"), new GUIContent("HMI Android最大尺寸"));
                    EditorGUILayout.PropertyField(maxSizeProp.FindPropertyRelative("iosMaxSize"), new GUIContent("iOS最大尺寸"));
                    EditorGUILayout.PropertyField(maxSizeProp.FindPropertyRelative("windowsMaxSize"), new GUIContent("Windows最大尺寸"));
                    EditorGUILayout.PropertyField(maxSizeProp.FindPropertyRelative("webGLMaxSize"), new GUIContent("WebGL最大尺寸"));

                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField("平台特定的贴图压缩格式", EditorStyles.boldLabel);
                    
                    SerializedProperty formatProp = serializedSettings.FindProperty("textureFormat");
                    EditorGUILayout.PropertyField(formatProp.FindPropertyRelative("androidFormat"), new GUIContent("Android压缩格式"));
                    EditorGUILayout.PropertyField(formatProp.FindPropertyRelative("hmiAndroidFormat"), new GUIContent("HMI Android压缩格式"));
                    EditorGUILayout.PropertyField(formatProp.FindPropertyRelative("iosFormat"), new GUIContent("iOS压缩格式"));
                    EditorGUILayout.PropertyField(formatProp.FindPropertyRelative("windowsFormat"), new GUIContent("Windows压缩格式"));
                    EditorGUILayout.PropertyField(formatProp.FindPropertyRelative("webGLFormat"), new GUIContent("WebGL压缩格式"));
                    
                    EditorGUILayout.Space(10);
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty("maxFileSize"), new GUIContent("最大文件大小(MB)"));
                    EditorGUILayout.Space(10);
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty("customMessage"), new GUIContent("自定义提示信息"));
                    serializedSettings.ApplyModifiedProperties();
                },
                keywords = new System.Collections.Generic.HashSet<string>(new[] { "Texture", "Check", "Size", "Width", "Height", "Platform", "HMI", "Format", "Compression" })
            };
            return provider;
        }
    }
} 