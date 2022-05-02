using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

#if ENABLE_SHADERGRAPH
using UnityEditor.ShaderGraph;
#endif

namespace NKStudio.ShaderGraph.HotKey
{
#if !ENABLE_INPUT_SYSTEM
    public class InputActionAsset : ScriptableObject
    {
    }
#endif

    public class ShaderGraphSettings : EditorWindow
    {
        private const string NodeAssetPath = "Assets/Settings/Node Controls.inputactions";

        private const string UxmlPath = "Assets/ShaderGraphHotKey/Editor/Settings/Window/ShaderGraphSettings.uxml";

        private const string HotKeyAssembly =
            "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"NKStudio.ShaderGraph.HotKey\")]";

        private const string ShaderGraphPackageName = "com.unity.shadergraph";

        private const string InputSystemPackageName = "com.unity.inputsystem";

        private const string HotKeyDefine = "SHADER_GRAPH_HOTKEY";

        private ShaderGraphHotKeySettings _hotKeySettings;

        private static AddRequest _addRequest;

#if ENABLE_INPUT_SYSTEM
        private InputActionAsset _inputActionAsset;
#endif

        [MenuItem("Window/ShaderGraph HotKey/ShaderGraphSettings")]
        public static void Title()
        {
            ShaderGraphSettings wnd = GetWindow<ShaderGraphSettings>();
            wnd.titleContent = new GUIContent("ShaderGraphSettings");
            wnd.minSize = new Vector2(434, 600);
            wnd.maxSize = new Vector2(560, 700);
        }


        [InitializeOnLoadMethod]
        private static void Init()
        {
#if ENABLE_SHADERGRAPH && ENABLE_INPUT_SYSTEM
            //세팅 파일의 GUID를 가져옵니다.
            int settingsId = EditorPrefs.GetInt("SGHKSettingsID", -1);

            //세팅 파일이 없는 경우 디폴트로 띄웁니다.
            if (settingsId == -1)
                EditorApplication.update += ShowAtStartup;
            else
            {
                //파일의 경로를 가져옵니다.
                string path = AssetDatabase.GetAssetPath(settingsId);

                //에셋을 로드합니다.
                ShaderGraphHotKeySettings temporarySettings =
                    AssetDatabase.LoadAssetAtPath<ShaderGraphHotKeySettings>(path);

                //로드에 실패하면 그냥 띄웁니다.
                if (temporarySettings == null)
                {
                    EditorApplication.update += ShowAtStartup;
                    EditorPrefs.SetInt("SGHKSettingsID", -1);
                    return;
                }

                //temporarySettings.StartAtShow가 Always이면 true, 아니면 false를 반환합니다.
                bool showOnStartup = temporarySettings.StartAtShow == ShaderGraphHotKeySettings.KStartUp.Always;

                //Always이면 표시
                if (showOnStartup)
                    EditorApplication.update += ShowAtStartup;
            }

            Application.logMessageReceived += ApplicationOnlogMessageReceived;
            AssemblyReloadEvents.afterAssemblyReload += EndCompile;
#else
            EditorApplication.update += ShowAtStartup;
#endif
        }


        private static void ShowAtStartup()
        {
            //유니티가 플레이모드가 아니라면 타이틀 표시
            if (!Application.isPlaying)
                Title();

            //해제
            EditorApplication.update -= ShowAtStartup;
        }

        private void OnDestroy()
        {
            //제거되면 해제
            EditorApplication.update -= ShowAtStartup;
        }

#if SHADER_GRAPH_HOTKEY
        void OnDisable()
        {
            Application.logMessageReceived -= ApplicationOnlogMessageReceived;
            AssemblyReloadEvents.afterAssemblyReload -= EndCompile;
        }

        private static void ApplicationOnlogMessageReceived(string condition, string stacktrace, LogType type)
        {
            //에러가 표시되었다면,
            if (type == LogType.Error)
            {
                //그 에러가 쉐이더 그래프에 대한 GraphData에러라면, 코드 패치를 한다.
                if (condition.Contains("GraphData"))
                {
                    OverridePackage();
                }
            }
        }
#endif
        public void CreateGUI()
        {
            #region Init

            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Import UXML
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            VisualElement container = visualTree.Instantiate();
            root.Add(container);

            #endregion

            #region Button

            Button changeBothBtn = root.Q<Button>("change-both-btn");
            Button installInputSystemBtn = root.Q<Button>("Install-InputSystem-btn");
            Button addSettingsBtn = root.Q<Button>("add-settings-btn");
            Button createInputActionBtn = root.Q<Button>("create-inputAction-btn");
            Button createSettingsBtn = root.Q<Button>("create-settings-btn");
            Button patchCodeBtn = root.Q<Button>("patch-Code");
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

            //초기화
            InitSetUp();


            installInputSystemBtn.RegisterCallback<MouseUpEvent>(evt =>
            {
#if !ENABLE_INPUT_SYSTEM

                bool AllowInstallInputSystem = EditorUtility.DisplayDialog("InputSystem Install", "인풋 시스템을 설치하겠습니까?", "네", "아니오");

                if (AllowInstallInputSystem)
                {
                    _addRequest = Client.Add(InputSystemPackageName);
                    EditorApplication.update += Progress;    
                }
#else
                Debug.Log("이미 설치되어 있습니다.");
#endif
            });

            //Active Handling을 Both로 변경합니다.
            changeBothBtn.RegisterCallback<MouseUpEvent>(evt => { ChangeInputSystemBoth(); });

#if ENABLE_INPUT_SYSTEM
            //InputAction 생성 버튼
            createInputActionBtn.RegisterCallback<MouseUpEvent>(_ => { CreateInputAction(); });
#endif
#if ENABLE_INPUT_SYSTEM && ENABLE_SHADERGRAPH
            //Settings파일 생성 버튼
            addSettingsBtn.RegisterCallback<MouseUpEvent>(_ => { InstallSettings(); });
            createSettingsBtn.RegisterCallback<MouseUpEvent>(_ => { InstallSettings(); });

            //세팅 필드 변화 체크
            settingsField.RegisterValueChangedCallback(evt =>
            {
                if (settingsField.value == null)
                {
                    settingsField.SetEnabled(true);
                    Reset();
                }
                else
                {
                    settingsField.SetEnabled(false);

                    //변경된 값 적용
                    _hotKeySettings = (ShaderGraphHotKeySettings) evt.newValue;
                    settingsField.value = _hotKeySettings;

                    //세팅
                    SetUp(_hotKeySettings);
                }

                // #region PatchCodeEnable
                //
                // if (settingsField.value == null || _hotKeySettings == null || inputActionField.value == null ||
                //     _inputActionAsset == null)
                //     patchCodeBtn.SetEnabled(false);
                // else
                //     patchCodeBtn.SetEnabled(true);
                //
                // #endregion
            });
#endif
#if ENABLE_INPUT_SYSTEM
            //인풋 액션 필드 변화 체크
            inputActionField.RegisterValueChangedCallback(evt =>
            {
                //변경된 값 적용
                if (evt.newValue == null)
                {
                    patchCodeBtn.SetEnabled(true);
                    _inputActionAsset = null;
                }
                else
                {
                    patchCodeBtn.SetEnabled(false);
                    //값 적용
                    _inputActionAsset = (InputActionAsset) evt.newValue;

                    //다시 저장
                    EditorPrefs.SetInt("SGHKInputActionID", _inputActionAsset.GetInstanceID());
                }


                // #region PatchCodeEnable
                //
                // if (settingsField.value == null || _hotKeySettings == null || inputActionField.value == null ||
                //     _inputActionAsset == null)
                //     patchCodeBtn.SetEnabled(false);
                // else
                //     patchCodeBtn.SetEnabled(true);
                //
                // #endregion
            });
#endif

            patchCodeBtn.RegisterCallback<MouseUpEvent>(_ =>
            {
#if ENABLE_SHADERGRAPH && ENABLE_INPUT_SYSTEM
                if (settingsField.value == null || _hotKeySettings == null || inputActionField.value == null || _inputActionAsset == null) return;

                _hotKeySettings.AutoShaderGraphOverride = true;
                autoOverride.value = true;
                OverridePackage(); //접근할 수 있도록 코드 수정 
#endif
            });

#if ENABLE_INPUT_SYSTEM
            addInputActionBtn.RegisterCallback<MouseUpEvent>(_ =>
            {
                //인풋 액션 설치
                CreateInputAction();

                //새로고침
                AssetDatabase.Refresh();
            });
#endif

#if ENABLE_SHADERGRAPH && ENABLE_INPUT_SYSTEM
            addDefineBtn.RegisterCallback<MouseUpEvent>(_ =>
            {
                //디파인 설치
                InstallDefine();

                //새로고침
                AssetDatabase.Refresh();
            });
#endif
#if ENABLE_SHADERGRAPH
            languageField.RegisterValueChangedCallback(_ =>
            {
                if (settingsField.value == null || _hotKeySettings == null) return;
                _hotKeySettings.Language = (ShaderGraphHotKeySettings.KLanguage) languageField.value;

                //버튼 설정
                switch (_hotKeySettings.Language)
                {
                    case ShaderGraphHotKeySettings.KLanguage.English:
                        createSettingsBtn.text = "Create";
                        createInputActionBtn.text = "Create";
                        autoOverride.label = "Auto Shader Graph Override";
                        addSettingsBtn.text = "Add Settings";
                        //patchCodeBtn.text = "Hot Key Code Patch";
                        addDefineBtn.text = "Add Define";
                        addInputActionBtn.text = "Add (Node)InputAction";
                        //patchHintBtn.text = "Hot Key Hint Patch";
                        break;
                    case ShaderGraphHotKeySettings.KLanguage.한국어:
                        createSettingsBtn.text = "생성";
                        createInputActionBtn.text = "생성";
                        autoOverride.label = "자동 쉐이더 그래프 덮어쓰기";
                        addSettingsBtn.text = "Settings 생성";
                        //patchCodeBtn.text = "단축키 시스템 패치";
                        addDefineBtn.text = "Define 적용";
                        addInputActionBtn.text = "(노드)인풋 액션 생성";
                        //patchHintBtn.text = "단축키 힌트 패치";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                //StartAtShow 설정
                startAtShowField.index = -1; //초기화

                //언어 체인지
                startAtShowField.choices = _hotKeySettings.Language == ShaderGraphHotKeySettings.KLanguage.English
                    ? _hotKeySettings.StartAtShowText[ShaderGraphHotKeySettings.KLanguage.English]
                    : _hotKeySettings.StartAtShowText[ShaderGraphHotKeySettings.KLanguage.한국어];

                startAtShowField.index = (int) _hotKeySettings.StartAtShow; //다시 설정
            });
#endif

            startAtShowField.RegisterValueChangedCallback(_ =>
            {
                if (settingsField.value == null || _hotKeySettings == null) return;
                _hotKeySettings.StartAtShow = (ShaderGraphHotKeySettings.KStartUp) startAtShowField.index;
            });

            autoOverride.RegisterValueChangedCallback(evt =>
            {
                autoOverride.value = evt.newValue;

                //체크 기록함
                if (settingsField.value != null && _hotKeySettings != null)
                    _hotKeySettings.AutoShaderGraphOverride = autoOverride.value;
            });

            void InitSetUp()
            {
#if !ENABLE_INPUT_SYSTEM
                installInputSystemBtn.SetEnabled(true);
                createInputActionBtn.SetEnabled(false);
                addSettingsBtn.SetEnabled(false);
                addInputActionBtn.SetEnabled(false);
                patchCodeBtn.SetEnabled(false);
#else
                installInputSystemBtn.SetEnabled(true);
                createInputActionBtn.SetEnabled(true);
#endif

#if ENABLE_INPUT_SYSTEM
                #region BothEnable

                bool isBoth = EditorPlayerSettingHelpers.oldSystemBackendsEnabled &&
                              EditorPlayerSettingHelpers.newSystemBackendsEnabled;

                changeBothBtn.SetEnabled(!isBoth);

                #endregion
#else
                changeBothBtn.SetEnabled(false);
#endif

#if ENABLE_SHADERGRAPH && ENABLE_INPUT_SYSTEM
                #region DefineEnable

                string defines =
                    PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

                bool hasDefine = defines.Contains(HotKeyDefine);
                
                addDefineBtn.SetEnabled(!hasDefine);

                #endregion
#else
                addDefineBtn.SetEnabled(false);
#endif

                #region GetGUID

                //에셋 파일들의 GUID를 가져옵니다.
                int settingsId = EditorPrefs.GetInt("SGHKSettingsID", -1);
                int actionId = EditorPrefs.GetInt("SGHKInputActionID", -1);

                #endregion

                //세팅 파일이 없는 경우 기본만 띄웁니다.
                if (settingsId == -1)
                {
                    //영어로 설정
                    languageField.Init(ShaderGraphHotKeySettings.KLanguage.English);

                    //항상으로 표시
                    startAtShowField.value = ShaderGraphHotKeySettings.KStartUp.Always.ToString();

                    //오토 패치는 끄기
                    autoOverride.value = false;

#if ENABLE_INPUT_SYSTEM
                    //세팅 파일 추가 버튼 활성화
                    addSettingsBtn.SetEnabled(true);
#endif
                }
                else
                {
                    //실제 경로를 가져옵니다.
                    string path = AssetDatabase.GetAssetPath(settingsId);

                    //가져왔을때 문제가 있을 경우
                    if (string.IsNullOrEmpty(path))
                    {
                        //-1로 초기화
                        EditorPrefs.SetInt("SGHKSettingsID", -1);

#if ENABLE_INPUT_SYSTEM
                    //세팅 파일 추가 버튼 활성화
                    addSettingsBtn.SetEnabled(true);
#endif
                    }
                    else
                    {
                        //실제 파일로 처리한다.
                        ShaderGraphHotKeySettings settings =
                            AssetDatabase.LoadAssetAtPath<ShaderGraphHotKeySettings>(path);

                        //파일 가져오기를 성공했다면,
                        if (settings)
                        {
#if ENABLE_INPUT_SYSTEM
                    //세팅 파일 추가 버튼 활성화
                    addSettingsBtn.SetEnabled(false);
#endif

                            //필드와 세팅스에 참조
                            settingsField.value = settings;
                            _hotKeySettings = settings;

                            //세팅 파일의 언어로 언어필드 세팅
                            languageField.Init(_hotKeySettings.Language);

                            //언어 패키지를 가져온다.
                            var startAtShowLanguage =
                                _hotKeySettings.Language == ShaderGraphHotKeySettings.KLanguage.English
                                    ? _hotKeySettings.StartAtShowText[ShaderGraphHotKeySettings.KLanguage.English]
                                    : _hotKeySettings.StartAtShowText[ShaderGraphHotKeySettings.KLanguage.한국어];

                            //StartAtShow 언어 설정
                            startAtShowField.choices = startAtShowLanguage;

                            //인덱스 설정
                            startAtShowField.index = (int) _hotKeySettings.StartAtShow;

                            //오토 패치 설정
                            autoOverride.value = _hotKeySettings.AutoShaderGraphOverride;

                            //언어 설정
                            SetLanguage();
                        }
                        else
                            addSettingsBtn.SetEnabled(true);
                    }
                }

#if ENABLE_INPUT_SYSTEM
                //액션 인풋이 없는 경우 기본적으로 띄웁니다.
                if (actionId == -1)
                    addInputActionBtn.SetEnabled(true);
                else
                {
                    //가져왔을때 문제가 없을 경우
                    string actionPath = AssetDatabase.GetAssetPath(actionId);

                    //가져왔을때 문제가 있을 경우
                    if (string.IsNullOrEmpty(actionPath))
                    {
                        //-1로 초기화
                        EditorPrefs.SetInt("SGHKInputActionID", -1);
                        
                        //인풋 액션 파일 추가 버튼 활성화
                        addInputActionBtn.SetEnabled(true);
                    }
                    else
                    {
                        //실제 파일로 처리한다.
                        InputActionAsset actionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(actionPath);
                        
                        if (actionAsset)
                        {
                            addInputActionBtn.SetEnabled(false);
                            inputActionField.value = actionAsset;
                            _inputActionAsset = actionAsset;
                        }
                        else
                        {
                            addInputActionBtn.SetEnabled(true);
                            EditorPrefs.SetInt("SGHKInputActionID", -1);
                        }
                    }
                }
#endif
                // #region PatchCodeEnable
                //
                // if (settingsField.value == null || _hotKeySettings == null || inputActionField.value == null ||
                //     _inputActionAsset == null)
                // {
                //     autoOverride.value = false;
                //     patchCodeBtn.SetEnabled(false);
                //     autoOverride.SetEnabled(false);
                // }
                // else
                // {
                //     patchCodeBtn.SetEnabled(true);
                //     autoOverride.SetEnabled(true);
                // }
                //
                // #endregion
            }

            void SetLanguage()
            {
                switch (_hotKeySettings.Language)
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

#if ENABLE_INPUT_SYSTEM
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
                    if (settingsField.value == null || _hotKeySettings == null) return;
                    string warningMsg = _hotKeySettings.Language == ShaderGraphHotKeySettings.KLanguage.English
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
                _hotKeySettings = CreateInstance<ShaderGraphHotKeySettings>();
                _hotKeySettings.Language = ShaderGraphHotKeySettings.KLanguage.English; //영어로 설정
                _hotKeySettings.StartAtShow = ShaderGraphHotKeySettings.KStartUp.Always; //컴파일 후 계속 켜짐 설정
                _hotKeySettings.AutoShaderGraphOverride = false; //초기에는 해제
                AssetDatabase.CreateAsset(_hotKeySettings, path);
                settingsField.value = _hotKeySettings;

                //파일 위치 저장
                int id = _hotKeySettings.GetInstanceID();
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

                if (defines.Contains(HotKeyDefine)) return;

                string addHotKeyDefineToCurrentDefine = string.Concat(defines, ";", HotKeyDefine);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                    addHotKeyDefineToCurrentDefine);
            }
#endif
        }

        //패키지를 오버라이드 시킵니다.
        private static void OverridePackage()
        {
#if ENABLE_SHADERGRAPH
            string shaderGraphPackagePath = HotKeyUtility.GetPackagePath(ShaderGraphPackageName);

            string filePath = $"{shaderGraphPackagePath}/Editor/Drawing/Views/GraphEditorView.cs";
            
            //전체 코드를 가져옵니다.
            string text = File.ReadAllText(filePath);

            if (text.Contains(HotKeyAssembly)) return;

            text = text.Replace("namespace UnityEditor.ShaderGraph.Drawing",
                    $"{HotKeyAssembly}\n\nnamespace UnityEditor.ShaderGraph.Drawing")
                .Replace("MaterialGraphView m_GraphView;",
                    "public static Action<MaterialGraphView, GraphData> keyboardCallback;\n\t\t\t\tMaterialGraphView m_GraphView;")
                .Replace("m_InspectorView.InitializeGraphSettings();",
                    "m_InspectorView.InitializeGraphSettings();\n\t\t\t\tkeyboardCallback?.Invoke(graphView, graph);");

            File.WriteAllText(filePath, text);

            AssetDatabase.Refresh();
            EditorUtility.RequestScriptReload();
#endif
        }

        //인풋에셋에 노드를 세팅합니다.
        private void CreateAllNode(string path)
        {
#if ENABLE_SHADERGRAPH && SHADER_GRAPH_HOTKEY
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

#if SHADER_GRAPH_HOTKEY
        private static string DefaultSetKeyPath(Type nodeType)
        {
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
                default:
                    keyboardPath = string.Empty;
                    break;
            }

            return keyboardPath;
        }
#endif

        private static void Progress()
        {
            if (!_addRequest.IsCompleted) return;

            switch (_addRequest.Status)
            {
                case StatusCode.Success:
                    Debug.Log("Install success");
                    break;
                case >= StatusCode.Failure:
                    Debug.Log(_addRequest.Error.message);
                    break;
            }

            EditorApplication.update -= Progress;
        }


        private static bool IsAllApplyHintNode()
        {
#if ENABLE_INPUT_SYSTEM
            //const string kDefaultKey = "<Keyboard>/";
            InputActionAsset inputActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(NodeAssetPath);

            //힌트가 없으면 
            if (inputActionAsset == null) return true;

            var hasHotKeyNodes = new Dictionary<string, string>();

            foreach (InputActionMap actionMap in inputActionAsset.actionMaps)
            foreach (InputAction action in actionMap.actions)
            foreach (InputBinding binding in action.bindings.Where(binding => binding.path.Length != 0))
                hasHotKeyNodes.Add(action.name, binding.path);

            //힌트가 없으면 
            if (hasHotKeyNodes.Count == 0) return true;


            foreach (var hasHotKeyNode in hasHotKeyNodes)
            {
                //해당 경로에 키 이름으로 찾습니다.
                string[] guidByNode = AssetDatabase.FindAssets(hasHotKeyNode.Key,
                    new[] {"Packages/com.unity.shadergraph/Editor/Data/Nodes"});

                //실제 패스로 변환
                string nodePath = AssetDatabase.GUIDToAssetPath(guidByNode[0]);

                //전체 코드를 가져옵니다.
                string text = File.ReadAllText(nodePath);

                //이미 적용 완료되어 있으면 Pass
                if (text.Contains("//HotKey\n")) continue;
                return false;
            }
#endif
            return true;
        }

        private static void AddHotKeyHintToNode()
        {
#if ENABLE_INPUT_SYSTEM
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

                //이미 적용 완료되어 있으면 Pass
                if (text.Contains("//HotKey\n")) continue;

                text = text.Insert(0, "//HotKey\n");

                //각 라인별로 자르기
                string[] codeLines = text.Split('\r', '\n');

                //단축키를 글자
                string hotKey = hasHotKeyNode.Value.Replace(kDefaultKey, "");

                //라인별로 체크
                foreach (string codeLine in codeLines)
                {
                    if (!codeLine.Contains("[Title(")) continue;
                    int lastIndex = codeLine.LastIndexOf('\"');
                    string afterTitleLine = codeLine.Insert(lastIndex, $" ({hotKey.ToUpper()})");
                    text = text.Replace(codeLine, afterTitleLine);
                    break;
                }

                File.WriteAllText(nodePath, text);
                return;
            }
#endif
        }

        /// <summary>
        /// 노드에 있는 힌트를 모두 제거합니다.
        /// </summary>
        private static void RemoveAllHint()
        {
#if false
            InputActionAsset inputActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(NodeAssetPath);

            foreach (var actionMap in inputActionAsset.actionMaps)
            {
                foreach (InputAction action in actionMap.actions)
                {
                    string[] guidByNode = AssetDatabase.FindAssets(action.name,
                        new[] {"Packages/com.unity.shadergraph/Editor/Data/Nodes"});

                    if (guidByNode.Length < 1) continue;
                    
                    string nodePath = AssetDatabase.GUIDToAssetPath(guidByNode[0]);

                    if (!string.IsNullOrEmpty(nodePath))
                    {
                        //전체 코드를 가져옵니다.
                        string text = File.ReadAllText(nodePath);

                        //적용되어 있지 않으면 Pass
                        if (!text.Contains("//HotKey\n")) continue;

                        //제거
                        text = text.Replace("//HotKey\n", "");

                        //각 라인별로 자르기
                        string[] codeLines = text.Split('\r', '\n');

                        //라인별로 체크
                        foreach (string codeLine in codeLines)
                        {
                            if (!codeLine.Contains("[Title(")) continue;
                            int startIndex = codeLine.LastIndexOf('(') - 1; // -1은 ' '이것을 자른 것이다.
                            int lastIndex = codeLine.LastIndexOf('\"', codeLine.Length - 3);

                            int deleteCount = lastIndex - startIndex;

                            string originCode = codeLine.Remove(startIndex, deleteCount);

                            text = text.Replace(codeLine, originCode);
                            break;
                        }

                        File.WriteAllText(nodePath, text);
                    }
                }
            }

            AssetDatabase.Refresh();
#endif
        }

        private static bool HasPackage(string package)
        {
            string jsonText = File.ReadAllText("Packages/manifest.json");
            bool result = jsonText.Contains(package);

            return result;
        }

        private static void ChangeInputSystemBoth()
        {
            if (!EditorUtility.DisplayDialog("Warning", "진짜로 Both할거야?", "Yes", "No")) return;

            EditorPlayerSettingHelpers.oldSystemBackendsEnabled = true;
            EditorPlayerSettingHelpers.oldSystemBackendsEnabled = true;
            RestartEditorAndRecompileScripts();
        }

        private static void RestartEditorAndRecompileScripts(bool dryRun = false)
        {
            // The API here are not public. Use reflection to get to them.
            Type editorApplicationType = typeof(EditorApplication);
            MethodInfo restartEditorAndRecompileScripts =
                editorApplicationType.GetMethod("RestartEditorAndRecompileScripts",
                    BindingFlags.NonPublic | BindingFlags.Static);
            if (!dryRun)
            {
                if (restartEditorAndRecompileScripts != null)
                    restartEditorAndRecompileScripts.Invoke(null, null);
            }
            else if (restartEditorAndRecompileScripts == null)
                throw new MissingMethodException(editorApplicationType.FullName, "RestartEditorAndRecompileScripts");
        }

        private static void EndCompile()
        {
#if ENABLE_SHADERGRAPH
            //세팅 파일의 GUID를 가져옵니다.
            int settingsId = EditorPrefs.GetInt("SGHKSettingsID", -1);

            //-1이 아니라면 -> 없는게 아니라면,
            if (settingsId != -1)
            {
                //GUID를 실제 경로로 변경합니다.
                string path = AssetDatabase.GetAssetPath(settingsId);

                //가져왔는데 없지 않을 경우
                if (!string.IsNullOrEmpty(path))
                {
                    //에셋의 데이터를 실체화 합니다.
                    ShaderGraphHotKeySettings settings = AssetDatabase.LoadAssetAtPath<ShaderGraphHotKeySettings>(path);

                    //자동적으로 오버라이드 토글이 켜져 있을 경우
                    if (settings.AutoShaderGraphOverride)
                    {
                        //쉐이더 그래프의 경로를 가져옵니다.
                        string shaderGraphPackagePath = HotKeyUtility.GetPackagePath(ShaderGraphPackageName);

                        //해당 경로에서 GraphEditorView를 가져옵니다.
                        string filePath = $"{shaderGraphPackagePath}/Editor/Drawing/Views/GraphEditorView.cs";

                        //해당 파일 데이터 객체로 만듭니다.
                        FileInfo file = new(filePath);

                        //null -> return
                        Assert.IsNotNull(file, "file != null");

                        //전체 코드를 가져옵니다.
                        string text = File.ReadAllText(filePath);

                        //GraphEditorView에 어셈블리가 추가되었는지 체크합니다.
                        //없다면 오버라이드 패키지를 계속 지속합니다.
                        if (!text.Contains(HotKeyAssembly))
                            OverridePackage();

                        // bool isUpdateHint = EditorPrefs.GetBool("UpdateHint", false);
                        // Debug.Log(isUpdateHint);
                        // if (isUpdateHint)
                        // {
                        //     RemoveAllHint();
                        //
                        //     // //힌트적용이 안된것이 있는지 체크
                        //     // if (!IsAllApplyHintNode())
                        //     //     AddHotKeyHintToNode();
                        //     // {
                        //     //     EditorPrefs.SetBool("UpdateHint", false);
                        //     //     Debug.Log("힌트 적용 완료");
                        //     // }
                        // }
                    }
                }
            }
#endif
        }
    }
}