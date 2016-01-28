using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MossGuru.Core.Util
{
  public static class FileUtils
  {
    public static IEnumerable<DirectoryInfo> GetTopLevelDirectories(string path)
    {
      var dirInfo = new DirectoryInfo(path);
      if (!dirInfo.Exists)
      {
        throw new DirectoryNotFoundException();
      }

      return dirInfo.GetDirectories("*", SearchOption.TopDirectoryOnly);
    }

    public static IEnumerable<FileInfo> GetFilesFromDir(string path, string[] extensions, string[] ignoreFolders = null)
    {
      var dirInfo = new DirectoryInfo(path);
      if (!dirInfo.Exists)
      {
        throw new DirectoryNotFoundException();
      }

      var extensionStr = extensions.Aggregate((c, n) => $"{c}, *.{n}");
      var files = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Where(f => extensions.Contains(f.Extension)).ToList();

      return files.Where(f => !f.FullName.ContainsAny(ignoreFolders, StringComparison.CurrentCultureIgnoreCase)).ToList();
    }
  }
}
