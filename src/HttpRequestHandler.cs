using System.IO.Compression;
using System.Net.Sockets;
using System.Text;
using codecrafters_http_server.helpers;

namespace codecrafters_http_server;

public class HttpRequestHandler {
    readonly string[] encodings = ["gzip"];
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

        Console.WriteLine("Full Request Params:");
        foreach (string param in requestParams) Console.WriteLine(param);

        // GET /index.html HTTP/1.1\ -> /index.html
        string requestType = requestParams[0].Split(" ")[0];
        string address = requestParams[0].Split(" ")[1];
        int? contentLength =
            int.TryParse(requestParams.FirstOrDefault(x => x.StartsWith("Content-Length"))?.Split(" ")[1], out int cl)
                ? cl
                : null;

        string? acceptEncodingParam = requestParams.FirstOrDefault(x => x.StartsWith("Accept-Encoding"));
        var acceptEncodingsEnumerable = acceptEncodingParam?.Split(":")[1].Split(",").Select(x => x.Trim());
        List<string> acceptEncodings = [];

        if (acceptEncodingsEnumerable != null)
            acceptEncodings = acceptEncodingsEnumerable.ToList();

        foreach (string enc in acceptEncodings) Console.WriteLine(enc);

        ResponseBuilder rb = new();

        if ((address[0] is '/' && endpoints.Contains(address[1..])) || address is "/" or null)
            rb.WithStatusCode("200").WithContent("");
        else if (address.StartsWith("/echo")) {
            string randomString = address.Split("/")[2];
            byte[] contentBytes;

            rb.WithStatusCode("200").WithContent(randomString);

            if (acceptEncodings.Any(IsAcceptableEncoding)) {
                using MemoryStream compressedStream = new();
                await using (GZipStream gzip = new(compressedStream, CompressionMode.Compress, true)) {
                    byte[] stringBytes = Encoding.UTF8.GetBytes(randomString);
                    gzip.Write(stringBytes, 0, stringBytes.Length);
                }

                contentBytes = compressedStream.ToArray();

                rb.WithHeader("Content-Encoding", "gzip");
            }
            else
                contentBytes = Encoding.UTF8.GetBytes(randomString);

            rb.WithStatusCode("200").WithContent(contentBytes);
        }
        else if (address.StartsWith("/user-agent")) {
            string? userAgentParam = requestParams.FirstOrDefault(x => x.Contains("User-Agent"));

            if (userAgentParam == null) return;

            string userAgent = userAgentParam.Split(" ")[1];
            rb.WithStatusCode("200").WithContent(userAgent);
        }
        else if (address.StartsWith("/files")) {
            string baseDirectory = directory ?? Path.Combine(AppContext.BaseDirectory, "files");
            string fileName = address.Split("/")[2];
            string filePath = Path.Combine(baseDirectory, fileName);

            if (requestType == "GET") {
                Console.WriteLine($"Serving file: {filePath}");

                if (!File.Exists(filePath))
                    rb.WithStatusCode("404")
                        .WithContent("404 Not Found");
                else {
                    byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                    string fileContent = Encoding.UTF8.GetString(fileBytes);

                    Console.WriteLine($"File content: {fileContent}");

                    rb.WithStatusCode("200")
                        .WithContentType("application/octet-stream")
                        .WithContent(fileContent);
                }
            }
            else if (requestType == "POST") {
                if (contentLength == null) return;
                string body = requestParams[^1];
                string sanitizedBody = body.TrimEnd('\0', ' ', '\r', '\n');
                byte[] bytes = Encoding.UTF8.GetBytes(sanitizedBody);
                Console.WriteLine($"Writing bytes: {Encoding.UTF8.GetString(bytes)}");

                await File.WriteAllBytesAsync(filePath, bytes);
                rb.WithStatusCode("201");
            }
            else
                rb.WithStatusCode("405").WithContent("Method Not Allowed");
        }
        else
            rb.WithStatusCode("404").WithContent("Not Found");

        byte[] responseData = Encoding.UTF8.GetBytes(rb.Build().ToString());
        await stream.WriteAsync(responseData);

        Console.WriteLine("Client disconnected.");
    }

    bool IsAcceptableEncoding(string encoding) => encodings.Contains(encoding);
}