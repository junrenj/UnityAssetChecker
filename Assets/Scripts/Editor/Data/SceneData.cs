public class SceneData
{
    public string filePath;
    public string sceneName;
    public bool isStandard = true;

    public SceneData(string filePath, string sceneName)
    {
        this.filePath = filePath;
        this.sceneName = sceneName;
    }
}
