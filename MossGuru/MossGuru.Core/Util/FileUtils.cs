﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MossGuru.Core.Util
{
  public static class FileUtils
  {
    public static IEnumerable<FileInfo> GetFilesFromDir(string path, string extension, string[] ignoreFolders = null)
    {
      var dirInfo = new DirectoryInfo(path);
      if (!dirInfo.Exists)
      {
        throw new DirectoryNotFoundException();
      }

      var files = dirInfo.GetFiles(string.Format("*.{0}", extension), SearchOption.AllDirectories);
      return files.Where(f => !f.FullName.ContainsAny(ignoreFolders, StringComparison.CurrentCultureIgnoreCase)).ToList();
    }
  }
}
