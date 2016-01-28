using System;
using System.Collections;
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

    public static IEnumerable<FileInfo> GetFilesFromDir(string path, string[] extensions = null, string[] ignoreFolders = null)
    {
      var dirInfo = new DirectoryInfo(path);
      if (!dirInfo.Exists)
      {
        throw new DirectoryNotFoundException();
      }

      var files = dirInfo.GetFiles("*", SearchOption.AllDirectories).ToList();
      if (extensions != null && extensions.Length > 0)
      {
        files = files.Where(f => extensions.Contains(f.Extension, StringComparer.OrdinalIgnoreCase)).ToList();
      }

      return files.Where(f => ignoreFolders == null || !f.FullName.ContainsAny(ignoreFolders, StringComparison.CurrentCultureIgnoreCase)).ToList();
    }

    

  }
}
