using System.Net;
using System.Net.Sockets;
using codecrafters_http_server;

TcpListener server = new(IPAddress.Any, 4221);

try {
    server.Start();
    Console.WriteLine("Server started");
    HttpRequestHandler handler = new();

    while (true) {
        TcpClient client = server.AcceptTcpClient();
        Console.WriteLine("Client connected!");

        await Task.Run(() => handler.Serve(client));
    }
}
catch (Exception e) {
    Console.WriteLine($"Error {e} occurred");
}

server.Dispose();