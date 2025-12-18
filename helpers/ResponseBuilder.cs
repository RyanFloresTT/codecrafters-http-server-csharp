using System.Text;

namespace codecrafters_http_server.helpers;

public class ResponseBuilder {
    readonly StringBuilder sb = new();
    string? content;
    string contentType = "text/plain";
    string? statusCode;

    public ResponseBuilder WithStatusCode(string sc) {
        statusCode = sc;
        return this;
    }

    public ResponseBuilder WithContent(string cnt) {
        content = cnt;
        return this;
    }

    public ResponseBuilder WithContentType(string ct) {
        contentType = ct;
        return this;
    }

    public StringBuilder Build() {
        if (statusCode == "404")
            return sb.Append($"HTTP/1.1 {statusCode} Not Found\r\n\r\n");

        if (statusCode != null) {
            string statusMessage = GetStatusMessage(statusCode);
            sb.Append($"HTTP/1.1 {statusCode} {statusMessage}\r\n");
        }

        if (content == null) return sb;

        sb.Append($"Content-Type: {contentType}\r\n");
        sb.Append($"Content-Length: {content.Length}\r\n\r\n");
        sb.Append($"{content}");

        return sb;
    }

    static string GetStatusMessage(string sc) => sc switch {
        "200" => "OK",
        "201" => "Created",
        "404" => "Not Found",
        "405" => "Method Not Allowed",
        _ => "Unknown Status Code"
    };
}