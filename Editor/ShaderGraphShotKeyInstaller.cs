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

        [InitializeOnLoadMethod]
        private static void Override()
        {
            //패키지의 정보를 가져옵니다.
            DirectoryInfo shaderGraphPackage = GetPackageInstalled(PackageName);

            //쉐이더 그래프가 설치되어 있지 않습니다.
            if (shaderGraphPackage == null)
            {
                Debug.LogError("쉐이더 그래프가 설치되어 있지 않습니다.");
                return;
            }
            
            //쉐이더 그래프에서 GraphEditorView.cs에 대한 파일을 가져옵니다. 
            string filePath = $"{shaderGraphPackage.FullName}/Editor/Drawing/Views/GraphEditorView.cs";
            FileInfo file = new (filePath);

            //파일이 없다면 에러를 표시한다.
            if (!file.Exists)
            {
                Debug.LogError("GraphEditorView.cs이 존재하지 않습니다.");
                return;
            }
            
            //현재 플레이어 세팅스의 디파인을 가져옵니다.
            string defines =
                PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            //전체 코드를 가져옵니다.
            string text = File.ReadAllText(filePath);

            //쉐이더 그래프 단축키 어셈블리가 작성되어 있고 디파인이 작성되어 있다면 실행
            if (text.Contains(HotKeyAssembly) && defines.Contains(HotKeyDefine))
                Run();
        }
        
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
        
        /// <summary>
        /// 패키지가 설치되어 있다면 해당 디렉토리 데이터를 가져옵니다.
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns></returns>
        private static DirectoryInfo GetPackageInstalled(string packageName)
        {
            //폴더 안의 정보를 가져옵니다.
            DirectoryInfo packageCache = new("Library/PackageCache");

            //찾고자 하는 패키지를 찾습니다.
            DirectoryInfo result = packageCache.GetDirectories()
                .FirstOrDefault(package => package.Name.Contains(packageName));

            return result;
        }
    }
}