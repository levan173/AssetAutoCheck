using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace TAKit.AssetAutoCheck
{
    public class TexturePostprocessor : AssetPostprocessor
    {
        // 用于收集当前导入批次中的问题贴图
        private static Dictionary<string, string> currentBatchIssues = new Dictionary<string, string>();

        // 在所有资源处理完成后调用
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // 处理导入的资产
            if (importedAssets != null && importedAssets.Length > 0)
            {
                foreach (string assetPath in importedAssets)
                {
                    // 检查是否为贴图资源
                    if (AssetImporter.GetAtPath(assetPath) is TextureImporter textureImporter)
                    {
                        var (hasIssue, message) = CheckTextureImporter(textureImporter, assetPath);
                        
                        if (hasIssue)
                        {
                            // 将问题贴图添加到当前批次
                            currentBatchIssues[assetPath] = message;
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
                platformSettings = textureImporter.GetDefaultPlatformTextureSettings();
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

            // 检查导入后的文件大小 - 使用理论计算方法替代运行时内存大小
            float theoryMemorySizeMB = CalculateTheoricalTextureMemorySize(textureImporter, assetPath);
            if (theoryMemorySizeMB > settings.maxFileSize)
            {
                hasIssue = true;
                message +=  "\n" +
                            $"贴图理论内存占用过大: {theoryMemorySizeMB:F2}MB\n" +
                            $"如果没有特殊需求，建议大小不要超过: {settings.maxFileSize}MB\n";
                
                // 检查是否为Crunched格式，添加特别说明
                if (currentFormat == TextureImporterFormat.DXT1Crunched || 
                    currentFormat == TextureImporterFormat.DXT5Crunched)
                {
                    message += "\n注意：当前贴图使用了Crunch压缩格式，Inspector显示的文件大小是磁盘上的压缩大小，" +
                               "而这里计算的是运行时内存占用大小，两者会有显著差异。" +
                               "Crunch压缩只减少磁盘空间和下载时间，不减少运行时内存占用。\n";
                }
            }

            if (hasIssue)
            {
                message +=  "\n" +
                            $"提示：{settings.customMessage}\n";
            }

            return (hasIssue, message);
        }

        /// <summary>
        /// 计算纹理的理论内存大小（类似Inspector面板中显示的大小）
        /// </summary>
        /// <param name="textureImporter">纹理导入器</param>
        /// <param name="assetPath">资源路径</param>
        /// <returns>纹理的理论内存大小（MB）</returns>
        private static float CalculateTheoricalTextureMemorySize(TextureImporter textureImporter, string assetPath)
        {
            // 获取源尺寸
            textureImporter.GetSourceTextureWidthAndHeight(out int width, out int height);
            
            // 获取当前平台设置
            string currentPlatform = EditorUserBuildSettings.activeBuildTarget.ToString();
            bool hasOverride = textureImporter.GetPlatformTextureSettings(currentPlatform).overridden;
            TextureImporterPlatformSettings platformSettings = hasOverride 
                ? textureImporter.GetPlatformTextureSettings(currentPlatform)
                : textureImporter.GetDefaultPlatformTextureSettings();
            
            // 获取最大尺寸设置和实际最大尺寸
            int maxSize = platformSettings.maxTextureSize;
            int maxSourceSize = Mathf.Max(width, height);
            int actualMaxSize = Mathf.Min(maxSourceSize, maxSize);
            
            // 如果宽高比超过1，按比例缩放
            float aspect = (float)width / height;
            int scaledWidth, scaledHeight;
            
            if (width > height)
            {
                scaledWidth = actualMaxSize;
                scaledHeight = Mathf.Max(1, Mathf.RoundToInt(actualMaxSize / aspect));
            }
            else
            {
                scaledHeight = actualMaxSize;
                scaledWidth = Mathf.Max(1, Mathf.RoundToInt(actualMaxSize * aspect));
            }
            
            // 获取格式并计算每像素字节数
            TextureImporterFormat format = platformSettings.format;
            if (format == TextureImporterFormat.Automatic)
            {
                format = textureImporter.GetAutomaticFormat(currentPlatform);
            }
            
            // 获取压缩质量
            int compressionQuality = platformSettings.compressionQuality;
            
            float bytesPerPixel = GetBytesPerPixel(format, compressionQuality);
            
            // 计算基本内存大小
            float memorySizeBytes = scaledWidth * scaledHeight * bytesPerPixel;
            
            // 如果启用了MipMap，增加约33%
            if (textureImporter.mipmapEnabled)
            {
                memorySizeBytes *= 1.33f;
            }
            
            // 转换为MB
            return memorySizeBytes / (1024f * 1024f);
        }

        /// <summary>
        /// 获取指定纹理格式的每像素字节数，考虑压缩质量
        /// </summary>
        /// <remarks>
        /// 此方法提供了对常见纹理格式的内存占用估计。
        /// 注意：不同Unity版本支持的TextureImporterFormat可能有所不同，
        /// 这里无法穷举所有可能的格式，而是采用分类处理的方式。
        /// 如果需要支持更多特定格式，可以扩展此方法。
        /// </remarks>
        private static float GetBytesPerPixel(TextureImporterFormat format, int compressionQuality)
        {
            // 基本字节数
            float baseBpp;
            
            // 无压缩格式 (4-8 bytes per pixel)
            if (format == TextureImporterFormat.RGBA32)
                return 4.0f;
            else if (format == TextureImporterFormat.RGBA16)
                return 2.0f;
            else if (format == TextureImporterFormat.RGB24)
                return 3.0f;
            else if (format == TextureImporterFormat.RGB16)
                return 2.0f;
            else if (format == TextureImporterFormat.Alpha8 || format == TextureImporterFormat.R8)
                return 1.0f;
            else if (format == TextureImporterFormat.R16 || format == TextureImporterFormat.RG16)
                return 2.0f;
            else if (format == TextureImporterFormat.RGHalf)
                return 4.0f;
            else if (format == TextureImporterFormat.RGBAHalf)
                return 8.0f;
            
            // 基于DXT/BC的块压缩格式 (通常0.5-1.0 bytes per pixel)
            else if (format == TextureImporterFormat.DXT1 || 
                     format == TextureImporterFormat.DXT1Crunched ||
                     format == TextureImporterFormat.BC4)
                baseBpp = 0.5f;
            
            else if (format == TextureImporterFormat.DXT5 || 
                     format == TextureImporterFormat.DXT5Crunched ||
                     format == TextureImporterFormat.BC5 ||
                     format == TextureImporterFormat.BC6H ||
                     format == TextureImporterFormat.BC7)
                baseBpp = 1.0f;
            
            // ETC/ETC2 格式 (通常0.5-1.0 bytes per pixel)
            else if (format == TextureImporterFormat.ETC_RGB4 || 
                     format == TextureImporterFormat.ETC_RGB4_3DS)
                baseBpp = 0.5f;
            
            else if (format == TextureImporterFormat.ETC2_RGBA8 || 
                     format == TextureImporterFormat.ETC_RGBA8_3DS)
                baseBpp = 1.0f;
            
            // PVRTC 格式 (根据bits per pixel定义，2bpp或4bpp)
            else if (format == TextureImporterFormat.PVRTC_RGB2 ||
                     format == TextureImporterFormat.PVRTC_RGBA2)
                baseBpp = 0.25f; // 2 bits per pixel = 0.25 bytes per pixel
            
            else if (format == TextureImporterFormat.PVRTC_RGB4 ||
                     format == TextureImporterFormat.PVRTC_RGBA4)
                baseBpp = 0.5f; // 4 bits per pixel = 0.5 bytes per pixel
            
            // ASTC 格式 (根据块大小不同，压缩率从0.8到0.09 bytes per pixel不等)
            else if (format == TextureImporterFormat.ASTC_4x4)
                baseBpp = 1.0f; // 8 bits per pixel = 1 byte per pixel
            
            else if (format == TextureImporterFormat.ASTC_5x5)
                baseBpp = 0.64f; // ~6.4 bits per pixel
            
            else if (format == TextureImporterFormat.ASTC_6x6)
                baseBpp = 0.44f; // ~4.4 bits per pixel
            
            else if (format == TextureImporterFormat.ASTC_8x8)
                baseBpp = 0.25f; // ~2.5 bits per pixel
                
            else if (format == TextureImporterFormat.ASTC_10x10)
                baseBpp = 0.16f; // ~1.6 bits per pixel
                
            else if (format == TextureImporterFormat.ASTC_12x12)
                baseBpp = 0.11f; // ~1.1 bits per pixel
            
            // 自动格式 - 使用较保守的估计，因为无法确定最终会使用哪种格式
            else if (format == TextureImporterFormat.Automatic)
                baseBpp = 1.0f;
            
            // 其他所有格式 - 使用较保守的估计值
            else
                baseBpp = 1.0f;
            
            // 根据压缩质量调整
            // 压缩质量范围为0-100，默认为50
            // 质量越高，内存占用可能越大（对于某些格式）
            if (compressionQuality != 0)
            {
                // 只对特定格式进行调整，如BC7和高级压缩格式
                if (format == TextureImporterFormat.BC7 || 
                    format == TextureImporterFormat.ASTC_4x4 ||
                    format == TextureImporterFormat.ASTC_5x5)
                {
                    // 对于高质量压缩，内存占用可能稍高
                    float qualityFactor = 1.0f + (compressionQuality - 50) / 200.0f; // 范围约为0.75到1.25
                    baseBpp *= qualityFactor;
                }
            }
            
            return baseBpp;
        }
    }
} 