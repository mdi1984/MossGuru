using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MossGuru.Core.Util;

namespace MossGuru.Core
{
  public delegate void MossStatusUpdateHandler(string state, double? percentCompleted);

  public class MossClient
  {
    private readonly string serverAddress;
    private readonly int port;
    private readonly string userId;
    private readonly int maxMatches;
    private readonly int displayResults;
    private readonly string lang;
    private readonly string comment;

    public event MossStatusUpdateHandler MossStatusUpdate;

    public MossClient(string serverAddress, int port, string userId, string lang, int maxMatches, int displayResults, string comment)
    {
      this.serverAddress = serverAddress;
      this.port = port;
      this.userId = userId;
      this.lang = lang.Replace(" ", string.Empty);
      this.maxMatches = maxMatches;
      this.displayResults = displayResults;
      this.comment = comment;
    }

    public MossClientResult SendFiles(DirectoryInfo baseDir, IEnumerable<DirectoryInfo> studentDirs, string[] extensions = null, string[] ignoreFolders = null)
    {
      IPEndPoint endPoint;
      try
      {
        var address = Dns.GetHostEntry(this.serverAddress).AddressList[0];
        endPoint = new IPEndPoint(address, this.port);
      }
      catch (SocketException ex)
      {
        return new MossClientResult() { Success = false, Status = ex.Message };
      }

      using (var client = new TcpClient())
      {
        try
        {
          client.Connect(endPoint);
          using (var stream = client.GetStream())
          {
            MossStatusUpdate?.Invoke("Client connected", null);

            this.SendOption("moss", this.userId, stream);
            this.SendOption("directory", "1", stream);
            this.SendOption("X", "0", stream);
            this.SendOption("maxmatches", this.maxMatches.ToString(), stream);
            this.SendOption("show", this.displayResults.ToString(), stream);
            this.SendOption("language", this.lang, stream);

            var responseBytes = new byte[512];
            var bytesRead = stream.Read(responseBytes, 0, 512);
            var response = Encoding.UTF8.GetString(responseBytes, 0, bytesRead);

            if (!response.StartsWith("yes"))
            {
              return new MossClientResult() { Success = false, Status = "invalid request" };
            }

            MossStatusUpdate?.Invoke("Initialized. Sending files...", null);

            var fCount = 1;
            var directoryInfos = studentDirs as IList<DirectoryInfo> ?? studentDirs.ToList();
            var totalFiles = directoryInfos.Sum(s => FileUtils.GetFilesFromDir(s.FullName, extensions, ignoreFolders).Count());
            foreach (var dir in directoryInfos)
            {
              var studentFiles = FileUtils.GetFilesFromDir(dir.FullName, extensions, ignoreFolders);

              foreach (var file in studentFiles)
              {
                var fileName = $@"{dir.Name}/{file.FullName.Substring(baseDir.FullName.Length).Substring(1).Replace("\\", "_")}".RemoveWhiteSpaces();
                MossStatusUpdate?.Invoke($"sending File: {file.FullName}...", ((double)(fCount - 1) / totalFiles) - 1);
                this.SendFile(file, fileName, this.lang, fCount++, stream, client.Client);
              }
            }

            this.SendOption("query 0", this.comment, stream);

            MossStatusUpdate?.Invoke($"{fCount - 1} files sent. waiting for server response", 0.99);
            responseBytes = new byte[512];
            bytesRead = stream.Read(responseBytes, 0, 512);
            response = Encoding.UTF8.GetString(responseBytes, 0, bytesRead);

            this.SendOption("end", string.Empty, stream);

            if (Uri.IsWellFormedUriString(response, UriKind.Absolute))
            {
              return new MossClientResult() { Success = true, ResultUri = new Uri(response) };
            }
            else if (response.ToLower().Contains("error"))
            {
              return new MossClientResult() { Success = false, Status = $"Server response : {response}" };
            }

            return new MossClientResult() { Success = false, Status = "invalid Response Uri" };
          }
        }
        catch (SocketException ex)
        {
          return new MossClientResult() { Success = false, Status = "Connection failed" };
        }
        catch (Exception ex)
        {
          return new MossClientResult() { Success = false, Status = ex.Message };
        }
      }
    }

    private void SendFile(FileInfo file, string fileName, string lang, int number, NetworkStream stream, Socket socket)
    {
      var fileBytes = Encoding.UTF8.GetBytes(File.ReadAllText(file.FullName));
      var filestr = $"{number} {lang} {fileBytes.Length} {fileName}";

      this.SendOption("file", filestr, stream);
      this.Send(fileBytes, stream);
    }

    private void SendOption(string option, string value, NetworkStream stream)
    {
      this.Send(Encoding.UTF8.GetBytes($"{option} {value}\n"), stream);
    }

    private void Send(byte[] byteArray, NetworkStream stream)
    {
      stream.Write(byteArray, 0, byteArray.Length);
    }
  }
}
