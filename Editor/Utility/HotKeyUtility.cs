using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace NKStudio.ShaderGraph.HotKey
{
    public class HotKeyUtility
    {
        private const string ShaderGraphPackageName = "com.unity.shadergraph";
        
        private const string HotKeyAssembly =
            "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"NKStudio.ShaderGraph.HotKey\")]";
        
        private const string HotKeyDefine = "SHADER_GRAPH_HOTKEY";
        
        /// <summary>
        /// 패키지가 설치되어 있다면 해당 디렉토리 데이터를 가져옵니다.
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns></returns>
        public static string GetPackagePath(string packageName)
        {
            //폴더 안의 정보를 가져옵니다.
            DirectoryInfo packageCache = new("Library/PackageCache");

            //찾고자 하는 패키지를 찾습니다.
            DirectoryInfo result = packageCache.GetDirectories()
                .FirstOrDefault(package => package.Name.Contains(packageName));

            return result == null ? string.Empty : result.FullName;
        }

        /// <summary>
        /// 시스템 언어가 한국어이면 True, 아니면 False를 반환한다.
        /// </summary>
        /// <returns></returns>
        public static bool IsSystemLanguageKorean() => Application.systemLanguage == SystemLanguage.Korean;

        /// <summary>
        /// 쉐이더 그래프가 패치되어 있으면 True, 아니면 False를 반환한다.
        /// </summary>
        /// <returns></returns>
        public static bool IsPatchCode()
        {
            //쉐이더 그래프의 경로를 가져옵니다.
            string shaderGraphPackagePath = GetPackagePath(ShaderGraphPackageName);

            //해당 경로에서 GraphEditorView를 가져옵니다.
            string filePath = $"{shaderGraphPackagePath}/Editor/Drawing/Views/GraphEditorView.cs";
            
            //전체 코드를 가져옵니다.
            string text = File.ReadAllText(filePath);
            
            //Return;
            return text.Contains(HotKeyAssembly);
        }

        public static bool HasSettingsFile()
        {
            int settingsGuid = EditorPrefs.GetInt("SGHKSettingsID", -1);

            if (settingsGuid == -1)
                return false;
            
            string path = AssetDatabase.GetAssetPath(settingsGuid);
            if (string.IsNullOrEmpty(path))
                return false;

            ShaderGraphSettings settings = AssetDatabase.LoadAssetAtPath<ShaderGraphSettings>(path);
            return settings;
        }
        
        /// <summary>
        /// 디파인을 설치합니다.
        /// </summary>
        public void InstallDefine()
        {
            string defines =
                PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            if (defines.Contains(HotKeyDefine)) return;

            string addHotKeyDefineToCurrentDefine = string.Concat(defines, ";", HotKeyDefine);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                addHotKeyDefineToCurrentDefine);
        }
    }
}