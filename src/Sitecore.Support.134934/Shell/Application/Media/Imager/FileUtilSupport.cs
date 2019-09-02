namespace Sitecore.Support.Shell.Applications.Media.Imager
{
    using Sitecore.Configuration;
    using Sitecore.Diagnostics;
    using Sitecore.IO;
    using System;
    using System.IO;

    internal static class FileUtilSupport
    {
        internal static string GetTemporaryFile(string file)
        {
            Assert.ArgumentNotNull(file, "file");
            file = string.Concat(str2: DateUtil.ToServerTime(DateTime.UtcNow).ToString("yyyyMMddTHHmmssffff"), str0: Path.GetFileNameWithoutExtension(file), str1: ".", str3: Path.GetExtension(file));
            TempFolder.EnsureFolder();
            string folder = TempFolder.Folder;
            return FileUtil.MapPath(FileUtil.MakePath(folder, file, '/'));
        }
    }
}