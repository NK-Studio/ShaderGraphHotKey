using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NKStudio.ShaderGraph.HotKey
{
    public class ShaderGraphShotKeyInstaller : Editor
    {
        private const string HotKeyAssembly =
            "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"NKStudio.ShaderGraph.HotKey\")]";

        private const string PackageName = "com.unity.shadergraph";

        private const string HotKeyDefine = "SHADER_GRAPH_HOTKEY";

#if ENABLE_SHADERGRAPH
        [InitializeOnLoadMethod]
        private static void Override()
        {
            //패키지의 정보를 가져옵니다.
            string shaderGraphPackagePath = GetPackagePath(PackageName);

            //쉐이더 그래프에서 GraphEditorView.cs에 대한 파일을 가져옵니다. 
            string filePath = $"{shaderGraphPackagePath}/Editor/Drawing/Views/GraphEditorView.cs";
            
            //전체 코드를 가져옵니다.
            string text = File.ReadAllText(filePath);
            
            //현재 플레이어 세팅스의 디파인을 가져옵니다.
            string defines =
                PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            //쉐이더 그래프 단축키 어셈블리가 작성되어 있고 디파인이 작성되어 있다면 실행
            if (text.Contains(HotKeyAssembly) && defines.Contains(HotKeyDefine))
                Run();
        }
#endif

        private static void Run()
        {
#if SHADER_GRAPH_HOTKEY
            Debug.Log("ShaderGraph HotKey Run");
            UnityEditor.ShaderGraph.Drawing.GraphEditorView.keyboardCallback += ((view, data) =>
            {
                KeyboardHotKey keyboardHotKey = new KeyboardHotKey(view, data);
            });
#endif
        }
    }
}