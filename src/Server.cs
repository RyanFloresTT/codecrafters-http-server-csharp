using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new (IPAddress.Any, 4221);
server.Start();
server.AcceptSocket(); // wait for client

TcpClient client = server.AcceptTcpClient();

const string httpVersion = "HTTP/1.1";

const int successStatusCode = 200;

string body = string.Empty;

string finalMessage = $"{httpVersion}\n{successStatusCode}\n{body}";

client.Client.Send(Encoding.ASCII.GetBytes(finalMessage));
client.Close();