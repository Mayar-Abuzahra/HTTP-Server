using codecrafters_http_server.src.Enums;
using codecrafters_http_server.src.Helper;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_http_server;

public class Server
{
    public static void Main()
    {
        Console.WriteLine("Program Logs will appear here!");

        try
        {
            TcpListener server = new TcpListener(IPAddress.Any, 4221);
            server.Start();

            string httpResponse = string.Empty;
            string httpVersion = "HTTP/1.1";
            string crlf = "\r\n";
            byte[] data = new byte[(int)DataSize.Kilobyte];
            string okMessage = $"{httpVersion} {(int)HTTPStatusCode.OK} {nameof(HTTPStatusCode.OK)}{crlf}";
            string notFoundMessage = $"{httpVersion} {(int)HTTPStatusCode.NotFound} Not Found{crlf}{crlf}";
            string createdMessage = $"{httpVersion} {(int)HTTPStatusCode.Created} {nameof(HTTPStatusCode.Created)}{crlf}{crlf}";

            while (true)
            {
                Socket socket = server.AcceptSocket();

                int dataSize = socket.Receive(data);
                string requestData = Encoding.UTF8.GetString(data, 0, dataSize);

                var requestLines = requestData.Split(crlf);
                var requestParts = Helper.SplitString(requestLines[0], '/');

                if (requestParts.Length > 1 && requestParts[1] == " HTTP")
                {
                    httpResponse = $"{okMessage}{crlf}";
                }
                else if (requestParts[1].StartsWith(nameof(HTTPFunction.echo)))
                {
                    string[] encodingHeader = (!string.IsNullOrEmpty(requestLines[2])) ? Helper.SplitString(Helper.SplitString(requestLines[2], ':')[1].Trim(), ',') : [];
                    bool containsGzip = encodingHeader.Any(header => string.Equals(header.Trim(), "gzip", StringComparison.OrdinalIgnoreCase));

                    if (containsGzip)
                    {
                        byte[] uncompressedData = Encoding.UTF8.GetBytes(Helper.SplitString(requestParts[2], ' ')[0]);

                        using MemoryStream compressedStream = new MemoryStream();
                        using GZipStream gzipStream = new GZipStream(compressedStream, CompressionMode.Compress, true);

                        gzipStream.Write(uncompressedData, 0, uncompressedData.Length);
                        gzipStream.Flush();
                        gzipStream.Close();

                        byte[] compressedData = compressedStream.ToArray();
                        httpResponse = $"{httpVersion} {(int)HTTPStatusCode.OK} {nameof(HTTPStatusCode.OK)}{crlf}Content-Encoding: gzip{crlf}Content-Type: text/plain{crlf}Content-Length: {compressedData.Length}{crlf}{crlf}";

                        socket.Send([..Encoding.UTF8.GetBytes(httpResponse), ..compressedData]);
                        socket.Close();

                        break;
                    }
                    else
                    {
                        httpResponse = $"{okMessage}Content-Type: text/plain{crlf}Content-Length: {Helper.SplitString(requestParts[2], ' ')[0].Length}{crlf}{crlf}{Helper.SplitString(requestParts[2], ' ')[0]}";
                    }
                }
                else if (requestParts[1].StartsWith(HTTPFunction.user_agent.ToString().Replace('_', '-')))
                {
                    httpResponse = $"{okMessage}Content-Type: text/plain{crlf}Content-Length: {Helper.SplitString(requestLines[2], ' ')[1].Length} {crlf}{crlf}{Helper.SplitString(requestLines[2], ' ')[1]}{crlf}";
                }
                else if (requestParts[1].StartsWith(nameof(HTTPFunction.files)))
                {
                    // ./server.sh --directory <directory>
                    string[] commandLineArgs = Environment.GetCommandLineArgs();

                    string dir = commandLineArgs[2];
                    string fileName = Helper.SplitString(requestParts[2], ' ')[0];
                    string filePath = $"{dir}{fileName}";

                    if (requestParts[0].StartsWith(nameof(HTTPMethod.GET)))
                    {
                        if (File.Exists(filePath))
                        {
                            string fileContent = File.ReadAllText(filePath);
                            httpResponse = $"{okMessage}Content-Type: application/octet-stream{crlf}Content-Length: {fileContent.Length}{crlf}{crlf}{fileContent}";
                        }
                        else
                        {
                            httpResponse = notFoundMessage;
                        }
                    }
                    else if (requestParts[0].StartsWith(nameof(HTTPMethod.POST)))
                    {
                        string fileContent = requestLines[requestLines.Length - 1];
                        File.WriteAllText(filePath, fileContent);
                        httpResponse = createdMessage;
                    }
                    else
                    {
                        httpResponse = notFoundMessage;
                    }
                }
                else
                {
                    httpResponse = notFoundMessage;
                }

                socket.Send(Encoding.UTF8.GetBytes(httpResponse));
                socket.Close();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.StackTrace);
        }
    }
}
