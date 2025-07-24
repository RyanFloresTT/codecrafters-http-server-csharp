using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new (IPAddress.Any, 4221);
server.Start();
Socket socket = server.AcceptSocket(); // wait for client

const string httpVersion = "HTTP/1.1";

const int successStatusCode = 200;

string body = string.Empty;

string finalMessage = $"{httpVersion}\n{successStatusCode}\n{body}";

socket.Send(Encoding.ASCII.GetBytes(finalMessage));
socket.Close();