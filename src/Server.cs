using codecrafters_http_server.src.Enums;
using codecrafters_http_server.src.Helper;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_http_server
{
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
                byte[] data = new byte[(int)DataSizeEnum.Kilobyte];
                string okMessage = $"HTTP/1.1 {(int)HTTPStatusCodesEnum.Ok} OK\r\n";
                string notFoundMessage = $"HTTP/1.1 {(int)HTTPStatusCodesEnum.NotFound} Not Found\r\n\r\n";
                string createdMessage = $"HTTP/1.1 {(int)HTTPStatusCodesEnum.Created} Created\r\n\r\n";

                while (true)
                {
                    Socket socket = server.AcceptSocket();

                    int dataSize = socket.Receive(data);
                    string requestData = Encoding.UTF8.GetString(data, 0, dataSize);

                    var requestLines = requestData.Split("\r\n");
                    var requestParts = Helper.SplitString(requestLines[0], '/');

                    if (requestParts.Length > 1 && requestParts[1] == " HTTP")
                        httpResponse = $"{okMessage}\r\n";
                    else if (requestParts[1].StartsWith("echo"))
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
                            httpResponse = $"HTTP/1.1 200 OK\r\nContent-Encoding: gzip\r\nContent-Type: text/plain\r\nContent-Length: {compressedData.Length}\r\n\r\n";

                            socket.Send([.. Encoding.UTF8.GetBytes(httpResponse), .. compressedData]);
                            socket.Close();

                            break;
                        }
                        else
                            httpResponse = $"{okMessage}Content-Type: text/plain\r\nContent-Length: {Helper.SplitString(requestParts[2], ' ')[0].Length}\r\n\r\n{Helper.SplitString(requestParts[2], ' ')[0]}";
                    }
                    else if (requestParts[1].StartsWith("user-agent"))
                        httpResponse = $"{okMessage}Content-Type: text/plain\r\nContent-Length: {Helper.SplitString(requestLines[2], ' ')[1].Length} \r\n\r\n{Helper.SplitString(requestLines[2], ' ')[1]}\r\n";
                    else if (requestParts[1].StartsWith("files"))
                    {
                        // ./server.sh --directory <directory>
                        string[] commandLineArgs = Environment.GetCommandLineArgs();

                        string dir = commandLineArgs[2];
                        string fileName = Helper.SplitString(requestParts[2], ' ')[0];
                        string filePath = $"{dir}{fileName}";

                        if (requestParts[0].StartsWith("GET"))
                        {
                            if (File.Exists(filePath))
                            {
                                string fileContent = File.ReadAllText(filePath);
                                httpResponse = $"{okMessage}Content-Type: application/octet-stream\r\nContent-Length: {fileContent.Length}\r\n\r\n{fileContent}";
                            }
                            else
                                httpResponse = notFoundMessage;
                        }
                        else if (requestParts[0].StartsWith("POST"))
                        {
                            string fileContent = requestLines[requestLines.Length - 1];
                            File.WriteAllText(filePath, fileContent);
                            httpResponse = createdMessage;
                        }
                        else
                            httpResponse = notFoundMessage;
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
}
