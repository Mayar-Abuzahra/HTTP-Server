using codecrafters_http_server.src.Enums;
using codecrafters_http_server.src.Helper;
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
                        httpResponse = $"{okMessage}Content-Type: text/plain\r\nContent-Length: {Helper.SplitString(requestParts[2], ' ')[0].Length}\r\n\r\n{Helper.SplitString(requestParts[2], ' ')[0]}";
                    else if (requestParts[1].StartsWith("user-agent"))
                        httpResponse = $"{okMessage}Content-Type: text/plain\r\nContent-Length: {Helper.SplitString(requestLines[2], ' ')[1].Length} \r\n\r\n{Helper.SplitString(requestLines[2], ' ')[1]}\r\n";
                    else if (requestParts[1].StartsWith("files"))
                    {
                        // ./server.sh --directory <directory>
                        string[] commandLineArgs = Environment.GetCommandLineArgs();

                        // <directory> is the third argument
                        string dir = commandLineArgs[2];
                        string fileName = Helper.SplitString(requestParts[2], ' ')[0];
                        string filePath = $"{dir}{fileName}";

                        if (File.Exists(filePath))
                        {
                            string fileContent = File.ReadAllText(filePath);
                            httpResponse = $"{okMessage}Content-Type: application/octet-stream\r\nContent-Length: {fileContent.Length}\r\n\r\n{fileContent}";
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
