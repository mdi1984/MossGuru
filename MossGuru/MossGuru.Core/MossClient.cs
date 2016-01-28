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
      this.lang = lang;
      this.maxMatches = maxMatches;
      this.displayResults = displayResults;
      this.comment = comment;
    }

    public MossClientResult SendFiles(DirectoryInfo baseDir, IEnumerable<DirectoryInfo> studentDirs, string extension, string[] ignoreFolders = null)
    {
      var address = Dns.GetHostEntry(this.serverAddress).AddressList[0];
      var endPoint = new IPEndPoint(address, this.port);

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

            MossStatusUpdate?.Invoke("Initialized. Sending files...", null);

            var fCount = 1;
            var directoryInfos = studentDirs as IList<DirectoryInfo> ?? studentDirs.ToList();
            var totalFiles = directoryInfos.Sum(s => FileUtils.GetFilesFromDir(s.FullName, extension, ignoreFolders).Count());
            foreach (var dir in directoryInfos)
            {
              var studentFiles = FileUtils.GetFilesFromDir(dir.FullName, extension, ignoreFolders);

              foreach (var file in studentFiles)
              {
                var fileName = $@"{dir.Name}/{file.FullName.Substring(baseDir.FullName.Length).Substring(1).Replace("\\", "_")}";
                MossStatusUpdate?.Invoke($"sending File: {file.FullName}...", (double)(fCount - 1) / totalFiles);
                this.SendFile(file, fileName, this.lang, fCount++, stream, client.Client);
              }
            }

            this.SendOption("query 0", this.comment, stream);

            var response = new byte[512];
            var bytesRead = stream.Read(response, 0, 512);
            this.SendOption("end", string.Empty, stream);

            Uri result = null;
            if (Uri.TryCreate(Encoding.UTF8.GetString(response, 0, bytesRead), UriKind.Absolute, out result))
            {
              return new MossClientResult() { Success = true, ResultUri = result };
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
