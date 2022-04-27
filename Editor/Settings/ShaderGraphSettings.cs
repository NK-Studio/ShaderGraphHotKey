using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;


namespace ShaderGraphShotKey.Editor.Settings
{
    public class ShaderGraphSettings : EditorWindow
    {
        private const string NodeAssetPath = "Assets/Settings/Node Controls.inputactions";

        private const string HotKeyAssembly =
            "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"NKStudio.ShaderGraph.HotKey\")]";

        private const string ShaderGraphPackageName = "com.unity.shadergraph";

        private const string HotKeyDefine = "SHADER_GRAPH_HOTKEY";

        private ShaderGraphHotKeySettings settings;

        private InputActionAsset _inputActionAsset;

        [MenuItem("Tools/ShaderGraphSettings")]
        public static void Title()
        {
            ShaderGraphSettings wnd = GetWindow<ShaderGraphSettings>();
            wnd.titleContent = new GUIContent("ShaderGraphSettings");
            wnd.minSize = new Vector2(290, 320);
            wnd.maxSize = new Vector2(380, 320);
        }

        [InitializeOnLoadMethod]
        private static void Init()
        {
            int settingsId = EditorPrefs.GetInt("SGHKSettingsID", -1);

            //세팅 파일이 없는 경우 기본만 띄웁니다.
            if (settingsId == -1)
                EditorApplication.update += ShowAtStartup;
            else
            {
                //기존에 있는 세팅 데이터를 기반으로 오픈합니다.
                string path = AssetDatabase.GetAssetPath(settingsId);
                
                ShaderGraphHotKeySettings temporarySettings =
                    AssetDatabase.LoadAssetAtPath<ShaderGraphHotKeySettings>(path);

                //없는 상태이다.
                if (temporarySettings == null)
                {
                    EditorApplication.update += ShowAtStartup;
                    EditorPrefs.SetInt("SGHKSettingsID", -1);
                    return;
                }
                
                bool showOnStartup = temporarySettings.StartAtShow == ShaderGraphHotKeySettings.KStartUp.Always;

                if (showOnStartup)
                    EditorApplication.update += ShowAtStartup;

                //오버라이드 시킵니다.
                if (temporarySettings.AutoShaderGraphOverride)
                    OverridePackage();
            }
        }

        private static void ShowAtStartup()
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
            #region Init

            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Import UXML
            VisualTreeAsset visualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/ShaderGraphHotKey/Editor/Settings/ShaderGraphSettings.uxml");
            VisualElement container = visualTree.Instantiate();
            root.Add(container);

            #endregion

            #region Button

            Button addSettingsBtn = root.Q<Button>("add-settings-btn");
            Button createInputActionBtn = root.Q<Button>("create-inputAction-btn");
            Button createSettingsBtn = root.Q<Button>("create-settings-btn");
            Button patchBtn = root.Q<Button>("override-btn");
            Button addInputActionBtn = root.Q<Button>("add-inputAction");
            Button addDefineBtn = root.Q<Button>("add-define");

            #endregion

            #region inputActionField

            ObjectField inputActionField = root.Q<ObjectField>("inputAction-field");
            inputActionField.objectType = typeof(InputActionAsset);
            inputActionField.allowSceneObjects = false;

            #endregion

            #region settingsField

            ObjectField settingsField = root.Q<ObjectField>("settings-field");
            settingsField.objectType = typeof(ShaderGraphHotKeySettings);
            settingsField.allowSceneObjects = false;

            #endregion

            #region DropdownField

            EnumField languageField = root.Q<EnumField>("language-field");
            DropdownField startAtShowField = root.Q<DropdownField>("startAtShow-field");

            #endregion

            #region Toggle

            Toggle autoOverride = root.Q<Toggle>("auto-override");

            #endregion

            //초기 셋업을 합니다.
            InitSetUp();

            //InputAction 생성 버튼
            createInputActionBtn.RegisterCallback<MouseUpEvent>(_ => { CreateInputAction(); });

            //Settings파일 생성 버튼
            addSettingsBtn.RegisterCallback<MouseUpEvent>(_ => { InstallSettings(); });
            createSettingsBtn.RegisterCallback<MouseUpEvent>(_ => { InstallSettings(); });

            //세팅 필드 변화 체크
            settingsField.RegisterValueChangedCallback(evt =>
            {
                if (settingsField.value == null)
                {
                    Reset();
                    return;
                }

                //변경된 값 적용
                settings = (ShaderGraphHotKeySettings) evt.newValue;
                settingsField.value = settings;

                //세팅
                SetUp(settings);
            });

            //인풋 액션 필드 변화 체크
            inputActionField.RegisterValueChangedCallback(evt =>
            {
                //변경된 값 적용
                if (evt.newValue == null)
                    _inputActionAsset = null;
                else
                    _inputActionAsset = (InputActionAsset) evt.newValue;
            });

            patchBtn.RegisterCallback<MouseUpEvent>(_ =>
            {
                if (settingsField.value == null || settings == null) return;
                settings.AutoShaderGraphOverride = true;
                autoOverride.value = true;
                OverridePackage();

                AddHotKeyHintToNode();
            });

            addInputActionBtn.RegisterCallback<MouseUpEvent>(_ =>
            {
                //인풋 액션 설치
                CreateInputAction();
                
                //새로고침
                AssetDatabase.Refresh();
            });
            
            addDefineBtn.RegisterCallback<MouseUpEvent>(_ =>
            {
                //디파인 설치
                InstallDefine();

                //새로고침
                AssetDatabase.Refresh();
            });

            languageField.RegisterValueChangedCallback(_ =>
            {
                if (settingsField.value == null || settings == null) return;
                settings.Language = (ShaderGraphHotKeySettings.KLanguage) languageField.value;

                //버튼 설정
                switch (settings.Language)
                {
                    case ShaderGraphHotKeySettings.KLanguage.English:
                        createSettingsBtn.text = "Create";
                        createInputActionBtn.text = "Create";
                        autoOverride.label = "Auto Shader Graph Override";
                        break;
                    case ShaderGraphHotKeySettings.KLanguage.한국어:
                        createSettingsBtn.text = "생성";
                        createInputActionBtn.text = "생성";
                        autoOverride.label = "자동 쉐이더 그래프 덮어쓰기";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                //StartAtShow 설정
                startAtShowField.index = -1; //초기화

                //언어 체인지
                startAtShowField.choices = settings.Language == ShaderGraphHotKeySettings.KLanguage.English
                    ? settings.StartAtShowText[ShaderGraphHotKeySettings.KLanguage.English]
                    : settings.StartAtShowText[ShaderGraphHotKeySettings.KLanguage.한국어];

                startAtShowField.index = (int) settings.StartAtShow; //다시 설정
            });

            startAtShowField.RegisterValueChangedCallback(_ =>
            {
                if (settingsField.value == null || settings == null) return;
                settings.StartAtShow = (ShaderGraphHotKeySettings.KStartUp) startAtShowField.index;
            });

            autoOverride.RegisterValueChangedCallback(evt =>
            {
                //체크 안되도록 막음
                if (settingsField.value == null || settings == null)
                {
                    autoOverride.value = false;
                    return;
                }

                settings.AutoShaderGraphOverride = evt.newValue;
            });

            void InitSetUp()
            {
                int settingsid = EditorPrefs.GetInt("SGHKSettingsID", -1);
                int Actionid = EditorPrefs.GetInt("SGHKInputActionID", -1);

                //세팅 파일이 없는 경우 기본만 띄웁니다.
                if (settingsid == -1)
                {
                    languageField.Init(ShaderGraphHotKeySettings.KLanguage.English);
                    startAtShowField.value = ShaderGraphHotKeySettings.KStartUp.Always.ToString();
                    autoOverride.value = false;
                }
                else
                {
                    //기존에 있던 것을 반영합니다.
                    string path = AssetDatabase.GetAssetPath(settingsid);
                    if (path.Equals(string.Empty))
                    {
                        EditorPrefs.SetInt("SGHKSettingsID", -1);
                        return;
                    }

                    settingsField.value = AssetDatabase.LoadAssetAtPath<ShaderGraphHotKeySettings>(path);
                    settings = (ShaderGraphHotKeySettings) settingsField.value;

                    languageField.Init(settings.Language);

                    if (settings.Language == ShaderGraphHotKeySettings.KLanguage.English)
                        startAtShowField.choices =
                            settings.StartAtShowText[ShaderGraphHotKeySettings.KLanguage.English];
                    else
                        startAtShowField.choices = settings.StartAtShowText[ShaderGraphHotKeySettings.KLanguage.한국어];

                    startAtShowField.index = (int) settings.StartAtShow;

                    autoOverride.value = settings.AutoShaderGraphOverride;

                    switch (settings.Language)
                    {
                        case ShaderGraphHotKeySettings.KLanguage.English:
                            createSettingsBtn.text = "Create";
                            createInputActionBtn.text = "Create";
                            break;
                        case ShaderGraphHotKeySettings.KLanguage.한국어:
                            createSettingsBtn.text = "생성";
                            createInputActionBtn.text = "생성";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                if (Actionid != -1)
                {
                    //기존에 있던 것을 반영합니다.
                    string Actionpath = AssetDatabase.GetAssetPath(Actionid);

                    if (Actionpath.Equals(string.Empty))
                    {
                        EditorPrefs.SetInt("SGHKInputActionID", -1);
                        return;
                    }

                    inputActionField.value = AssetDatabase.LoadAssetAtPath<InputActionAsset>(Actionpath);
                    _inputActionAsset = (InputActionAsset) inputActionField.value;
                }
            }

            void SetUp(ShaderGraphHotKeySettings settings)
            {
                //다시 저장
                EditorPrefs.SetInt("SGHKSettingsID", settings.GetInstanceID());

                #region LanguageField

                languageField.Init(settings.Language);

                #endregion

                #region StartAtShowField

                //초기화
                startAtShowField.index = -1;

                if (settings.Language == ShaderGraphHotKeySettings.KLanguage.English)
                    startAtShowField.choices = settings.StartAtShowText[ShaderGraphHotKeySettings.KLanguage.English];
                else
                    startAtShowField.choices = settings.StartAtShowText[ShaderGraphHotKeySettings.KLanguage.한국어];

                startAtShowField.index = (int) settings.StartAtShow;

                #endregion

                #region AutoOverride

                autoOverride.value = settings.AutoShaderGraphOverride;

                #endregion
            }

            void Reset()
            {
                //모두 초기화
                languageField.Init(ShaderGraphHotKeySettings.KLanguage.English);
                startAtShowField.choices = null;
                startAtShowField.value = ShaderGraphHotKeySettings.KStartUp.Always.ToString();
                autoOverride.value = false;

                EditorPrefs.SetInt("SGHKSettingsID", -1);
            }

            void InstallSettings()
            {
                const string path = "Assets/Settings/ShaderGraphHotKeySettingsAsset.asset";

                if (File.Exists(path))
                {
                    Debug.Log("이미 파일이 있습니다.");
                    return;
                }

                //파일을 가지고 있는지 체크
                bool hasSettingsFile = File.Exists(path);

                //가지고 있다면 에러 표시
                if (hasSettingsFile)
                {
                    if (settingsField.value == null || settings == null) return;
                    string warningMsg = settings.Language == ShaderGraphHotKeySettings.KLanguage.English
                        ? "There is already a file in the Settings folder."
                        : "이미 Settings 폴더에 파일이 있습니다.";
                    Debug.LogWarning(warningMsg);

                    return;
                }

                //Settings 폴더가 있는지 체크
                bool hasSettingsDir = Directory.Exists($"{Application.dataPath}/Settings");

                //없으면 생성
                if (!hasSettingsDir)
                    Directory.CreateDirectory($"{Application.dataPath}/Settings");

                //스크립터블 생성
                settings = CreateInstance<ShaderGraphHotKeySettings>();
                settings.Language = ShaderGraphHotKeySettings.KLanguage.English; //영어로 설정
                settings.StartAtShow = ShaderGraphHotKeySettings.KStartUp.Always; //컴파일 후 계속 켜짐 설정
                settings.AutoShaderGraphOverride = false; //초기에는 해제
                AssetDatabase.CreateAsset(settings, path);
                settingsField.value = settings;

                //파일 위치 저장
                int id = settings.GetInstanceID();
                EditorPrefs.SetInt("SGHKSettingsID", id);
                Debug.Log("생성되었습니다.");
            }

            void CreateInputAction()
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
                _inputActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);

                //스킴 추가
                var scheme = _inputActionAsset.FindControlScheme("Keyboard");
                if (scheme == null) _inputActionAsset.AddControlScheme("Keyboard").WithOptionalDevice<Keyboard>();

                //노드 추가
                CreateAllNode(path);

                //파일 위치 저장
                int id = _inputActionAsset.GetInstanceID();
                EditorPrefs.SetInt("SGHKInputActionID", id);

                //바인딩
                inputActionField.value = _inputActionAsset;

                //새로고침
                AssetDatabase.Refresh();

                Debug.Log("생성되었습니다.");
            }

            void InstallDefine()
            {
                string defines =
                    PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

                string addHotKeyDefineToCurrentDefine = string.Concat(defines, ";", HotKeyDefine);

                if (!defines.Contains(HotKeyDefine))
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                        addHotKeyDefineToCurrentDefine);
            }
        }

        //패키지를 오버라이드 시킵니다.
        private static void OverridePackage()
        {
            DirectoryInfo shaderGraphPackage = GetPackageInstalled(ShaderGraphPackageName);

            string filePath = $"{shaderGraphPackage.FullName}/Editor/Drawing/Views/GraphEditorView.cs";

            FileInfo file = new FileInfo(filePath);

            Assert.IsNotNull(file);

            //전체 코드를 가져옵니다.
            string text = File.ReadAllText(filePath);

            if (text.Contains(HotKeyAssembly))
            {
                Debug.Log("세팅이 완료되었습니다.");
                return;
            }

            text = text.Replace("namespace UnityEditor.ShaderGraph.Drawing",
                    $"{HotKeyAssembly}\n\nnamespace UnityEditor.ShaderGraph.Drawing")
                .Replace("MaterialGraphView m_GraphView;",
                    "public static Action<MaterialGraphView, GraphData> keyboardCallback;\n\t\t\t\tMaterialGraphView m_GraphView;")
                .Replace("m_InspectorView.InitializeGraphSettings();",
                    "m_InspectorView.InitializeGraphSettings();\n\t\t\t\tkeyboardCallback?.Invoke(graphView, graph);");

            File.WriteAllText(filePath, text);
        }

        //인풋에셋에 노드를 세팅합니다.
        private void CreateAllNode(string path)
        {
#if SHADER_GRAPH_HOTKEY
            //AbstractMaterialNode을 상속받는 클래스 타입 모두 가져오기
            var types = Assembly.GetAssembly(typeof(AbstractMaterialNode)).GetTypes()
                .Where(t => t.IsSubclassOf(typeof(AbstractMaterialNode)));

            //타입별로 전부 순례한다.
            foreach (Type type in types)
            {
                //해당 타입들의 타이틀 어트리뷰트를 체크
                Attribute attribute = Attribute.GetCustomAttribute(type, typeof(TitleAttribute));
                TitleAttribute titleAttribute = (TitleAttribute) attribute;

                string category;

                if (titleAttribute != null)
                    category = titleAttribute.title[0];
                else
                    continue;

                //인풋 액션 에셋에 해당 카테고리 이름으로 액션맵이 없으면 생성
                if (_inputActionAsset.FindActionMap(category) == null)
                    _inputActionAsset.AddActionMap(category);

                //기본 세팅 단축키 적용
                string keyboardPath = DefaultSetKeyPath(type);

                //단축키가 적용된 상태로 바인딩 처리
                _inputActionAsset.FindActionMap(category).AddAction(name: type.Name, type: InputActionType.Button)
                    .AddBinding(path: keyboardPath, groups: "Keyboard");
            }

            //저장
            string data = _inputActionAsset.ToJson();

            File.WriteAllText(path, data);
#endif
        }

        private string DefaultSetKeyPath(Type nodeType)
        {
            string keyboardPath = string.Empty;
#if SHADER_GRAPH_HOTKEY
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
                default:
                    keyboardPath = string.Empty;
                    break;
            }
#endif
            return keyboardPath;
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

        private void AddHotKeyHintToNode()
        {
            const string kDefaultKey = "<Keyboard>/";
            InputActionAsset inputActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(NodeAssetPath);

            Dictionary<string, string> hasHotKeyNodes = new Dictionary<string, string>();

            foreach (InputActionMap actionMap in inputActionAsset.actionMaps)
            foreach (InputAction action in actionMap.actions)
            foreach (InputBinding binding in action.bindings.Where(binding => binding.path.Length != 0))
                hasHotKeyNodes.Add(action.name, binding.path);

            if (hasHotKeyNodes.Count == 0) return;


            foreach (KeyValuePair<string, string> hasHotKeyNode in hasHotKeyNodes)
            {
                string[] guidByNode = AssetDatabase.FindAssets(hasHotKeyNode.Key,
                    new[] {"Packages/com.unity.shadergraph/Editor/Data/Nodes"});

                string nodePath = AssetDatabase.GUIDToAssetPath(guidByNode[0]);

                FileInfo file = new(nodePath);

                Assert.IsTrue(file.Exists, $"{hasHotKeyNode.Key}.cs이 존재하지 않습니다.");

                //전체 코드를 가져옵니다.
                string text = File.ReadAllText(nodePath);

                string[] codeLines = text.Split('\r', '\n');

                string hotKey = hasHotKeyNode.Value.Replace(kDefaultKey, "");

                foreach (string codeLine in codeLines)
                {
                    if (!codeLine.Contains("[Title(")) continue;
                    int startIndex = codeLine.IndexOf("\")]", StringComparison.Ordinal);
                    string afterTitleLine = codeLine.Insert(startIndex, $" ({hotKey.ToUpper()})");
                    text = text.Replace(codeLine, afterTitleLine);
                    break;
                }

                File.WriteAllText(nodePath, text);
            }

            Debug.Log("단축키 힌트 적용 완료");
        }
    }
}