using UnityEditor;
using UnityEngine;

namespace TAKit.AssetAutoCheck
{
    public class TextureCheckSettingsTracker : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // 处理资产移动
            for (int i = 0; i < movedAssets.Length; i++)
            {
                var movedAsset = AssetDatabase.LoadAssetAtPath<TextureCheckSettings>(movedAssets[i]);
                if (movedAsset != null)
                {
                    // 检查这个移动的资源是否是当前正在使用的设置
                    var currentSettings = TextureCheckSettings.Instance;
                    if (currentSettings == movedAsset)
                    {
                        // 更新路径
                        EditorPrefs.SetString(TextureCheckSettings.SETTINGS_PATH_PREF_KEY, movedAssets[i]);
                    }
                }
            }
        }
    }
} 