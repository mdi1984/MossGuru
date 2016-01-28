using System;

namespace MossGuru.Core
{
  public class MossClientResult
  {
    public bool Success { get; set; }
    public string Status { get; set; }
    public Uri ResultUri { get; internal set; }
  }
}