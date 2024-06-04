using codecrafters_http_server.src.Enums;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_http_server
{
    class Program
    {
        public static void Main()
        {
            Console.WriteLine("Program Logs will appear here!");

            TcpListener server = new TcpListener(IPAddress.Any, 4221);
            server.Start();

            try
            {
                string httpResponse = "HTTP/1.1 ";
                byte[] data = new byte[(int)DataSize.Kilobyte];
                string okMessage = $"{httpResponse}{(int)HTTPStatusCodesEnum.Ok} OK\r\n";
                string notFoundMessage = $"{httpResponse}{(int)HTTPStatusCodesEnum.NotFound} Not Found\r\n\r\n";

                while (true)
                {
                    Socket socket = server.AcceptSocket();

                    int dataSize = socket.Receive(data);
                    string requestData = Encoding.UTF8.GetString(data, 0, dataSize);

                    var requestLines = requestData.Split("\r\n");
                    var requestParts = requestLines[0].Split("/");

                    if (requestParts.Length > 1 && requestParts[1] == " HTTP")
                        httpResponse = okMessage;
                    else if (requestParts[1].StartsWith("echo"))
                        httpResponse = $"{okMessage}Content-Type: text/plain\r\nContent-Length: {requestParts[2].Trim().Split(' ')[0].Length}\r\n\r\n{requestParts[2].Trim().Split(' ')[0]}";
                    else if (requestParts[1].StartsWith("user-agent"))
                        httpResponse = $"{okMessage}Content-Type: text/plain\r\nContent-Length: {requestLines[2].Trim().Split(' ')[1].Length} \r\n\r\n{requestLines[2].Trim().Split(' ')[1]}";
                    else if (requestParts[1].StartsWith("files"))
                    {
                        // ./server.sh --directory <directory>
                        string[] commandLineArgs = Environment.GetCommandLineArgs();

                        // <directory> is the third argument
                        string dir = commandLineArgs[2];
                        string fileName = requestParts[1].Split('/')[1];
                        string filePath = $"{dir}{fileName}";

                        if (File.Exists(filePath))
                        {
                            string fileContent = File.ReadAllText(filePath);
                            httpResponse = $"{httpResponse}{(int)HTTPStatusCodesEnum.Ok} OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {fileContent.Length}\r\n\r\n{fileContent}";
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