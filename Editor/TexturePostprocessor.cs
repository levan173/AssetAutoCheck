using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace AssetAutoCheck
{
    public class TexturePostprocessor : AssetPostprocessor
    {
        // 用于收集当前导入批次中的问题贴图
        private static Dictionary<string, string> currentBatchIssues = new Dictionary<string, string>();

        void OnPostprocessTexture(Texture2D texture)
        {
            if (assetImporter is TextureImporter textureImporter)
            {
                var settings = TextureCheckSettings.GetOrCreateSettings();
                
                // 获取源尺寸
                int width = 0, height = 0;
                textureImporter.GetSourceTextureWidthAndHeight(out width, out height);
                int maxSourceSize = Mathf.Max(width, height);

                // 获取当前平台的最大尺寸设置
                var platformSettings = textureImporter.GetPlatformTextureSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
                int maxSize = platformSettings.maxTextureSize;
                
                // 计算实际最大尺寸（源尺寸和设置尺寸的最小值）
                int actualMaxSize = Mathf.Min(maxSourceSize, maxSize);
                
                if (actualMaxSize > 0)
                {
                    bool hasIssue = false;
                    string message = $"贴图名称: {Path.GetFileName(assetPath)}\n";

                    // 获取当前平台的最大尺寸限制
                    int platformMaxSize = settings.maxTextureSize.GetCurrentPlatformSize();

                    // 检查最大尺寸
                    if (actualMaxSize > platformMaxSize)
                    {
                        hasIssue = true;
                        message += $"贴图实际最大尺寸过大: {actualMaxSize}\n" +
                                 $"源尺寸: {width}x{height}, 最大压缩尺寸设置: {maxSize}\n" +
                                 $"当前目标平台: {EditorUserBuildSettings.activeBuildTarget}, 平台最大尺寸限制: {platformMaxSize}\n";
                    }

                    // 检查压缩格式
                    TextureImporterFormat expectedFormat = settings.textureFormat.GetCurrentPlatformFormat();
                    TextureImporterFormat currentFormat = platformSettings.format;
                    
                    if (currentFormat != expectedFormat)
                    {
                        hasIssue = true;
                        message += $"贴图压缩格式不符合要求\n" +
                                 $"当前格式: {currentFormat}\n" +
                                 $"当前目标平台: {EditorUserBuildSettings.activeBuildTarget},建议格式: {expectedFormat}\n";
                    }

                    // 检查文件大小
                    string fullPath = Path.GetFullPath(assetPath);
                    if (File.Exists(fullPath))
                    {
                        float fileSizeMB = new FileInfo(fullPath).Length / (1024f * 1024f);
                        if (fileSizeMB > settings.maxFileSize)
                        {
                            hasIssue = true;
                            message += $"文件大小过大: {fileSizeMB:F2}MB\n" +
                                     $"如果没有特殊需求，建议大小不要超过: {settings.maxFileSize}MB\n";
                        }
                    }

                    if (hasIssue)
                    {
                        message += $"提示：{settings.customMessage}\n";
                        
                        // 将问题贴图添加到当前批次
                        lock (currentBatchIssues)
                        {
                            currentBatchIssues[assetPath] = message;
                        }
                        TextureHighlighter.MarkTexture(assetPath, message);
                    }
                    else
                    {
                        // 如果贴图现在满足要求，移除高亮标记
                        TextureHighlighter.RemoveTexture(assetPath);
                    }
                }
            }
        }

        // 在所有资源处理完成后调用
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // 处理删除的资产
            if (deletedAssets != null && deletedAssets.Length > 0)
            {
                foreach (string deletedAsset in deletedAssets)
                {
                    TextureHighlighter.RemoveTexture(deletedAsset);
                }
            }

            // 处理移动的资产
            if (movedAssets != null && movedAssets.Length > 0)
            {
                for (int i = 0; i < movedAssets.Length; i++)
                {
                    TextureHighlighter.UpdateTexturePath(movedFromAssetPaths[i], movedAssets[i]);
                }
            }

            if (currentBatchIssues.Count > 0)
            {
                var settings = TextureCheckSettings.GetOrCreateSettings();
                // 只在启用检查时显示窗口
                if (settings.enableCheck)
                {
                    TextureCheckWindow.ShowWindow("问题贴图检查", currentBatchIssues);
                }
                currentBatchIssues.Clear();
            }
        }
    }
} 