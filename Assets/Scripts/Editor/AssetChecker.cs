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
    // UI 元素
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

    // 执行模式
    private E_targetType executeType = E_targetType.Texture;

    // 临时图片路径信息
    private List<ImageData> imgsDataList = new List<ImageData>();
    private List<ImageData> unsatisfiedImgsList = new List<ImageData>();

    // 临时网格体存储信息
    private List<MeshData> meshesDataList = new List<MeshData>();
    private List<MeshData> unsatisfiedMeshList = new List<MeshData>();

    // 临时场景存储信息
    private List<SceneData> scenesDataList = new List<SceneData>();
    private List<SceneData> unsatisfiedSceneList = new List<SceneData>();

    // 存储打印信息
    private List<string> logMessages = new List<string>();

    // 存储的检查设定信息
    private AssetCheckerSetting recentSetting;
    // 存储的本地的ScriptableObject配置的检查设定信息
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
        // 设置根节点
        VisualElement root = rootVisualElement;

        // 初始化UI
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);

        // 拿到模板数据
        fileDataTemp = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Editor/ImageIconTemplate.uxml");
        debugMessageTemp = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Editor/DebugMessTemplate.uxml");

        // 拿到对应的图片ListView控件
        imgListView = root.Q<VisualElement>("TopPart").Q<VisualElement>("DetailPart").Q<ListView>("ImgsPanel");
        meshesListView = root.Q<VisualElement>("TopPart").Q<VisualElement>("DetailPart").Q<ListView>("MeshesPanel");
        sceneListView = root.Q<VisualElement>("TopPart").Q<VisualElement>("DetailPart").Q<ListView>("ScenesPanel");
        debugLogListView = root.Q<VisualElement>("BottomPart").Q<ListView>("DebugList");
        // 获得label
        lab_Details = root.Q<VisualElement>("TopPart").Q<VisualElement>("DetailPart").Q<Label>("Lab_Detail");

        // 拿到默认的ScriptableObject信息 万一没有配置Json文件
        FindLocalSOFile();
        recentSetting = assetCheckerSetting_SO.assetCheckerSettingsList[0];

        #region 给所有按钮添加事件
        // 给导入按钮添加事件
        btnImport = root.Q<Button>("btn_Import");
        btnImport.clicked += OnImportBtnClicked;

        // 给修复按钮添加事件
        btnFix = root.Q<Button>("btn_Fix");
        btnFix.clicked += OnFixBtnClicked;

        // 给检查按钮添加事件
        btnCheck = root.Q<Button>("btn_Check");
        btnCheck.clicked += OnCheckBtnClicked;

        // 给切换贴图模式按钮添加事件
        btnSwitchTexmode = root.Q<Button>("Btn_Texture");
        btnSwitchTexmode.clicked += OnSwitchTexBtnClicked;

        // 给切换网格模式按钮添加事件
        btnSwitchMeshMode = root.Q<Button>("Btn_Mesh");
        btnSwitchMeshMode.clicked += OnSwitchMeshBtnClicked;

        // 给切换网格模式按钮添加事件
        btnSwitchSceneMode = root.Q<Button>("Btn_Scene");
        btnSwitchSceneMode.clicked += OnSwitchSceneBtnClicked;

        // 给导出打印信息按钮添加事件
        btnExportLog = root.Q<Button>("Btn_ExportLog");
        btnExportLog.clicked += OnExportLogBtnClicked;

        // 给导入按钮
        btnImportSetting = root.Q<Button>("Btn_ImportSetting");
        btnImportSetting.clicked += OnImportSettingBtnClicked;
        #endregion

        // 构建ListView视图
        GenerateDebugMessageListView();
        GenerateMeshesList();
        GenerateImagesList();
        GenerateScenesList();

        // 切换模式
        meshesListView.style.display = DisplayStyle.None;
        sceneListView.style.display = DisplayStyle.None;
    }


    /// <summary>
    /// 创建图片显示序列
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
                // 获取背景图片资源
                if (selectedData != null)
                {
                        // 在 Project 窗口选中并高亮
                        UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(selectedData.filePath);
                        Selection.activeObject = asset;
                        EditorGUIUtility.PingObject(asset);
                }
            }
        };
    }

    /// <summary>
    /// 创建网格预览序列
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
                    logMessages.Add("无法加载模型，请检查路径是否正确：" + meshesDataList[i].filePath);
                    return;
                }

                int totalVertices = 0;

                // 查找所有 MeshFilter 组件（用于静态模型）
                foreach (MeshFilter meshFilter in model.GetComponentsInChildren<MeshFilter>())
                {
                    if (meshFilter.sharedMesh != null)
                    {
                        totalVertices += meshFilter.sharedMesh.vertexCount;
                    }
                }

                // 查找所有 SkinnedMeshRenderer 组件（用于蒙皮模型）
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
                    e.Q<Label>("Size").text = $"顶点个数: {meshesDataList[i].vertices}";
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
                // 获取选择的网格体资源
                if (selectedData != null)
                {
                    // 在 Project 窗口选中并高亮
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(selectedData.filePath);
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }
        };
    }

    /// <summary>
    /// 创建场景序列
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
                    logMessages.Add("无法加载场景，请检查路径是否正确：" + scenesDataList[i].filePath);
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
                // 获取选择的网格体资源
                if (selectedData != null)
                {
                    // 在 Project 窗口选中并高亮
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(selectedData.filePath);
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }
        };
    }

    /// <summary>
    /// 创建打印台打印信息序列
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

    #region 切换功能相关
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
                lab_Details.text = "导入的图片";
                break;
            case E_targetType.Mesh:
                imgListView.style.display = DisplayStyle.None;
                meshesListView.style.display = DisplayStyle.Flex;
                sceneListView.style.display = DisplayStyle.None;
                lab_Details.text = "导入的网格体";
                break;
            case E_targetType.Scene:
                imgListView.style.display = DisplayStyle.None;
                meshesListView.style.display = DisplayStyle.None;
                sceneListView.style.display = DisplayStyle.Flex;
                lab_Details.text = "导入的场景";
                break;
        }

        this.executeType = targetType;
    }
    #endregion

    #region 导入相关
    /// <summary>
    /// 导入按钮按下事件
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
    /// 导入图片
    /// </summary>
    private void ImportImages()
    {
        string directory;
        directory = EditorUtility.OpenFolderPanel("选择贴图文件夹", "", "");
        if (!Directory.Exists(directory))
        {
            logMessages.Add("目标文件夹不存在: " + directory);
            return;
        }

        // 定义要搜索的扩展名
        string[] extensions = { "*.png", "*.jpg", "*.tif", "*.tiff" };

        List<string> imgsPathList = new List<string>();

        // 遍历所有扩展名
        foreach (string ext in extensions)
        {
            imgsPathList.AddRange(Directory.GetFiles(directory, ext, SearchOption.AllDirectories));
        }

        string projectPath = Application.dataPath;

        imgsDataList.Clear();
        // 转换为相对路径
        for (int i = 0; i < imgsPathList.Count; i++)
        {
            imgsPathList[i] = "Assets" + imgsPathList[i].Replace(projectPath, "").Replace("\\", "/");
            ImageData data = new ImageData(imgsPathList[i], Path.GetFileName(imgsPathList[i]));
            imgsDataList.Add(data);
        }

        imgListView.Rebuild();
    }

    /// <summary>
    /// 输入相对路径 获取图片信息
    /// </summary>
    /// <param name="directory"></param>
    private void ImportImages(string directory)
    {
        string fullPath = Path.Combine(Application.dataPath, directory.Replace("Assets/", ""));
        if (!Directory.Exists(fullPath))
        {
            logMessages.Add("目标文件夹不存在: " + fullPath);
            return;
        }

        // 定义要搜索的扩展名
        string[] extensions = { "*.png", "*.jpg", "*.tif", "*.tiff" };

        List<string> imgsPathList = new List<string>();

        // 遍历所有扩展名
        foreach (string ext in extensions)
        {
            imgsPathList.AddRange(Directory.GetFiles(fullPath, ext, SearchOption.AllDirectories));
        }

        string projectPath = Application.dataPath;

        imgsDataList.Clear();
        // 转换为相对路径
        for (int i = 0; i < imgsPathList.Count; i++)
        {
            imgsPathList[i] = "Assets" + imgsPathList[i].Replace(projectPath, "").Replace("\\", "/");
            ImageData data = new ImageData(imgsPathList[i], Path.GetFileName(imgsPathList[i]));
            imgsDataList.Add(data);
        }

        imgListView.Rebuild();
    }

    /// <summary>
    /// 导入网格
    /// </summary>
    private void ImportMeshes()
    {
        string directory;
        directory = EditorUtility.OpenFolderPanel("选择模型文件夹", "", "");
        if (!Directory.Exists(directory))
        {
            logMessages.Add("目标文件夹不存在: " + directory);
            return;
        }
        // 定义要搜索的扩展名
        string[] extensions = { "*.obj", "*.fbx"};
        List<string> meshesPathList = new List<string>();

        // 遍历所有扩展名
        foreach (string ext in extensions)
        {
            meshesPathList.AddRange(Directory.GetFiles(directory, ext, SearchOption.AllDirectories));
        }

        string projectPath = Application.dataPath;
        string extension = Path.GetExtension(projectPath);
        meshesDataList.Clear();
        // 转换为相对路径
        for (int i = 0; i < meshesPathList.Count; i++)
        {
            meshesPathList[i] = "Assets" + meshesPathList[i].Replace(projectPath, "").Replace("\\", "/");
            MeshData data = new MeshData(meshesPathList[i], Path.GetFileName(meshesPathList[i]), extension);
            meshesDataList.Add(data);
        }
        meshesListView.Rebuild();
    }

    /// <summary>
    /// 输入相对路径 获取网格
    /// </summary>
    /// <param name="directory"></param>
    private void ImportMeshes(string directory)
    {
        string fullPath = Path.Combine(Application.dataPath, directory.Replace("Assets/", ""));
        if (!Directory.Exists(fullPath))
        {
            logMessages.Add("目标文件夹不存在: " + fullPath);
            return;
        }
        // 定义要搜索的扩展名
        string[] extensions = { "*.obj", "*.fbx" };
        List<string> meshesPathList = new List<string>();

        // 遍历所有扩展名
        foreach (string ext in extensions)
        {
            meshesPathList.AddRange(Directory.GetFiles(fullPath, ext, SearchOption.AllDirectories));
        }

        string projectPath = Application.dataPath;
        string extension = Path.GetExtension(projectPath);
        meshesDataList.Clear();
        // 转换为相对路径
        for (int i = 0; i < meshesPathList.Count; i++)
        {
            meshesPathList[i] = "Assets" + meshesPathList[i].Replace(projectPath, "").Replace("\\", "/");
            MeshData data = new MeshData(meshesPathList[i], Path.GetFileName(meshesPathList[i]), extension);
            meshesDataList.Add(data);
        }
        meshesListView.Rebuild();
    }

    /// <summary>
    /// 导入场景
    /// </summary>
    private void ImportSceneFile()
    {
        string directory;
        directory = EditorUtility.OpenFolderPanel("选择场景文件夹", "", "");
        if (!Directory.Exists(directory))
        {
            logMessages.Add("目标文件夹不存在: " + directory);
            return;
        }
        // 定义要搜索的扩展名
        string[] extensions = { "*.unity"};
        List<string> scenePathsList = new List<string>();

        // 遍历所有扩展名
        foreach (string ext in extensions)
        {
            scenePathsList.AddRange(Directory.GetFiles(directory, ext, SearchOption.AllDirectories));
        }

        string projectPath = Application.dataPath;
        scenesDataList.Clear();
        // 转换为相对路径
        for (int i = 0; i < scenePathsList.Count; i++)
        {
            scenePathsList[i] = "Assets" + scenePathsList[i].Replace(projectPath, "").Replace("\\", "/");
            SceneData data = new SceneData(scenePathsList[i], Path.GetFileName(scenePathsList[i]));
            scenesDataList.Add(data);
        }
        sceneListView.Rebuild();

    }

    /// <summary>
    /// 输入相对路径 获取场景文件
    /// </summary>
    private void ImportSceneFile(string directory)
    {
        string fullPath = Path.Combine(Application.dataPath, directory.Replace("Assets/", ""));
        if (!Directory.Exists(fullPath))
        {
            logMessages.Add("目标文件夹不存在: " + fullPath);
            return;
        }
        // 定义要搜索的扩展名
        string[] extensions = { "*.unity" };
        List<string> scenePathsList = new List<string>();

        // 遍历所有扩展名
        foreach (string ext in extensions)
        {
            scenePathsList.AddRange(Directory.GetFiles(fullPath, ext, SearchOption.AllDirectories));
        }

        string projectPath = Application.dataPath;
        scenesDataList.Clear();
        // 转换为相对路径
        for (int i = 0; i < scenePathsList.Count; i++)
        {
            scenePathsList[i] = "Assets" + scenePathsList[i].Replace(projectPath, "").Replace("\\", "/");
            SceneData data = new SceneData(scenePathsList[i], Path.GetFileName(scenePathsList[i]));
            scenesDataList.Add(data);
        }
        sceneListView.Rebuild();

    }


    #endregion

    #region 检查相关
    /// <summary>
    /// 检查按钮按下
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
    /// 检查图片集
    /// </summary>
    private void CheckImages()
    {
        // 清空旧列表
        unsatisfiedImgsList.Clear();
        logMessages.Clear();
        int count = 0;
        // 遍历图片信息列表
        foreach (var imgData in imgsDataList)
        {
            if (imgData != null)
            {
                imgData.isStandard = CheckImageSizeAndFormat(imgData);
                if (!imgData.isStandard)
                {
                    // 说明不通过
                    unsatisfiedImgsList.Add(imgData);
                    count++;
                }
            }
        }

        SortAndRefreshImgsList();
        logMessages.Add($"检查完成,总共有{count}个文件不符合规范");
        debugLogListView.Rebuild();
    }

    /// <summary>
    /// 检查单张图片大小和格式
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
            logMessages.Add($"无法获取 TextureImporter: {assetPath}");
            return false;
        }

        for (int i = 0; i < recentSetting.textureImporterFormat.Count; i++)
        {
            TextureImporterPlatformSettings textureSetting = importer.GetPlatformTextureSettings(recentSetting.textureImporterFormat[i].targetPlatform);
            // 检查贴图尺寸
            if (textureSetting.maxTextureSize > recentSetting.textureImporterFormat[i].maxSize)
            {
                logMessages.Add("文件路径: " + data.filePath + " 不符合规范原因: 超出贴图最大范围");
                isStandard = false;
            }
            if (textureSetting.format != recentSetting.textureImporterFormat[i].format)
            {
                logMessages.Add("文件路径: " + data.filePath + $" 不符合规范原因: {recentSetting.textureImporterFormat[i].targetPlatform} 的压缩格式错误, 当前格式: " + textureSetting.format);
                isStandard = false;
            }
            

        }

        return isStandard;
    }

    /// <summary>
    /// 图片序列重新排序并刷新面板
    /// </summary>
    private void SortAndRefreshImgsList()
    {
        // false元素排在前面，true的排在后面
        imgsDataList = imgsDataList.OrderBy(item => item.isStandard).ToList();
        imgListView.itemsSource = imgsDataList;
        imgListView.RefreshItems();
    }

    /// <summary>
    /// 检查网格体集合
    /// </summary>
    private void CheckMesh()
    {
        // 清空旧列表
        unsatisfiedMeshList.Clear(); 
        logMessages.Clear();
        int count = 0;
        // 遍历图片信息列表
        foreach (var meshData in meshesDataList)
        {
            if (meshData != null)
            {
                meshData.isStandard = CheckMeshVerticesAndName(meshData);
                if (!meshData.isStandard)
                {
                    // 说明不通过
                    unsatisfiedMeshList.Add(meshData);
                    count++;
                }
            }
        }

        SortAndRefreshMeshList();
        logMessages.Add($"检查完成,总共有{count}个文件不符合规范");
        debugLogListView.Rebuild();
    }

    /// <summary>
    /// 检查单个网格体顶点数和路径
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private bool CheckMeshVerticesAndName(MeshData data)
    {
        bool isStandard = true;
        // 检查顶点数量有没有超限制
        if(data.vertices > recentSetting.verticesLimit)
        {
            logMessages.Add("文件名: " + data.name + " 不符合规范原因: 定点数量超过最大上限");
            isStandard = false;
        }
        // 检查命名有没有符合正则表达式
        if (!Regex.IsMatch(data.filePath, recentSetting.standardPattern))
        {
            logMessages.Add("文件名: " + data.name + " 不符合规范原因: 不符合预设的正则表达式: " + recentSetting.standardPattern);
            isStandard = false;
        }
        return isStandard;
    }

    /// <summary>
    /// 网格序列重新排序并刷新
    /// </summary>
    private void SortAndRefreshMeshList()
    {
        // false元素排在前面，true的排在后面
        meshesDataList = meshesDataList.OrderBy(item => item.isStandard).ToList();
        meshesListView.itemsSource = meshesDataList;
        meshesListView.RefreshItems();
    }

    /// <summary>
    /// 检查场景集合
    /// </summary>
    private void CheckScenes()
    {
        // 清空旧列表
        unsatisfiedMeshList.Clear();
        logMessages.Clear();
        int count = 0;
        // 遍历图片信息列表
        foreach (var scene in scenesDataList)
        {
            if (scene != null)
            {
                scene.isStandard = CheckScenesHasComponent(scene);
                if (!scene.isStandard)
                {
                    // 说明不通过
                    unsatisfiedSceneList.Add(scene);
                    count++;
                }
            }
        }

        SortAndRefreshSceneList();
        logMessages.Add($"检查完成,总共有{count}个文件不符合规范");
        debugLogListView.Rebuild();
    }

    /// <summary>
    /// 检查单个场景是否含有组件
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private bool CheckScenesHasComponent(SceneData data)
    {
        bool isStandard = true;
        if (!File.Exists(data.filePath))
        {
            logMessages.Add("场景文件不存在：" + data.filePath);
            return false;
        }

        // 先保存当前打开的场景
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            // 加载指定场景（不在 Play 模式下）
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(data.filePath);
            if (sceneAsset == null)
            {
                logMessages.Add("无法加载场景：" + data.filePath);
                return false;
            }

            EditorSceneManager.OpenScene(data.filePath, OpenSceneMode.Single);
            int modifiedCount = 0;

            // 获取场景中的所有 GameObject
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);
            List<ComponentPair> limitComponents = recentSetting.limitComponents;
            foreach (GameObject obj in allObjects)
            {
                for (int i = 0; i < limitComponents.Count; i++) 
                {
                    if (obj.GetComponent(limitComponents[i].component) != null)
                    {
                        logMessages.Add($"场景 {data.filePath} 的{obj.name}存在{limitComponents[i].component}组件");
                        if (!obj.name.StartsWith(limitComponents[i].pattern)) // 避免重复添加
                        {
                            obj.name = limitComponents[i].pattern + obj.name;
                            modifiedCount++;
                            isStandard = false;
                        }
                    }
                }
            }

            // 执行完成打印信息
            logMessages.Add($"场景 {data.filePath} 处理完成，标记了 {modifiedCount} 个物体的名称。");

            // 保存场景修改
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        return isStandard;
    }

    /// <summary>
    /// 重新排列场景集合
    /// </summary>
    private void SortAndRefreshSceneList()
    {
        // false元素排在前面，true的排在后面
        scenesDataList = scenesDataList.OrderBy(item => item.isStandard).ToList();
        sceneListView.itemsSource = scenesDataList;
        sceneListView.RefreshItems();
    }
    #endregion

    #region 一键修复相关
    /// <summary>
    /// 修复按钮按下事件
    /// </summary>
    private void OnFixBtnClicked()
    {
        if (executeType != E_targetType.Texture)
        {
            logMessages.Add("只有贴图文件支持自动修复");
            debugLogListView.Rebuild();
            return;
        }
        foreach (var data in unsatisfiedImgsList)
        {
            if (FixTexture(data.filePath))
            {
                // 修正成功后 改成标准了
                data.isStandard = true;
            }
        }
        // 刷新面板
        imgListView.Rebuild();
    }

    /// <summary>
    /// 修复材质
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private bool FixTexture(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            logMessages.Add($"无法获取 TextureImporter: {path}");
            return false;
        }

        // 按照不同平台要求修改图片
        for (int i = 0; i < recentSetting.textureImporterFormat.Count; i++)
        {
            ApplyTextureSettings(importer, recentSetting.textureImporterFormat[i].targetPlatform, 
                recentSetting.textureImporterFormat[i].format, recentSetting.textureImporterFormat[i].maxSize);
        }

        // 标记已修改 & 重新导入
        EditorUtility.SetDirty(importer);
        AssetDatabase.WriteImportSettingsIfDirty(path);
        AssetDatabase.ImportAsset(path);
        AssetDatabase.Refresh();

        return true;
    }

    /// <summary>
    /// 应用不同平台的图片格式更改
    /// </summary>
    /// <param name="importer"></param>
    /// <param name="platform"></param>
    /// <param name="format"></param>
    /// <param name="maxSize"></param>
    private void ApplyTextureSettings(TextureImporter importer, string platform, TextureImporterFormat format, int maxSize)
    {
        importer.ClearPlatformTextureSettings(platform); // 先清除旧设置

        TextureImporterPlatformSettings settings = new TextureImporterPlatformSettings
        {
            name = platform,
            overridden = true, // 强制覆盖
            format = format,
            maxTextureSize = maxSize
            
        };

        importer.SetPlatformTextureSettings(settings);
    }


    #endregion

    #region 打印台信息相关
    private void OnExportLogBtnClicked()
    {
        // 1. 确保 DebugLog 文件夹存在
        string folderPath = recentSetting.logExportPath;
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh(); // 刷新 Unity 资源管理器
        }

        // 2. 生成文件名（Debug_当前时间.xml）
        string fileName = $"Debug_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.xml";
        string filePath = Path.Combine(folderPath, fileName);

        // 3. 创建 XML 文档
        XmlDocument xmlDoc = new XmlDocument();

        // 创建 XML 根节点
        XmlElement rootElement = xmlDoc.CreateElement("DebugLog");
        xmlDoc.AppendChild(rootElement);

        // 4. 遍历 List<string> 并写入 XML
        foreach (string log in logMessages)
        {
            XmlElement logElement = xmlDoc.CreateElement("LogEntry");
            logElement.InnerText = log;
            rootElement.AppendChild(logElement);
        }

        // 5. 保存 XML 文件
        xmlDoc.Save(filePath);

        // 6. 刷新 Unity 资源管理器
        AssetDatabase.Refresh();
    }
    #endregion

    #region 读取检查设置相关
    /// <summary>
    /// 找到默认的本地SO文件
    /// </summary>
    private void FindLocalSOFile()
    {
        // 尝试加载现有的 ScriptableObject
        AssetCheckerSetting_SO settings = AssetDatabase.LoadAssetAtPath<AssetCheckerSetting_SO>(default_SO_Path);

        if (settings == null)
        {
            // 找不到则创建一个新的 ScriptableObject
            settings = ScriptableObject.CreateInstance<AssetCheckerSetting_SO>();

            // 确保目录存在
            string directory = Path.GetDirectoryName(default_SO_Path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 创建并保存 ScriptableObject 资产
            AssetDatabase.CreateAsset(settings, default_SO_Path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        else
        {
            logMessages.Add("成功加载本地SO配置文件");
        }
        assetCheckerSetting_SO = settings;
    }

    /// <summary>
    /// 导入Json设置
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void OnImportSettingBtnClicked()
    {
        // 让用户选取一个Json文件
        string path = EditorUtility.OpenFilePanel("选择 JSON 文件", "", "json");
        // 如果用户没有选择文件，path 会是空字符串
        if (!string.IsNullOrEmpty(path))
        {
            Debug.Log("用户选择的 JSON 文件路径：" + path);
        }
        else
        {
            Debug.LogWarning("未选择 JSON 文件");
        }
        if(AssetCheckerSettingsMgr.LoadSettings(path) == null)
        {
            logMessages.Add("导入失败，文件为空");
            return;
        }

        recentSetting = AssetCheckerSettingsMgr.LoadSettings(path);
        // 开始按照检查类型检测
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
