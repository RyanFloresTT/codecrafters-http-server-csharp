using System.Net;
using System.Net.Sockets;
using codecrafters_http_server;
using codecrafters_http_server.helpers;

TcpListener server = new(IPAddress.Any, 4221);

// Start the server and accept multiple clients concurrently
try {
    server.Start();
    Console.WriteLine("Server started at localhost:4221.");

    string? baseDirectory = ArgsHelper.ParseDirectoryFlag(args);

    while (true) {
        TcpClient client = await server.AcceptTcpClientAsync();
        Console.WriteLine("Client connected!");

        _ = Task.Run(async () => {
            HttpRequestHandler handler = new();
            try {
                await handler.Serve(client, baseDirectory);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error while serving a client: {ex.Message}");
            }
            finally {
                client.Close();
                Console.WriteLine("Client disconnected.");
            }
        });
    }
}
catch (Exception e) {
    Console.WriteLine($"Server error: {e.Message}");
}

server.Dispose();