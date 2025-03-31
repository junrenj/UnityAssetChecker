using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

[CreateAssetMenu(fileName = "AssetCheckerSettings", menuName = "Settings/Asset Checker Settings")]
public class AssetCheckerSetting_SO : ScriptableObject
{
    public List<AssetCheckerSetting> assetCheckerSettingsList = new List<AssetCheckerSetting>();

    private void OnValidate()
    {
        AddNewItem();
    }

    public void AddNewItem()
    {
        AssetCheckerSetting asset = new AssetCheckerSetting
        {
            targetFolderPath = "Assets/Texture",
            textureImporterFormat = new List<ImagesFormatPair>
            {
                new ImagesFormatPair("Default", TextureImporterFormat.Automatic, 2048),
                new ImagesFormatPair("Standalone", TextureImporterFormat.RGBA32, 2048)
            },
            verticesLimit = 5000,
            standardPattern = @"^Assets/Models/SM_.*\.(fbx|obj)$",
            logExportPath = "Assets/Scripts/Editor/DebugLog",
            limitComponents = new List<ComponentPair> { new ComponentPair("MeshRenderer", "///"), new ComponentPair("Animator", "//") }
        };
    }
}