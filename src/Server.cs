using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new (IPAddress.Any, 4221);
server.Start();
server.AcceptSocket(); // wait for client

TcpClient client = server.AcceptTcpClient();

string message = "HTTP/1.1 200 OK\r\n\r\n";

client.Client.Send(Encoding.ASCII.GetBytes(message));