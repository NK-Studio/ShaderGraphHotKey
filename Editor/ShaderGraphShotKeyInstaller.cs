using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace ShaderGraphShotKey.Editor
{
    public class ShaderGraphShotKeyInstaller : UnityEditor.Editor
    {
        private const string HotKeyAssembly =
            "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"NKStudio.ShaderGraph.HotKey\")]";

        private const string PackageName = "com.unity.shadergraph";

        private const string HotKeyDefine = "SHADER_GRAPH_HOTKEY";

        [InitializeOnLoadMethod]
        private static void Override()
        {
            DirectoryInfo shaderGraphPackage = GetPackageInstalled(PackageName);

            Assert.IsNotNull(shaderGraphPackage, "쉐이더 그래프가 설치되어 있지 않습니다.");

            string filePath = $"{shaderGraphPackage.FullName}/Editor/Drawing/Views/GraphEditorView.cs";
            FileInfo file = new (filePath);

            Assert.IsTrue(file.Exists, "GraphEditorView.cs이 존재하지 않습니다.");

            //현재 플레이어 세팅스의 디파인을 가져옵니다.
            string defines =
                PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            //전체 코드를 가져옵니다.
            string text = File.ReadAllText(filePath);

            //쉐이더 그래프 단축키 어셈블리가 작성되어 있다면,
            if (text.Contains(HotKeyAssembly))
                Run();
            else
            {
                #region 어셈블리가 표기되어 있지 않다면,

                string removed = defines.Replace(HotKeyDefine, "");

                //프로젝트 세팅스에서 디파인 제거
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                    removed);
                #endregion
            }
            
            EditorApplication.wantsToQuit += EditorApplicationOnwantsToQuit;
        }
        
        private static void Run()
        {
#if SHADER_GRAPH_HOTKEY
            Debug.Log("동작 중");
            UnityEditor.ShaderGraph.Drawing.GraphEditorView.keyboardCallback += ((view, data) =>
            {
                KeyboardHotKey keyboardHotKey = new KeyboardHotKey(view, data);
            });
#endif
        }

        //유니티 에디터를 끄면 디파인 제거
        private static bool EditorApplicationOnwantsToQuit()
        {
            //현재 플레이어 세팅스의 디파인을 가져옵니다.
            string defines =
                PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            string removed = defines.Replace(HotKeyDefine, "");

            //프로젝트 세팅스에서 디파인 제거
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                removed);

            return true;
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
    }
}