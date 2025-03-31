using System;
using UnityEditor;
using System.Collections.Generic;

[Serializable] // ��������л�
public class AssetCheckerSetting
{
    public string targetFolderPath = "Assets/Texture"; // ָ��Ŀ¼
    public E_targetType targetType = E_targetType.Texture; // �������ļ���ʽ
    public List<ImagesFormatPair> textureImporterFormat = 
        new List<ImagesFormatPair> { new ImagesFormatPair("Default", TextureImporterFormat.Automatic, 2048), new ImagesFormatPair("Default", TextureImporterFormat.Automatic, 2048)}; // ��ͼ��ʽ
    public int verticesLimit = 5000; // ����������
    public string standardPattern = @"^Assets/Models/SM_.*\.(fbx|obj)$"; // ��Ҫƥ���������ʽ
    public string logExportPath = "Assets/Scripts/Editor/DebugLog";    // ��ӡ���ɵ�XML��ı���·��
    public List<ComponentPair> limitComponents = 
        new List<ComponentPair> { new ComponentPair("MeshRenderer", "///"), new ComponentPair("Animator", "//") }; // ����Ƿ������� Key = ��Ӧ���  Value = ��ӵ������༣
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