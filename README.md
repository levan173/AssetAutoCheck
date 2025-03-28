# Asset Auto Check

Unity编辑器工具，用于自动检查贴图资源的尺寸和文件大小，并提供可视化警告。

## 功能

- 自动检查导入的贴图尺寸和文件大小
- 在Project窗口中高亮显示不符合要求的贴图
- 可配置的检查规则（最大尺寸和文件大小限制）
- 支持多贴图同时导入时的批量检查
- 点击警告信息可快速定位到问题贴图
- 可通过设置开关启用/禁用检查功能

## 安装

### 通过 Unity Package Manager

1. 打开 Package Manager (Window > Package Manager)
2. 点击 "+" 按钮
3. 选择 "Add package from git URL..."
4. 输入: `https://github.com/levan173/AssetAutoCheck.git`

### 手动安装

将本包复制到你的项目的 `Packages` 文件夹中。

## 使用方法

1. 在 Project Settings 中找到 "Texture Check Settings"
2. 配置贴图检查的规则：
   - 最大尺寸限制
   - 最大文件大小限制
   - 启用/禁用检查功能
3. 导入贴图时会自动进行检查
4. 不符合要求的贴图会在Project窗口中被高亮显示
5. 点击警告窗口中的贴图信息可快速定位到对应贴图

## 许可

[MIT License] 