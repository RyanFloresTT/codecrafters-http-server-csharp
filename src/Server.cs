using System.Net;
using System.Net.Sockets;
using System.Text;

string[] endpoints = ["index.html"];

TcpListener server = new(IPAddress.Any, 4221);

try {
    server.Start();
    Console.WriteLine("Server started");

    while (true) {
        TcpClient client = server.AcceptTcpClient();
        Console.WriteLine("Client connected!");

        await Task.Run(() => Serve(client));
    }
}
catch (Exception e) {
    Console.WriteLine($"Error {e} occurred");
}

server.Dispose();
return;

async Task Serve(TcpClient client) {
    NetworkStream stream = client.GetStream();

    byte[] requestBuffer = new byte[1024];

    int bytesRead = await stream.ReadAsync(requestBuffer);
    if (bytesRead == 0) {
        Console.WriteLine("No data received from client.");
        return;
    }

    string request = Encoding.UTF8.GetString(requestBuffer);

    // GET /index.html HTTP/1.1\r\nHost: localhost:4221\r\nUser-Agent: curl/7.64.1\r\nAccept: */*\r\n\r\n
    string[] requestParams = request.Split("\r\n");

    // GET /index.html HTTP/1.1\ -> /index.html
    string address = requestParams[0].Split(" ")[1];

    StringBuilder response;

    if ((address[0] is '/' && endpoints.Contains(address[1..])) || address is "/" or null)
        response = ResponseBuilder("200", "");
    else if (address.StartsWith("/echo")) {
        string randomString = address.Split("/")[2];
        response = ResponseBuilder("200", randomString);
    }
    else if (address.StartsWith("/user-agent")) {
        string userAgentParam = requestParams[1];
        string userAgent = userAgentParam.Split(" ")[1];
        response = ResponseBuilder("200", userAgent);
    }
    else
        response = ResponseBuilder("404", "Not Found");

    byte[] responseData = Encoding.UTF8.GetBytes(response.ToString());
    await stream.WriteAsync(responseData);

    client.Close();
    Console.WriteLine("Client disconnected.");
}

StringBuilder ResponseBuilder(string statusCode, string content) {
    StringBuilder responseString = new();

    if (statusCode == "404")
        return responseString.Append($"HTTP/1.1 {statusCode} Not Found\r\n\r\n");

    responseString.Append($"HTTP/1.1 {statusCode} OK\r\n");
    responseString.Append("Content-Type: text/plain\r\n");
    responseString.Append($"Content-Length: {content.Length}\r\n\r\n");
    responseString.Append($"{content}");

    return responseString;
}