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

        #region Added code
        // This method changes the path from relative to full.
        internal static string MapPathWithTempRequestPrefix(string path)
        {
            path = Settings.TempFolderPath + "/" + StringUtil.RemovePrefix("/-/temp/", path);
            path = path.Replace('/', '\\');
            if (FileUtil.IsAspxFile(path))
            {
                path = path.Substring(0, path.LastIndexOf(".aspx", StringComparison.InvariantCulture));
            }
            return path;
        }
        #endregion
    }
}