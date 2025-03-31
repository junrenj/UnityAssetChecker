public class MeshData
{
    public string filePath;
    public string name;
    public int vertices;
    public bool isStandard = true;

    public MeshData(string path, string name, string format)
    {
        this.filePath = path;
        this.name = name;
    }
}
