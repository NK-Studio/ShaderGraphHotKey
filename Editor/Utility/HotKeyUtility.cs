using System.IO;
using System.Linq;

namespace NKStudio.ShaderGraph.HotKey
{
    public class HotKeyUtility
    {
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
    }
}