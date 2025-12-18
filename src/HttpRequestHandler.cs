using System.Net.Sockets;
using System.Text;
using codecrafters_http_server.helpers;

namespace codecrafters_http_server;

public class HttpRequestHandler {
    readonly string[] endpoints = ["index.html"];

    public async Task Serve(TcpClient client, string? directory = null) {
        NetworkStream stream = client.GetStream();

        byte[] requestBuffer = new byte[1024];

        int bytesRead = await stream.ReadAsync(requestBuffer);
        if (bytesRead == 0) {
            Console.WriteLine("No data received from client.");
            return;
        }

        string request = Encoding.UTF8.GetString(requestBuffer);

        //          0                           1                       2        ...              
        // GET /index.html HTTP/1.1\r\nHost: localhost:4221\r\nUser-Agent: curl/7.64.1\r\nAccept: */*\r\n\r\n Body
        string[] requestParams = request.Split("\r\n");
        // GET /index.html HTTP/1.1 || Host: localhost:4221 || User-Agent: curl/7.64.1 || Accept: */*

        // GET /index.html HTTP/1.1\ -> /index.html
        string requestType = requestParams[0].Split(" ")[0];
        string address = requestParams[0].Split(" ")[1];

        ResponseBuilder rb = new();

        StringBuilder response;

        if ((address[0] is '/' && endpoints.Contains(address[1..])) || address is "/" or null)
            response = rb.WithStatusCode("200").WithContent("").Build();
        else if (address.StartsWith("/echo")) {
            string randomString = address.Split("/")[2];
            response = rb.WithStatusCode("200").WithContent(randomString).Build();
        }
        else if (address.StartsWith("/user-agent")) {
            string? userAgentParam = requestParams.FirstOrDefault(x => x.Contains("User-Agent"));

            if (userAgentParam == null) return;

            string userAgent = userAgentParam.Split(" ")[1];
            response = rb.WithStatusCode("200").WithContent(userAgent).Build();
        }
        else if (address.StartsWith("/files")) {
            string baseDirectory = directory ?? Path.Combine(AppContext.BaseDirectory, "files");
            string fileName = address.Split("/")[2];
            string filePath = Path.Combine(baseDirectory, fileName);

            if (requestType == "GET") {
                Console.WriteLine($"Serving file: {filePath}");

                if (!File.Exists(filePath))
                    response = rb.WithStatusCode("404")
                        .WithContent("404 Not Found")
                        .Build();
                else {
                    byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                    string fileContent = Encoding.UTF8.GetString(fileBytes);

                    Console.WriteLine($"File content: {fileContent}");

                    response = rb.WithStatusCode("200")
                        .WithContentType("application/octet-stream")
                        .WithContent(fileContent)
                        .Build();
                }
            }
            else if (requestType == "POST") {
                string body = requestParams[^1];
                Console.WriteLine($"Received POST request with body: {body}");
                await File.WriteAllTextAsync(filePath, body);
                response = rb.WithStatusCode("201").WithContent("Created").Build();
            }
            else
                response = rb.WithStatusCode("405").WithContent("Method Not Allowed").Build();
        }
        else
            response = rb.WithStatusCode("404").WithContent("Not Found").Build();

        byte[] responseData = Encoding.UTF8.GetBytes(response.ToString());
        await stream.WriteAsync(responseData);

        Console.WriteLine("Client disconnected.");
    }
}