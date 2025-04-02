using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace AssetAutoCheck
{
    public static class TextureImporterExtensions
    {
        public static TextureImporterFormat GetDefaultPlatformTextureFormat(this TextureImporter importer)
        {
            switch (importer.textureCompression)
            {
                case TextureImporterCompression.Compressed:
                    return TextureImporterFormat.Automatic;
                case TextureImporterCompression.CompressedHQ:
                    return TextureImporterFormat.Automatic;
                case TextureImporterCompression.CompressedLQ:
                    return TextureImporterFormat.Automatic;
                default:
                    return TextureImporterFormat.RGBA32;
            }
        }
    }

    public class TexturePostprocessor : AssetPostprocessor
    {
        // 用于收集当前导入批次中的问题贴图
        private static Dictionary<string, string> currentBatchIssues = new Dictionary<string, string>();

        void OnPostprocessTexture(Texture2D texture)
        {
            if (assetImporter is TextureImporter textureImporter)
            {
                var (hasIssue, message) = CheckTextureImporter(textureImporter, assetPath);
                
                if (hasIssue)
                {
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

        /// <summary>
        /// 检查贴图导入器的设置是否符合要求
        /// </summary>
        /// <param name="textureImporter">贴图导入器</param>
        /// <param name="assetPath">资源路径</param>
        /// <returns>返回一个元组，包含是否有问题和问题描述信息</returns>
        public static (bool hasIssue, string message) CheckTextureImporter(TextureImporter textureImporter, string assetPath)
        {
            var settings = TextureCheckSettings.GetOrCreateSettings();
            if (!settings.enableCheck) return (false, string.Empty);

            // 检查是否在排除列表中
            if (settings.ShouldExclude(assetPath))
            {
                return (false, string.Empty);
            }

            // 获取源尺寸
            int width = 0, height = 0;
            textureImporter.GetSourceTextureWidthAndHeight(out width, out height);
            int maxSourceSize = Mathf.Max(width, height);

            string currentPlatform = EditorUserBuildSettings.activeBuildTarget.ToString();
            
            // 检查是否有平台特定的覆盖设置
            bool hasOverride = textureImporter.GetPlatformTextureSettings(currentPlatform).overridden;
            TextureImporterPlatformSettings platformSettings;
            
            if (hasOverride)
            {
                // 使用平台特定的设置
                platformSettings = textureImporter.GetPlatformTextureSettings(currentPlatform);
            }
            else
            {
                // 使用默认设置
                platformSettings = new TextureImporterPlatformSettings
                {
                    maxTextureSize = textureImporter.maxTextureSize,
                    format = textureImporter.textureCompression == TextureImporterCompression.Uncompressed 
                        ? TextureImporterFormat.RGBA32 
                        : textureImporter.GetDefaultPlatformTextureFormat()
                };
            }

            int maxSize = platformSettings.maxTextureSize;
            
            // 计算实际最大尺寸（源尺寸和设置尺寸的最小值）
            int actualMaxSize = Mathf.Min(maxSourceSize, maxSize);
            
            if (actualMaxSize <= 0) return (false, string.Empty);

            bool hasIssue = false;
            string message = $"贴图名称: {Path.GetFileName(assetPath)}\n";
            message += $"项目当前目标平台: {currentPlatform}\n";
            message += hasOverride ? $"[贴图使用平台特定设置 - {currentPlatform}]\n" : "[贴图使用默认设置]\n";

            // 获取当前平台的最大尺寸限制
            int platformMaxSize = settings.maxTextureSize.GetCurrentPlatformSize();

            // 检查最大尺寸
            if (actualMaxSize > platformMaxSize)
            {
                hasIssue = true;
                message +=  "\n" +
                            $"贴图实际最大尺寸过大: {actualMaxSize}\n" +
                            $"源尺寸: {width}x{height}, 贴图最大压缩尺寸设置: {maxSize}\n" +
                            $"当前目标平台最大尺寸限制: {platformMaxSize}\n";
            }

            // 检查压缩格式
            var expectedFormats = settings.textureFormat.GetCurrentPlatformFormats();
            TextureImporterFormat currentFormat = platformSettings.format;
            if(currentFormat == TextureImporterFormat.Automatic)
            {
                currentFormat = textureImporter.GetAutomaticFormat(currentPlatform);
                message +=  "\n" +
                            "贴图选择了自动格式，当前平台自动格式为：" + currentFormat + "\n";
            }
            
            if (!expectedFormats.Contains(currentFormat))
            {
                hasIssue = true;
                message +=  "\n" +
                            $"贴图压缩格式不符合要求\n" +
                            $"贴图当前格式: {currentFormat}\n" +
                            $"当前目标平台建议格式: {string.Join(" 或 ", expectedFormats)}\n";
            }

            // 检查文件大小
            string fullPath = Path.GetFullPath(assetPath);
            if (File.Exists(fullPath))
            {
                float fileSizeMB = new FileInfo(fullPath).Length / (1024f * 1024f);
                if (fileSizeMB > settings.maxFileSize)
                {
                    hasIssue = true;
                    message +=  "\n" +
                                $"文件大小过大: {fileSizeMB:F2}MB\n" +
                                $"如果没有特殊需求，建议大小不要超过: {settings.maxFileSize}MB\n";
                }
            }

            if (hasIssue)
            {
                message +=  "\n" +
                            $"提示：{settings.customMessage}\n";
            }

            return (hasIssue, message);
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