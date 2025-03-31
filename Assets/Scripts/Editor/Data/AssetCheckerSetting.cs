using System;
using UnityEditor;
using System.Collections.Generic;

[Serializable] // 让类可序列化
public class AssetCheckerSetting
{
    public string targetFolderPath = "Assets/Texture"; // 指定目录
    public E_targetType targetType = E_targetType.Texture; // 检索的文件格式
    public List<ImagesFormatPair> textureImporterFormat = 
        new List<ImagesFormatPair> { new ImagesFormatPair("Default", TextureImporterFormat.Automatic, 2048), new ImagesFormatPair("Default", TextureImporterFormat.Automatic, 2048)}; // 贴图格式
    public int verticesLimit = 5000; // 顶点数限制
    public string standardPattern = @"^Assets/Models/SM_.*\.(fbx|obj)$"; // 需要匹配的正则表达式
    public string logExportPath = "Assets/Scripts/Editor/DebugLog";    // 打印生成的XML表的保存路径
    public List<ComponentPair> limitComponents = 
        new List<ComponentPair> { new ComponentPair("MeshRenderer", "///"), new ComponentPair("Animator", "//") }; // 检测是否存在组件 Key = 对应组件  Value = 添加的命名脏迹
}

[Serializable]
public class ComponentPair
{
    public string component;
    public string pattern;

    public ComponentPair(string component, string pattern)
    {
        this.component = component;
        this.pattern = pattern;
    }
}

[Serializable]
public class ImagesFormatPair
{
    public string targetPlatform;
    public TextureImporterFormat format;
    public int maxSize;

    public ImagesFormatPair(string targetPlatform, TextureImporterFormat format, int maxSize)
    {
        this.targetPlatform = targetPlatform;
        this.format = format;
        this.maxSize = maxSize; 
    }
}