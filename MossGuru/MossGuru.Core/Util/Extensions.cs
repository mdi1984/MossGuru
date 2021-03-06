﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MossGuru.Core.Util
{
  public static class StringExtensions
  {
    public static bool ContainsAny(this string input, IEnumerable<string> containsKeywords, StringComparison comparisonType)
    {
      return containsKeywords.Any(keyword => input.IndexOf(keyword, comparisonType) >= 0);
    }
    public static string RemoveWhiteSpaces(this string input)
    {
      return Regex.Replace(input, @"\s+", "");
    }
  }
}
