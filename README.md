# Asset Auto Check

Unity编辑器工具，用于自动检查贴图资源的尺寸，压缩格式和文件大小等，并提供可视化警告。

## 功能

- 自动检查导入的贴图尺寸，压缩格式和文件大小等
- 在Project窗口中高亮显示不符合要求的贴图
- 可配置的检查规则（最大尺寸和文件大小限制等）
- 支持多贴图同时导入时的批量检查
- 点击警告信息可快速定位到问题贴图
- 可通过设置开关启用/禁用检查功能
- 一键全局检查贴图功能

## 安装

### 通过 Unity Package Manager

1. 打开 Package Manager (Window > Package Manager)
2. 点击 "+" 按钮
3. 选择 "Add package from git URL..."
4. 输入: 仓库中的HTTPS或者SSH链接

### 手动安装

将本包复制到你的项目的 `Packages` 文件夹中。

## 使用方法
### 配置项目贴图规范

使用流程的第一步是配置当前项目的贴图规范，具体操作如下

1. 在 Project Settings 中找到 "贴图检查设置"

   ![贴图检查设置项](https://github.com/user-attachments/assets/ca604fe6-8dbf-45fb-954d-c921c5cab0ff)

2. 可以配置的贴图检查选项如下：
   - 不同平台下的最大分辨率限制，实际检查的为图片打包后的分辨率，即图片源分辨率和TextureImporter面板中设置的maxSize中更小的那个
   - 不同平台下的压缩格式限制，此处可以多选，当TextureImporter中选择Automatic时，也会检查当前平台automatic对应的格式是否满足限制

      ![压缩格式限制](https://github.com/user-attachments/assets/73a96786-c5ea-4dde-bbaa-a50e823ae151)

   - 文件最大大小限制，单位为MB
   - 检查时自动排除的目录，可以直接指定目录，或者是通过关键字模式来排除路径中带有该关键字的目录。关键字模式下为全匹配排除，以下图为例，排除关键字为Editor时，会排除Assets/SomeFolder/Editor/下的贴图，但是不会排除Assets/SomeFolder/SomeEditor/下的贴图。

      ![排除目录设置](https://github.com/user-attachments/assets/a854608f-ea75-477e-ba43-135fbe87b662)

### 手动全局检查
在配置好项目的贴图规范后，即可通过Project面板中的检查所有贴图按钮调用检查贴图功能了，具体操作流程如下
1. 指定检查的目录范围，如果不指定的话则默认检查Assets下所有贴图(会应用排除目录设定)
   ![检查目录设置](https://github.com/user-attachments/assets/a8c59be5-9d1e-4209-be18-c196a39d5da0)
2. 点击检查所有贴图按钮
3. 如果检查到了存在问题的贴图则会弹出一个窗口显示所有问题贴图信息，其中每项对应一张存在问题的贴图，点击左侧按钮可在project窗口中定位对应贴图，右侧是他的问题信息和提示  
   ![问题贴图窗口](https://github.com/user-attachments/assets/5610fb0a-ad2f-4ac6-a75e-5b2c1f664991)  
   同时不符合规范的贴图会在project面板中标红，显示警告图标  
   ![project中的贴图警告](https://github.com/user-attachments/assets/5fb49c58-4470-4c83-968c-6145e4650ecb)
4. 查看对应贴图inspector面板上的违规提示，根据提示修改贴图，修改完成应用会自动检查是否满足规范，满足则会自动移除标记  
   ![inspector中的提示](https://github.com/user-attachments/assets/99204495-5c9c-47c3-be83-5b1ca13188e6)
5. 如果贴图有特殊需求，确实无法满足项目统一规范的话，也可以手动清除标记，只需在project面板中右键对应贴图，然后选择清除贴图检查标记即可(注意别和清除所有贴图检查标记弄混)  
   ![清除贴图标记选项](https://github.com/user-attachments/assets/ed637acb-53b5-4253-bcab-47f40ed0a495)

### 导入时自动检查
配置好项目的贴图规范后，往项目中导入新贴图也会自动触发贴图检查，修改流程和手动检查时的一样。

## 已知问题
目前可能存在一个如下的编辑器报错
'AssetAutoCheck.TextureInspectorExtension' is missing the class attribute 'ExtensionOfNativeClass'!
这并不影响正常使用，直接无视即可

## 许可

[MIT License] 