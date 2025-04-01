using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

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
        private const string DEFAULT_SETTINGS_PATH = "Assets/TextureCheckSettings.asset";

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

        [Tooltip("指定要检查的目录（为空则检查所有目录）")]
        public List<string> checkDirectories = new List<string>();

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
        private static Vector2 directoryScrollPosition;

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
                    
                    var settings = TextureCheckSettings.GetOrCreateSettings();
                    var serializedSettings = TextureCheckSettings.GetSerializedSettings();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var currentSettings = AssetDatabase.LoadAssetAtPath<TextureCheckSettings>(EditorPrefs.GetString("TextureCheckSettingsPath", ""));
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

                    EditorGUILayout.Space(20);
                    EditorGUILayout.LabelField("检查目录设置", EditorStyles.boldLabel);
                    
                    SerializedProperty directoriesProp = serializedSettings.FindProperty("checkDirectories");

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("指定要检查的目录（为空则检查所有目录）：");
                    
                    // 添加新目录的按钮
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("添加目录", GUILayout.Width(100)))
                    {
                        string selectedPath = EditorUtility.OpenFolderPanel("选择要检查的目录", "Assets", "");
                        if (!string.IsNullOrEmpty(selectedPath))
                        {
                            // 转换为相对于Assets的路径
                            string relativePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                            if (!settings.checkDirectories.Contains(relativePath))
                            {
                                settings.checkDirectories.Add(relativePath);
                                EditorUtility.SetDirty(settings);
                            }
                        }
                    }
                    
                    if (GUILayout.Button("清空列表", GUILayout.Width(100)))
                    {
                        if (EditorUtility.DisplayDialog("确认", "是否确定清空所有检查目录？", "确定", "取消"))
                        {
                            settings.checkDirectories.Clear();
                            EditorUtility.SetDirty(settings);
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    // 显示目录列表
                    directoryScrollPosition = EditorGUILayout.BeginScrollView(directoryScrollPosition, GUILayout.Height(100));
                    for (int i = settings.checkDirectories.Count - 1; i >= 0; i--)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(settings.checkDirectories[i]);
                        if (GUILayout.Button("移除", GUILayout.Width(60)))
                        {
                            settings.checkDirectories.RemoveAt(i);
                            EditorUtility.SetDirty(settings);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.Space(10);
                    if (GUILayout.Button("检查所有贴图"))
                    {
                        CheckAllTextures();
                    }

                    serializedSettings.ApplyModifiedProperties();
                },
                keywords = new System.Collections.Generic.HashSet<string>(new[] { "Texture", "Check", "Size", "Width", "Height", "Platform", "HMI", "Format", "Compression" })
            };
            return provider;
        }

        private static void CheckAllTextures()
        {
            var settings = TextureCheckSettings.GetOrCreateSettings();
            if (!settings.enableCheck)
            {
                EditorUtility.DisplayDialog("提示", "请先启用贴图检查功能！", "确定");
                return;
            }

            Dictionary<string, string> issues = new Dictionary<string, string>();
            
            // 根据设置的目录范围构建搜索路径
            List<string> searchPaths = new List<string>();
            if (settings.checkDirectories != null && settings.checkDirectories.Count > 0)
            {
                searchPaths.AddRange(settings.checkDirectories);
            }
            else
            {
                searchPaths.Add("Assets"); // 如果没有指定目录，则检查所有Assets下的贴图
            }

            // 在每个指定的目录中搜索贴图
            HashSet<string> processedGuids = new HashSet<string>(); // 用于去重
            foreach (string searchPath in searchPaths)
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { searchPath });
                foreach (string guid in guids)
                {
                    if (processedGuids.Add(guid)) // 如果是新的GUID才处理
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                        
                        if (textureImporter != null)
                        {
                            var (hasIssue, message) = TexturePostprocessor.CheckTextureImporter(textureImporter, assetPath);
                            if (hasIssue)
                            {
                                issues[assetPath] = message;
                                TextureHighlighter.MarkTexture(assetPath, message);
                            }
                            else
                            {
                                TextureHighlighter.RemoveTexture(assetPath);
                            }
                        }
                    }
                }
            }

            if (issues.Count > 0)
            {
                TextureCheckWindow.ShowWindow("问题贴图检查", issues);
            }
            else
            {
                EditorUtility.DisplayDialog("检查完成", "所有贴图都符合要求！", "确定");
            }
        }
    }
} 