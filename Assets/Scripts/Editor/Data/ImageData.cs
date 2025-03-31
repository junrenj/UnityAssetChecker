using System.Numerics;
using UnityEngine.UIElements;

public class ImageData
{
    public string filePath;
    public string name;
    public Vector2 size;
    public bool isStandard = true;
    public VisualElement ui;

    public ImageData(string path, string fileName)
    {
        this.filePath = path;
        this.name = fileName;
    }
}
