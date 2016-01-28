using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MossGuru.Core
{
  public delegate void MossStatusUpdateHandler(string state, double? percentCompleted);

  public class MossClient
  {
    private readonly string serverAddress = "moss.stanford.edu";
    private readonly int port = 7690;
    private readonly string userId;
    private readonly int maxMatches;
    private readonly int displayResults;
    private readonly string lang;
    private readonly string comment;

    public event MossStatusUpdateHandler MossStatusUpdate;

    public MossClient(string userId, string lang, int maxMatches, int displayResults, string comment)
    {
      this.userId = userId;
      this.lang = lang;
      this.maxMatches = maxMatches;
      this.displayResults = displayResults;
      this.comment = comment;
    }

    public MossClientResult SendFiles(ICollection<FileInfo> files)
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

            // send files
            int fCount = 1;
            foreach (var file in files)
            {
              MossStatusUpdate?.Invoke($"sending File: {file.FullName}...", (double)(fCount - 1) / files.Count);
              this.SendFile(file, this.lang, fCount++, stream, client.Client);
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

    private void SendFile(FileInfo file, string lang, int number, NetworkStream stream, Socket socket)
    {
      var fileBytes = Encoding.UTF8.GetBytes(File.ReadAllText(file.FullName));
      var filestr = $"{number} {lang} {fileBytes.Length} {file.FullName.Replace("\\", "/")}";

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
