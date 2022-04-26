using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;


public class ShaderGraphSettings : EditorWindow
{
    private const string NodeAssetPath = "Assets/Settings/Node Controls.inputactions";

    private const string HotKeyAssembly =
        "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"NKStudio.ShaderGraph.HotKey\")]";

    private const string ShaderGraphPackageName = "com.unity.shadergraph";

    private const string HotKeyDefine = "SHADER_GRAPH_HOTKEY";

    private const string KShowOnStartupPreference = "NKStudio.ShaderGraph.HotKey";

    private static bool ShowOnStartup
    {
        get => EditorPrefs.GetBool(KShowOnStartupPreference, true);
        set
        {
            if (value != ShowOnStartup) EditorPrefs.SetBool(KShowOnStartupPreference, value);
        }
    }

    [MenuItem("Tools/ShaderGraphSettings")]
    public static void Title()
    {
        ShaderGraphSettings wnd = GetWindow<ShaderGraphSettings>();
        wnd.titleContent = new GUIContent("ShaderGraphSettings");
    }

    [InitializeOnLoadMethod]
    private static void Init()
    {
        if (ShowOnStartup)
            EditorApplication.update += ShowAtStartup;
    }

    static void ShowAtStartup()
    {
        if (!Application.isPlaying)
            Title();

        EditorApplication.update -= ShowAtStartup;
    }

    private void OnDestroy()
    {
        EditorApplication.update -= ShowAtStartup;
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Import UXML
        VisualTreeAsset visualTree =
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/ShaderGraphHotKey/Editor/Settings/ShaderGraphSettings.uxml");
        VisualElement container = visualTree.Instantiate();
        root.Add(container);

        Button create_btn = root.Q<Button>("create-btn");
        Button change_btn = root.Q<Button>("unity-change-btn");
        Button change_btn2 = root.Q<Button>("unity-change2-btn");
        Button add_Define_btn = root.Q<Button>("unity-add-define");
        Button apply_btn = root.Q<Button>("unity-apply-btn");
        ObjectField inputActionField = root.Q<ObjectField>("inputAction-field");
        inputActionField.objectType = typeof(InputActionAsset);
        inputActionField.allowSceneObjects = false;

        create_btn.RegisterCallback<MouseUpEvent>(evt =>
        {
            //파일이 있어야되는 경로
            const string path = "Assets/Settings/Node Controls.inputactions";
            const string kDefaultAssetLayout = "{}";

            //파일을 가지고 있는지 체크
            bool hasSettingsFile = File.Exists(path);

            //가지고 있다면 에러 표시
            if (hasSettingsFile)
            {
                Debug.LogWarning("이미 Settings 폴더에 Node Controls파일이 있습니다.");
                return;
            }

            //파일 생성
            TextAsset json = new TextAsset(kDefaultAssetLayout);
            File.WriteAllText(path, json.text);
            AssetDatabase.Refresh();

            //가져오기
            InputActionAsset inputActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            CreateAllNode(inputActionAsset, path);
            AssetDatabase.Refresh();
            inputActionField.value = inputActionAsset;
            Debug.Log("생성되었습니다.");
        });
        change_btn.RegisterCallback<MouseUpEvent>(ChangeCallback);
        change_btn2.RegisterCallback<MouseUpEvent>(ChangeCallback2);
        add_Define_btn.RegisterCallback<MouseUpEvent>(AddDefineCallback);
        apply_btn.RegisterCallback<MouseUpEvent>(ApplyCallback);
        inputActionField.RegisterValueChangedCallback(Callback);
    }

    private void ChangeCallback2(MouseUpEvent evt)
    {
        DirectoryInfo shaderGraphPackage = GetPackageInstalled(ShaderGraphPackageName);
        AddHotKeyHintToNode(shaderGraphPackage);
        AssetDatabase.Refresh();
    }

    //인풋에셋에 노드를 세팅합니다.
    private static void CreateAllNode(InputActionAsset inputActionAsset, string path)
    {
#if SHADER_GRAPH_HOTKEY
        IEnumerable<Type> types = Assembly.GetAssembly(typeof(AbstractMaterialNode)).GetTypes()
            .Where(t => t.IsSubclassOf(typeof(AbstractMaterialNode)));

        foreach (Type type in types)
        {
            Attribute attribute = Attribute.GetCustomAttribute(type, typeof(TitleAttribute));
            TitleAttribute titleAttribute = (TitleAttribute) attribute;

            string category;

            if (titleAttribute != null)
                category = titleAttribute.title[0];
            else
                continue;

            if (inputActionAsset.FindActionMap(category) == null)
                inputActionAsset.AddActionMap(category);

            string keyboardPath = DefaultSetKeyPath(type);

            inputActionAsset.FindActionMap(category).AddAction(name: type.Name, type: InputActionType.Button)
                .AddBinding(path: keyboardPath, groups: "Keyboard");
        }

        string data = inputActionAsset.ToJson();
        File.WriteAllText(path, data);
#endif
    }

    private static string DefaultSetKeyPath(Type nodeType)
    {
#if SHADER_GRAPH_HOTKEY
        string keyboardPath = string.Empty;
        const string kDefaultKey = "<Keyboard>";

        switch (nodeType)
        {
            case var type when type == typeof(IntegerNode):
                keyboardPath = $"{kDefaultKey}/0";
                break;
            case var type when type == typeof(Vector1Node):
                keyboardPath = $"{kDefaultKey}/1";
                break;
            case var type when type == typeof(Vector2Node):
                keyboardPath = $"{kDefaultKey}/2";
                break;
            case var type when type == typeof(Vector3Node):
                keyboardPath = $"{kDefaultKey}/3";
                break;
            case var type when type == typeof(Vector4Node):
                keyboardPath = $"{kDefaultKey}/4";
                break;
            case var type when type == typeof(ColorNode):
                keyboardPath = $"{kDefaultKey}/5";
                break;
            case var type when type == typeof(SampleTexture2DNode):
                keyboardPath = $"{kDefaultKey}/t";
                break;
            case var type when type == typeof(UVNode):
                keyboardPath = $"{kDefaultKey}/u";
                break;
            case var type when type == typeof(AddNode):
                keyboardPath = $"{kDefaultKey}/a";
                break;
            case var type when type == typeof(SubtractNode):
                keyboardPath = $"{kDefaultKey}/s";
                break;
            case var type when type == typeof(MultiplyNode):
                keyboardPath = $"{kDefaultKey}/m";
                break;
            case var type when type == typeof(DivideNode):
                keyboardPath = $"{kDefaultKey}/d";
                break;
            case var type when type == typeof(OneMinusNode):
                keyboardPath = $"{kDefaultKey}/o";
                break;
            case var type when type == typeof(PowerNode):
                keyboardPath = $"{kDefaultKey}/e";
                break;
            case var type when type == typeof(LerpNode):
                keyboardPath = $"{kDefaultKey}/l";
                break;
            case var type when type == typeof(SplitNode):
                keyboardPath = $"{kDefaultKey}/b";
                break;
            case var type when type == typeof(SwizzleNode):
                keyboardPath = $"{kDefaultKey}/z";
                break;
            case var type when type == typeof(NormalizeNode):
                keyboardPath = $"{kDefaultKey}/n";
                break;
            case var type when type == typeof(DotProductNode):
                keyboardPath = $"{kDefaultKey}/.";
                break;
        }
        return keyboardPath;
#else
        return "";
#endif
    }

    private void Callback(ChangeEvent<Object> evt)
    {
    }

    private void ChangeCallback(MouseUpEvent evt)
    {
        DirectoryInfo shaderGraphPackage = GetPackageInstalled(ShaderGraphPackageName);

        string filePath = $"{shaderGraphPackage.FullName}/Editor/Drawing/Views/GraphEditorView.cs";

        FileInfo file = new(filePath);

        Assert.IsNotNull(file);

        //전체 코드를 가져옵니다.
        string text = File.ReadAllText(filePath);
        text = text.Replace("namespace UnityEditor.ShaderGraph.Drawing",
                $"{HotKeyAssembly}\n\nnamespace UnityEditor.ShaderGraph.Drawing")
            .Replace("MaterialGraphView m_GraphView;",
                "public static Action<MaterialGraphView, GraphData> keyboardCallback;\n\t\t\t\tMaterialGraphView m_GraphView;")
            .Replace("m_InspectorView.InitializeGraphSettings();",
                "m_InspectorView.InitializeGraphSettings();\n\t\t\t\tkeyboardCallback?.Invoke(graphView, graph);");

        File.WriteAllText(filePath, text);
    }

    private static DirectoryInfo GetPackageInstalled(string packageName)
    {
        //폴더 안의 정보를 가져옵니다.
        DirectoryInfo packageCache = new("Library/PackageCache");

        //찾고자 하는 패키지를 찾습니다.
        DirectoryInfo result = packageCache.GetDirectories()
            .FirstOrDefault(package => package.Name.Contains(packageName));

        return result;
    }

    private static void AddHotKeyHintToNode(DirectoryInfo shaderGraphPackage)
    {
        //키보드 패스
        const string kDefaultKey = "<Keyboard>/";
        
        //인풋 액션파일이 있는지 찾습니다.
        InputActionAsset inputActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(NodeAssetPath);

        //단축키가 있는 노드를 모아놓은 곳입니다.
        var hasHotKeyNodes = new Dictionary<string, string>();

        //단축키가 있는 것을 체크합니다.
        foreach (InputActionMap actionMap in inputActionAsset.actionMaps)
        foreach (InputAction action in actionMap.actions)
        foreach (InputBinding binding in action.bindings.Where(binding => binding.path.Length != 0))
            hasHotKeyNodes.Add(action.name, binding.path);

        //단축키가 없으면 여기서 스톱
        if (hasHotKeyNodes.Count == 0) return;

        //단축키가 있는 노드들을 모두 순례를 돕니다.
        foreach (var hasHotKeyNode in hasHotKeyNodes)
        {
            //노드 이름으로 CS파일의 GUID를 찾습니다.
            string[] guidByNodes = AssetDatabase.FindAssets(hasHotKeyNode.Key,
                new[] {"Packages/com.unity.shadergraph/Editor/Data/Nodes"});

            //노드 파일의 경로를 가져옵니다.

            string nodePath = string.Empty;

            //찾은 CS파일을 한번 필터링 합니다.
            foreach (string guidByNode in guidByNodes)
            {
                string realPath = AssetDatabase.GUIDToAssetPath(guidByNode);

                int startSplit = realPath.LastIndexOf("/", StringComparison.Ordinal);
                string formatText = realPath[startSplit..].Replace("/", "").Replace(".cs", "");
                if (!formatText.Equals(hasHotKeyNode.Key)) continue;
                nodePath = realPath;
                break;
            }

            Assert.IsTrue(nodePath != string.Empty,"304번 코드에 문제가 발생했습니다.");
            
            //단축키를 표시합니다.
            string hotKey = hasHotKeyNode.Value.Replace(kDefaultKey, "").ToUpper();

            //전체 코드를 가져옵니다.
            string text = File.ReadAllText(nodePath);

            //코드를 라인별로 자릅니다.
            string[] codeLines = text.Split('\r', '\n');

            //라인별로 순례를 돕니다.
            foreach (string codeLine in codeLines)
            {
                //해당 라인에 타이틀이 없으면 무시합니다.
                if (!codeLine.Contains("[Title(")) continue;

                int startIndex = codeLine.IndexOf("\")]", StringComparison.Ordinal);
                string afterTitleLine = codeLine.Insert(startIndex, $" ({hotKey})");
                text = text.Replace(codeLine, afterTitleLine);
                break;
            }
            File.WriteAllText(nodePath, text);
        }
    }

    private void AddDefineCallback(MouseUpEvent evt)
    {
        string defines =
            PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

        string addHotKeyDefineToCurrentDefine = string.Concat(defines, ";", HotKeyDefine);

        if (!defines.Contains(HotKeyDefine))
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                addHotKeyDefineToCurrentDefine);
    }

    private void ApplyCallback(MouseUpEvent evt)
    {
        // AssetDatabase.Refresh();
        // Debug.Log("적용 완료");
    }
}