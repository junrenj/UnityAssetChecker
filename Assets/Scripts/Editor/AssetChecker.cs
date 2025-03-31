using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public class AssetChecker : EditorWindow
{
    [SerializeField]
    // UI Ԫ��
    private VisualTreeAsset m_VisualTreeAsset = default;
    private VisualTreeAsset fileDataTemp;
    private VisualTreeAsset debugMessageTemp;
    private ListView imgListView;
    private ListView meshesListView;
    private ListView sceneListView;
    private ListView debugLogListView;
    private Button btnSwitchTexmode;
    private Button btnSwitchMeshMode;
    private Button btnSwitchSceneMode;
    private Button btnImport;
    private Button btnFix;
    private Button btnCheck;
    private Button btnExportLog;
    private Button btnImportSetting;
    private Label lab_Details;

    // ִ��ģʽ
    private E_targetType executeType = E_targetType.Texture;

    // ��ʱͼƬ·����Ϣ
    private List<ImageData> imgsDataList = new List<ImageData>();
    private List<ImageData> unsatisfiedImgsList = new List<ImageData>();

    // ��ʱ������洢��Ϣ
    private List<MeshData> meshesDataList = new List<MeshData>();
    private List<MeshData> unsatisfiedMeshList = new List<MeshData>();

    // ��ʱ�����洢��Ϣ
    private List<SceneData> scenesDataList = new List<SceneData>();
    private List<SceneData> unsatisfiedSceneList = new List<SceneData>();

    // �洢��ӡ��Ϣ
    private List<string> logMessages = new List<string>();

    // �洢�ļ���趨��Ϣ
    private AssetCheckerSetting recentSetting;
    // �洢�ı��ص�ScriptableObject���õļ���趨��Ϣ
    private AssetCheckerSetting_SO assetCheckerSetting_SO;
    private string default_SO_Path = "Assets/Scripts/Editor/Data/AssetCheckerSettings_SO.asset";

    [MenuItem("Tools/AssetChecker")]
    public static void ShowExample()
    {
        AssetChecker wnd = GetWindow<AssetChecker>();
        wnd.titleContent = new GUIContent("AssetChecker");
    }

    public void CreateGUI()
    {
        // ���ø��ڵ�
        VisualElement root = rootVisualElement;

        // ��ʼ��UI
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);

        // �õ�ģ������
        fileDataTemp = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Editor/ImageIconTemplate.uxml");
        debugMessageTemp = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Editor/DebugMessTemplate.uxml");

        // �õ���Ӧ��ͼƬListView�ؼ�
        imgListView = root.Q<VisualElement>("TopPart").Q<VisualElement>("DetailPart").Q<ListView>("ImgsPanel");
        meshesListView = root.Q<VisualElement>("TopPart").Q<VisualElement>("DetailPart").Q<ListView>("MeshesPanel");
        sceneListView = root.Q<VisualElement>("TopPart").Q<VisualElement>("DetailPart").Q<ListView>("ScenesPanel");
        debugLogListView = root.Q<VisualElement>("BottomPart").Q<ListView>("DebugList");
        // ���label
        lab_Details = root.Q<VisualElement>("TopPart").Q<VisualElement>("DetailPart").Q<Label>("Lab_Detail");

        // �õ�Ĭ�ϵ�ScriptableObject��Ϣ ��һû������Json�ļ�
        FindLocalSOFile();
        recentSetting = assetCheckerSetting_SO.assetCheckerSettingsList[0];

        #region �����а�ť����¼�
        // �����밴ť����¼�
        btnImport = root.Q<Button>("btn_Import");
        btnImport.clicked += OnImportBtnClicked;

        // ���޸���ť����¼�
        btnFix = root.Q<Button>("btn_Fix");
        btnFix.clicked += OnFixBtnClicked;

        // ����鰴ť����¼�
        btnCheck = root.Q<Button>("btn_Check");
        btnCheck.clicked += OnCheckBtnClicked;

        // ���л���ͼģʽ��ť����¼�
        btnSwitchTexmode = root.Q<Button>("Btn_Texture");
        btnSwitchTexmode.clicked += OnSwitchTexBtnClicked;

        // ���л�����ģʽ��ť����¼�
        btnSwitchMeshMode = root.Q<Button>("Btn_Mesh");
        btnSwitchMeshMode.clicked += OnSwitchMeshBtnClicked;

        // ���л�����ģʽ��ť����¼�
        btnSwitchSceneMode = root.Q<Button>("Btn_Scene");
        btnSwitchSceneMode.clicked += OnSwitchSceneBtnClicked;

        // ��������ӡ��Ϣ��ť����¼�
        btnExportLog = root.Q<Button>("Btn_ExportLog");
        btnExportLog.clicked += OnExportLogBtnClicked;

        // �����밴ť
        btnImportSetting = root.Q<Button>("Btn_ImportSetting");
        btnImportSetting.clicked += OnImportSettingBtnClicked;
        #endregion

        // ����ListView��ͼ
        GenerateDebugMessageListView();
        GenerateMeshesList();
        GenerateImagesList();
        GenerateScenesList();

        // �л�ģʽ
        meshesListView.style.display = DisplayStyle.None;
        sceneListView.style.display = DisplayStyle.None;
    }


    /// <summary>
    /// ����ͼƬ��ʾ����
    /// </summary>
    private void GenerateImagesList()
    {
        Func<VisualElement> makeItem = () => fileDataTemp.CloneTree();
        Action<VisualElement, int> bindItem = (e, i) =>
        {
            if(i < imgsDataList.Count)
            {
                Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(imgsDataList[i].filePath);
                imgsDataList[i].size.X = t.width;
                imgsDataList[i].size.Y = t.height;
                TextureImporter importer = AssetImporter.GetAtPath(imgsDataList[i].filePath) as TextureImporter;
                TextureImporterFormat format = TextureImporterFormat.DXT1;
                if (importer != null)
                {
                    format = importer.GetPlatformTextureSettings("Default").format;
                }
                if (t != null)
                {
                        e.Q<VisualElement>("Icon").style.backgroundImage = t;
                        e.Q<Label>("FileName").text = t.name;
                        e.Q<Label>("Size").text = imgsDataList[i].size.X.ToString() + " x " + imgsDataList[i].size.Y.ToString();
                        e.Q<Label>("Format").text = format.ToString();
                        e.Q<Label>("Path").text = imgsDataList[i].filePath;
                        e.Q<Label>("FileName").style.color = imgsDataList[i].isStandard ? Color.white : Color.red;
                }

            }
        };
        imgListView.itemsSource = imgsDataList;
        imgListView.makeItem = makeItem;
        imgListView.bindItem = bindItem;
        imgListView.fixedItemHeight = 100; 
        imgListView.selectionChanged += (selection) =>
        {
            if (imgListView.selectedIndex >= 0 && imgListView.selectedIndex < imgsDataList.Count)
            {
                ImageData selectedData = imgsDataList[imgListView.selectedIndex];
                // ��ȡ����ͼƬ��Դ
                if (selectedData != null)
                {
                        // �� Project ����ѡ�в�����
                        UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(selectedData.filePath);
                        Selection.activeObject = asset;
                        EditorGUIUtility.PingObject(asset);
                }
            }
        };
    }

    /// <summary>
    /// ��������Ԥ������
    /// </summary>
    private void GenerateMeshesList()
    {
        Func<VisualElement> makeItem_1 = () => fileDataTemp.CloneTree();
        Action<VisualElement, int> bindItem_1 = (e, i) =>
        {
            if (i < meshesDataList.Count)
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(meshesDataList[i].filePath);
                Texture2D pre_Tex = AssetPreview.GetAssetPreview(model); 
                if (model == null)
                {
                    logMessages.Add("�޷�����ģ�ͣ�����·���Ƿ���ȷ��" + meshesDataList[i].filePath);
                    return;
                }

                int totalVertices = 0;

                // �������� MeshFilter ��������ھ�̬ģ�ͣ�
                foreach (MeshFilter meshFilter in model.GetComponentsInChildren<MeshFilter>())
                {
                    if (meshFilter.sharedMesh != null)
                    {
                        totalVertices += meshFilter.sharedMesh.vertexCount;
                    }
                }

                // �������� SkinnedMeshRenderer �����������Ƥģ�ͣ�
                foreach (SkinnedMeshRenderer skinnedMesh in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (skinnedMesh.sharedMesh != null)
                    {
                        totalVertices += skinnedMesh.sharedMesh.vertexCount;
                    }
                }
                meshesDataList[i].vertices = totalVertices;
                if (pre_Tex != null)
                {
                    e.Q<VisualElement>("Icon").style.backgroundImage = pre_Tex;
                    e.Q<Label>("FileName").text = meshesDataList[i].name;
                    e.Q<Label>("Size").text = $"�������: {meshesDataList[i].vertices}";
                    e.Q<Label>("Format").text = "";
                    e.Q<Label>("Path").text = meshesDataList[i].filePath;
                    e.Q<Label>("FileName").style.color = meshesDataList[i].isStandard ? Color.white : Color.red;
                }

            }
        };
        meshesListView.itemsSource = meshesDataList;
        meshesListView.makeItem = makeItem_1;
        meshesListView.bindItem = bindItem_1;
        meshesListView.fixedItemHeight = 100;
        meshesListView.selectionChanged += (selection) =>
        {
            if (meshesListView.selectedIndex >= 0 && meshesListView.selectedIndex < meshesDataList.Count)
            {
                MeshData selectedData = meshesDataList[meshesListView.selectedIndex];
                // ��ȡѡ�����������Դ
                if (selectedData != null)
                {
                    // �� Project ����ѡ�в�����
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(selectedData.filePath);
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }
        };
    }

    /// <summary>
    /// ������������
    /// </summary>
    private void GenerateScenesList()
    {
        Func<VisualElement> makeItem_2 = () => fileDataTemp.CloneTree();
        Action<VisualElement, int> bindItem_2 = (e, i) =>
        {
            if (i < scenesDataList.Count)
            {
                SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenesDataList[i].filePath);
                scenesDataList[i].sceneName = scene.name;
                if (scene == null)
                {
                    logMessages.Add("�޷����س���������·���Ƿ���ȷ��" + scenesDataList[i].filePath);
                    return;
                }
                //e.Q<VisualElement>("Icon").style.backgroundImage = pre_Tex;
                e.Q<Label>("Path").text = scenesDataList[i].filePath;
                e.Q<Label>("FileName").text = scene.name;
                e.Q<Label>("Size").text = "";
                e.Q<Label>("Format").text = "";
                e.Q<Label>("FileName").style.color = scenesDataList[i].isStandard ? Color.white : Color.red;

            }
        };
        sceneListView.itemsSource = scenesDataList;
        sceneListView.makeItem = makeItem_2;
        sceneListView.bindItem = bindItem_2;
        sceneListView.fixedItemHeight = 100;
        sceneListView.selectionChanged += (selection) =>
        {
            if (sceneListView.selectedIndex >= 0 && sceneListView.selectedIndex < scenesDataList.Count)
            {
                SceneData selectedData = scenesDataList[sceneListView.selectedIndex];
                // ��ȡѡ�����������Դ
                if (selectedData != null)
                {
                    // �� Project ����ѡ�в�����
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(selectedData.filePath);
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }
        };
    }

    /// <summary>
    /// ������ӡ̨��ӡ��Ϣ����
    /// </summary>
    private void GenerateDebugMessageListView()
    {
        Func<VisualElement> makeItem = () => debugMessageTemp.CloneTree();
        Action<VisualElement, int> bindItem = (e, i) =>
        {
            if (i <= logMessages.Count)
            {
                e.Q<Label>("Log").text = logMessages[i];
            }
        };
        debugLogListView.itemsSource = logMessages;
        debugLogListView.makeItem = makeItem;
        debugLogListView.bindItem = bindItem;
    }

    #region �л��������
    private void OnSwitchTexBtnClicked()
    {
        SwitchMode(E_targetType.Texture);
    }
    private void OnSwitchMeshBtnClicked()
    {
        SwitchMode(E_targetType.Mesh);
    }
    private void OnSwitchSceneBtnClicked()
    {
        SwitchMode(E_targetType.Scene);
    }
    private void SwitchMode(E_targetType targetType)
    {
        switch (targetType)
        {
            case E_targetType.Texture:
                imgListView.style.display = DisplayStyle.Flex;
                meshesListView.style.display = DisplayStyle.None;
                sceneListView.style.display = DisplayStyle.None;
                lab_Details.text = "�����ͼƬ";
                break;
            case E_targetType.Mesh:
                imgListView.style.display = DisplayStyle.None;
                meshesListView.style.display = DisplayStyle.Flex;
                sceneListView.style.display = DisplayStyle.None;
                lab_Details.text = "�����������";
                break;
            case E_targetType.Scene:
                imgListView.style.display = DisplayStyle.None;
                meshesListView.style.display = DisplayStyle.None;
                sceneListView.style.display = DisplayStyle.Flex;
                lab_Details.text = "����ĳ���";
                break;
        }

        this.executeType = targetType;
    }
    #endregion

    #region �������
    /// <summary>
    /// ���밴ť�����¼�
    /// </summary>
    private void OnImportBtnClicked()
    {
        switch (executeType)
        {
            case E_targetType.Texture:
                ImportImages();
                break;
            case E_targetType.Mesh:
                ImportMeshes();
                break;
            case E_targetType.Scene:
                ImportSceneFile();
                break;
        }
    }

    /// <summary>
    /// ����ͼƬ
    /// </summary>
    private void ImportImages()
    {
        string directory;
        directory = EditorUtility.OpenFolderPanel("ѡ����ͼ�ļ���", "", "");
        if (!Directory.Exists(directory))
        {
            logMessages.Add("Ŀ���ļ��в�����: " + directory);
            return;
        }

        // ����Ҫ��������չ��
        string[] extensions = { "*.png", "*.jpg", "*.tif", "*.tiff" };

        List<string> imgsPathList = new List<string>();

        // ����������չ��
        foreach (string ext in extensions)
        {
            imgsPathList.AddRange(Directory.GetFiles(directory, ext, SearchOption.AllDirectories));
        }

        string projectPath = Application.dataPath;

        imgsDataList.Clear();
        // ת��Ϊ���·��
        for (int i = 0; i < imgsPathList.Count; i++)
        {
            imgsPathList[i] = "Assets" + imgsPathList[i].Replace(projectPath, "").Replace("\\", "/");
            ImageData data = new ImageData(imgsPathList[i], Path.GetFileName(imgsPathList[i]));
            imgsDataList.Add(data);
        }

        imgListView.Rebuild();
    }

    /// <summary>
    /// �������·�� ��ȡͼƬ��Ϣ
    /// </summary>
    /// <param name="directory"></param>
    private void ImportImages(string directory)
    {
        string fullPath = Path.Combine(Application.dataPath, directory.Replace("Assets/", ""));
        if (!Directory.Exists(fullPath))
        {
            logMessages.Add("Ŀ���ļ��в�����: " + fullPath);
            return;
        }

        // ����Ҫ��������չ��
        string[] extensions = { "*.png", "*.jpg", "*.tif", "*.tiff" };

        List<string> imgsPathList = new List<string>();

        // ����������չ��
        foreach (string ext in extensions)
        {
            imgsPathList.AddRange(Directory.GetFiles(fullPath, ext, SearchOption.AllDirectories));
        }

        string projectPath = Application.dataPath;

        imgsDataList.Clear();
        // ת��Ϊ���·��
        for (int i = 0; i < imgsPathList.Count; i++)
        {
            imgsPathList[i] = "Assets" + imgsPathList[i].Replace(projectPath, "").Replace("\\", "/");
            ImageData data = new ImageData(imgsPathList[i], Path.GetFileName(imgsPathList[i]));
            imgsDataList.Add(data);
        }

        imgListView.Rebuild();
    }

    /// <summary>
    /// ��������
    /// </summary>
    private void ImportMeshes()
    {
        string directory;
        directory = EditorUtility.OpenFolderPanel("ѡ��ģ���ļ���", "", "");
        if (!Directory.Exists(directory))
        {
            logMessages.Add("Ŀ���ļ��в�����: " + directory);
            return;
        }
        // ����Ҫ��������չ��
        string[] extensions = { "*.obj", "*.fbx"};
        List<string> meshesPathList = new List<string>();

        // ����������չ��
        foreach (string ext in extensions)
        {
            meshesPathList.AddRange(Directory.GetFiles(directory, ext, SearchOption.AllDirectories));
        }

        string projectPath = Application.dataPath;
        string extension = Path.GetExtension(projectPath);
        meshesDataList.Clear();
        // ת��Ϊ���·��
        for (int i = 0; i < meshesPathList.Count; i++)
        {
            meshesPathList[i] = "Assets" + meshesPathList[i].Replace(projectPath, "").Replace("\\", "/");
            MeshData data = new MeshData(meshesPathList[i], Path.GetFileName(meshesPathList[i]), extension);
            meshesDataList.Add(data);
        }
        meshesListView.Rebuild();
    }

    /// <summary>
    /// �������·�� ��ȡ����
    /// </summary>
    /// <param name="directory"></param>
    private void ImportMeshes(string directory)
    {
        string fullPath = Path.Combine(Application.dataPath, directory.Replace("Assets/", ""));
        if (!Directory.Exists(fullPath))
        {
            logMessages.Add("Ŀ���ļ��в�����: " + fullPath);
            return;
        }
        // ����Ҫ��������չ��
        string[] extensions = { "*.obj", "*.fbx" };
        List<string> meshesPathList = new List<string>();

        // ����������չ��
        foreach (string ext in extensions)
        {
            meshesPathList.AddRange(Directory.GetFiles(fullPath, ext, SearchOption.AllDirectories));
        }

        string projectPath = Application.dataPath;
        string extension = Path.GetExtension(projectPath);
        meshesDataList.Clear();
        // ת��Ϊ���·��
        for (int i = 0; i < meshesPathList.Count; i++)
        {
            meshesPathList[i] = "Assets" + meshesPathList[i].Replace(projectPath, "").Replace("\\", "/");
            MeshData data = new MeshData(meshesPathList[i], Path.GetFileName(meshesPathList[i]), extension);
            meshesDataList.Add(data);
        }
        meshesListView.Rebuild();
    }

    /// <summary>
    /// ���볡��
    /// </summary>
    private void ImportSceneFile()
    {
        string directory;
        directory = EditorUtility.OpenFolderPanel("ѡ�񳡾��ļ���", "", "");
        if (!Directory.Exists(directory))
        {
            logMessages.Add("Ŀ���ļ��в�����: " + directory);
            return;
        }
        // ����Ҫ��������չ��
        string[] extensions = { "*.unity"};
        List<string> scenePathsList = new List<string>();

        // ����������չ��
        foreach (string ext in extensions)
        {
            scenePathsList.AddRange(Directory.GetFiles(directory, ext, SearchOption.AllDirectories));
        }

        string projectPath = Application.dataPath;
        scenesDataList.Clear();
        // ת��Ϊ���·��
        for (int i = 0; i < scenePathsList.Count; i++)
        {
            scenePathsList[i] = "Assets" + scenePathsList[i].Replace(projectPath, "").Replace("\\", "/");
            SceneData data = new SceneData(scenePathsList[i], Path.GetFileName(scenePathsList[i]));
            scenesDataList.Add(data);
        }
        sceneListView.Rebuild();

    }

    /// <summary>
    /// �������·�� ��ȡ�����ļ�
    /// </summary>
    private void ImportSceneFile(string directory)
    {
        string fullPath = Path.Combine(Application.dataPath, directory.Replace("Assets/", ""));
        if (!Directory.Exists(fullPath))
        {
            logMessages.Add("Ŀ���ļ��в�����: " + fullPath);
            return;
        }
        // ����Ҫ��������չ��
        string[] extensions = { "*.unity" };
        List<string> scenePathsList = new List<string>();

        // ����������չ��
        foreach (string ext in extensions)
        {
            scenePathsList.AddRange(Directory.GetFiles(fullPath, ext, SearchOption.AllDirectories));
        }

        string projectPath = Application.dataPath;
        scenesDataList.Clear();
        // ת��Ϊ���·��
        for (int i = 0; i < scenePathsList.Count; i++)
        {
            scenePathsList[i] = "Assets" + scenePathsList[i].Replace(projectPath, "").Replace("\\", "/");
            SceneData data = new SceneData(scenePathsList[i], Path.GetFileName(scenePathsList[i]));
            scenesDataList.Add(data);
        }
        sceneListView.Rebuild();

    }


    #endregion

    #region ������
    /// <summary>
    /// ��鰴ť����
    /// </summary>
    private void OnCheckBtnClicked()
    {
        switch (executeType)
        {
            case E_targetType.Texture:
                CheckImages();
                break;
            case E_targetType.Mesh:
                CheckMesh();
                break;
            case E_targetType.Scene:
                CheckScenes();
                break;
        }
    }

    /// <summary>
    /// ���ͼƬ��
    /// </summary>
    private void CheckImages()
    {
        // ��վ��б�
        unsatisfiedImgsList.Clear();
        logMessages.Clear();
        int count = 0;
        // ����ͼƬ��Ϣ�б�
        foreach (var imgData in imgsDataList)
        {
            if (imgData != null)
            {
                imgData.isStandard = CheckImageSizeAndFormat(imgData);
                if (!imgData.isStandard)
                {
                    // ˵����ͨ��
                    unsatisfiedImgsList.Add(imgData);
                    count++;
                }
            }
        }

        SortAndRefreshImgsList();
        logMessages.Add($"������,�ܹ���{count}���ļ������Ϲ淶");
        debugLogListView.Rebuild();
    }

    /// <summary>
    /// ��鵥��ͼƬ��С�͸�ʽ
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private bool CheckImageSizeAndFormat(ImageData data)
    {
        string assetPath = data.filePath;
        bool isStandard = true;

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            logMessages.Add($"�޷���ȡ TextureImporter: {assetPath}");
            return false;
        }

        for (int i = 0; i < recentSetting.textureImporterFormat.Count; i++)
        {
            TextureImporterPlatformSettings textureSetting = importer.GetPlatformTextureSettings(recentSetting.textureImporterFormat[i].targetPlatform);
            // �����ͼ�ߴ�
            if (textureSetting.maxTextureSize > recentSetting.textureImporterFormat[i].maxSize)
            {
                logMessages.Add("�ļ�·��: " + data.filePath + " �����Ϲ淶ԭ��: ������ͼ���Χ");
                isStandard = false;
            }
            if (textureSetting.format != recentSetting.textureImporterFormat[i].format)
            {
                logMessages.Add("�ļ�·��: " + data.filePath + $" �����Ϲ淶ԭ��: {recentSetting.textureImporterFormat[i].targetPlatform} ��ѹ����ʽ����, ��ǰ��ʽ: " + textureSetting.format);
                isStandard = false;
            }
            

        }

        return isStandard;
    }

    /// <summary>
    /// ͼƬ������������ˢ�����
    /// </summary>
    private void SortAndRefreshImgsList()
    {
        // falseԪ������ǰ�棬true�����ں���
        imgsDataList = imgsDataList.OrderBy(item => item.isStandard).ToList();
        imgListView.itemsSource = imgsDataList;
        imgListView.RefreshItems();
    }

    /// <summary>
    /// ��������弯��
    /// </summary>
    private void CheckMesh()
    {
        // ��վ��б�
        unsatisfiedMeshList.Clear(); 
        logMessages.Clear();
        int count = 0;
        // ����ͼƬ��Ϣ�б�
        foreach (var meshData in meshesDataList)
        {
            if (meshData != null)
            {
                meshData.isStandard = CheckMeshVerticesAndName(meshData);
                if (!meshData.isStandard)
                {
                    // ˵����ͨ��
                    unsatisfiedMeshList.Add(meshData);
                    count++;
                }
            }
        }

        SortAndRefreshMeshList();
        logMessages.Add($"������,�ܹ���{count}���ļ������Ϲ淶");
        debugLogListView.Rebuild();
    }

    /// <summary>
    /// ��鵥�������嶥������·��
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private bool CheckMeshVerticesAndName(MeshData data)
    {
        bool isStandard = true;
        // ��鶥��������û�г�����
        if(data.vertices > recentSetting.verticesLimit)
        {
            logMessages.Add("�ļ���: " + data.name + " �����Ϲ淶ԭ��: �������������������");
            isStandard = false;
        }
        // ���������û�з���������ʽ
        if (!Regex.IsMatch(data.filePath, recentSetting.standardPattern))
        {
            logMessages.Add("�ļ���: " + data.name + " �����Ϲ淶ԭ��: ������Ԥ���������ʽ: " + recentSetting.standardPattern);
            isStandard = false;
        }
        return isStandard;
    }

    /// <summary>
    /// ����������������ˢ��
    /// </summary>
    private void SortAndRefreshMeshList()
    {
        // falseԪ������ǰ�棬true�����ں���
        meshesDataList = meshesDataList.OrderBy(item => item.isStandard).ToList();
        meshesListView.itemsSource = meshesDataList;
        meshesListView.RefreshItems();
    }

    /// <summary>
    /// ��鳡������
    /// </summary>
    private void CheckScenes()
    {
        // ��վ��б�
        unsatisfiedMeshList.Clear();
        logMessages.Clear();
        int count = 0;
        // ����ͼƬ��Ϣ�б�
        foreach (var scene in scenesDataList)
        {
            if (scene != null)
            {
                scene.isStandard = CheckScenesHasComponent(scene);
                if (!scene.isStandard)
                {
                    // ˵����ͨ��
                    unsatisfiedSceneList.Add(scene);
                    count++;
                }
            }
        }

        SortAndRefreshSceneList();
        logMessages.Add($"������,�ܹ���{count}���ļ������Ϲ淶");
        debugLogListView.Rebuild();
    }

    /// <summary>
    /// ��鵥�������Ƿ������
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private bool CheckScenesHasComponent(SceneData data)
    {
        bool isStandard = true;
        if (!File.Exists(data.filePath))
        {
            logMessages.Add("�����ļ������ڣ�" + data.filePath);
            return false;
        }

        // �ȱ��浱ǰ�򿪵ĳ���
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            // ����ָ������������ Play ģʽ�£�
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(data.filePath);
            if (sceneAsset == null)
            {
                logMessages.Add("�޷����س�����" + data.filePath);
                return false;
            }

            EditorSceneManager.OpenScene(data.filePath, OpenSceneMode.Single);
            int modifiedCount = 0;

            // ��ȡ�����е����� GameObject
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);
            List<ComponentPair> limitComponents = recentSetting.limitComponents;
            foreach (GameObject obj in allObjects)
            {
                for (int i = 0; i < limitComponents.Count; i++) 
                {
                    if (obj.GetComponent(limitComponents[i].component) != null)
                    {
                        logMessages.Add($"���� {data.filePath} ��{obj.name}����{limitComponents[i].component}���");
                        if (!obj.name.StartsWith(limitComponents[i].pattern)) // �����ظ����
                        {
                            obj.name = limitComponents[i].pattern + obj.name;
                            modifiedCount++;
                            isStandard = false;
                        }
                    }
                }
            }

            // ִ����ɴ�ӡ��Ϣ
            logMessages.Add($"���� {data.filePath} ������ɣ������ {modifiedCount} ����������ơ�");

            // ���泡���޸�
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        return isStandard;
    }

    /// <summary>
    /// �������г�������
    /// </summary>
    private void SortAndRefreshSceneList()
    {
        // falseԪ������ǰ�棬true�����ں���
        scenesDataList = scenesDataList.OrderBy(item => item.isStandard).ToList();
        sceneListView.itemsSource = scenesDataList;
        sceneListView.RefreshItems();
    }
    #endregion

    #region һ���޸����
    /// <summary>
    /// �޸���ť�����¼�
    /// </summary>
    private void OnFixBtnClicked()
    {
        if (executeType != E_targetType.Texture)
        {
            logMessages.Add("ֻ����ͼ�ļ�֧���Զ��޸�");
            debugLogListView.Rebuild();
            return;
        }
        foreach (var data in unsatisfiedImgsList)
        {
            if (FixTexture(data.filePath))
            {
                // �����ɹ��� �ĳɱ�׼��
                data.isStandard = true;
            }
        }
        // ˢ�����
        imgListView.Rebuild();
    }

    /// <summary>
    /// �޸�����
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private bool FixTexture(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            logMessages.Add($"�޷���ȡ TextureImporter: {path}");
            return false;
        }

        // ���ղ�ͬƽ̨Ҫ���޸�ͼƬ
        for (int i = 0; i < recentSetting.textureImporterFormat.Count; i++)
        {
            ApplyTextureSettings(importer, recentSetting.textureImporterFormat[i].targetPlatform, 
                recentSetting.textureImporterFormat[i].format, recentSetting.textureImporterFormat[i].maxSize);
        }

        // ������޸� & ���µ���
        EditorUtility.SetDirty(importer);
        AssetDatabase.WriteImportSettingsIfDirty(path);
        AssetDatabase.ImportAsset(path);
        AssetDatabase.Refresh();

        return true;
    }

    /// <summary>
    /// Ӧ�ò�ͬƽ̨��ͼƬ��ʽ����
    /// </summary>
    /// <param name="importer"></param>
    /// <param name="platform"></param>
    /// <param name="format"></param>
    /// <param name="maxSize"></param>
    private void ApplyTextureSettings(TextureImporter importer, string platform, TextureImporterFormat format, int maxSize)
    {
        importer.ClearPlatformTextureSettings(platform); // �����������

        TextureImporterPlatformSettings settings = new TextureImporterPlatformSettings
        {
            name = platform,
            overridden = true, // ǿ�Ƹ���
            format = format,
            maxTextureSize = maxSize
            
        };

        importer.SetPlatformTextureSettings(settings);
    }


    #endregion

    #region ��ӡ̨��Ϣ���
    private void OnExportLogBtnClicked()
    {
        // 1. ȷ�� DebugLog �ļ��д���
        string folderPath = recentSetting.logExportPath;
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh(); // ˢ�� Unity ��Դ������
        }

        // 2. �����ļ�����Debug_��ǰʱ��.xml��
        string fileName = $"Debug_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.xml";
        string filePath = Path.Combine(folderPath, fileName);

        // 3. ���� XML �ĵ�
        XmlDocument xmlDoc = new XmlDocument();

        // ���� XML ���ڵ�
        XmlElement rootElement = xmlDoc.CreateElement("DebugLog");
        xmlDoc.AppendChild(rootElement);

        // 4. ���� List<string> ��д�� XML
        foreach (string log in logMessages)
        {
            XmlElement logElement = xmlDoc.CreateElement("LogEntry");
            logElement.InnerText = log;
            rootElement.AppendChild(logElement);
        }

        // 5. ���� XML �ļ�
        xmlDoc.Save(filePath);

        // 6. ˢ�� Unity ��Դ������
        AssetDatabase.Refresh();
    }
    #endregion

    #region ��ȡ����������
    /// <summary>
    /// �ҵ�Ĭ�ϵı���SO�ļ�
    /// </summary>
    private void FindLocalSOFile()
    {
        // ���Լ������е� ScriptableObject
        AssetCheckerSetting_SO settings = AssetDatabase.LoadAssetAtPath<AssetCheckerSetting_SO>(default_SO_Path);

        if (settings == null)
        {
            // �Ҳ����򴴽�һ���µ� ScriptableObject
            settings = ScriptableObject.CreateInstance<AssetCheckerSetting_SO>();

            // ȷ��Ŀ¼����
            string directory = Path.GetDirectoryName(default_SO_Path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // ���������� ScriptableObject �ʲ�
            AssetDatabase.CreateAsset(settings, default_SO_Path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        else
        {
            logMessages.Add("�ɹ����ر���SO�����ļ�");
        }
        assetCheckerSetting_SO = settings;
    }

    /// <summary>
    /// ����Json����
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void OnImportSettingBtnClicked()
    {
        // ���û�ѡȡһ��Json�ļ�
        string path = EditorUtility.OpenFilePanel("ѡ�� JSON �ļ�", "", "json");
        // ����û�û��ѡ���ļ���path ���ǿ��ַ���
        if (!string.IsNullOrEmpty(path))
        {
            Debug.Log("�û�ѡ��� JSON �ļ�·����" + path);
        }
        else
        {
            Debug.LogWarning("δѡ�� JSON �ļ�");
        }
        if(AssetCheckerSettingsMgr.LoadSettings(path) == null)
        {
            logMessages.Add("����ʧ�ܣ��ļ�Ϊ��");
            return;
        }

        recentSetting = AssetCheckerSettingsMgr.LoadSettings(path);
        // ��ʼ���ռ�����ͼ��
        SwitchMode(recentSetting.targetType);
        switch (executeType)
        {
            case E_targetType.Texture:
                ImportImages(recentSetting.targetFolderPath);
                CheckImages();
                break;
            case E_targetType.Mesh:
                ImportMeshes(recentSetting.targetFolderPath);
                CheckMesh();
                break;
            case E_targetType.Scene:
                ImportSceneFile(recentSetting.targetFolderPath);
                CheckScenes();
                break;
        }
    }
    #endregion


}
