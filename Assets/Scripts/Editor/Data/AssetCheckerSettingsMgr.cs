using System.Collections.Generic;
using System.IO;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public static class AssetCheckerSettingsMgr
{
    /// <summary>
    /// �������õ� JSON �ļ�
    /// </summary>
    public static void SaveSettings(AssetCheckerSetting settings, string path)
    {
        AssetCheckerSetting setting = new AssetCheckerSetting();
        string json = JsonConvert.SerializeObject(setting, Formatting.Indented);
        File.WriteAllText(path, json);
        Debug.Log("�����ѱ��浽: " + path);
    }

    /// <summary>
    /// �� JSON �ļ���������
    /// </summary>
    public static AssetCheckerSetting LoadSettings(string path)
    {
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            AssetCheckerSetting setting = JsonConvert.DeserializeObject<AssetCheckerSetting>(json);
            return setting;
        }
        return null;
    }
}

