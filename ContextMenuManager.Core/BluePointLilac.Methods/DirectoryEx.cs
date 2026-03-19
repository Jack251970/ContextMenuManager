using System.IO;

namespace BluePointLilac.Methods
{
    public static class DirectoryEx
    {
        public static void CopyTo(string srcDirPath, string dstDirPath)
        {
            var srcDi = new DirectoryInfo(srcDirPath);
            var dstDi = new DirectoryInfo(dstDirPath);
            dstDi.Create();
            foreach (var srcFi in srcDi.GetFiles())
            {
                var dstFilePath = $@"{dstDirPath}\{srcFi.Name}";
                srcFi.CopyTo(dstFilePath, true);
            }
            foreach (var srcSubDi in srcDi.GetDirectories())
            {
                var dstSubDi = dstDi.CreateSubdirectory(srcSubDi.Name);
                CopyTo(srcSubDi.FullName, dstSubDi.FullName);
            }
        }
    }
}