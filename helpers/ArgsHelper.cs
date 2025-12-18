namespace codecrafters_http_server.helpers;

public static class ArgsHelper {
    public static string? ParseDirectoryFlag(string?[] args) {
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--directory" && i + 1 < args.Length)
                return args[i + 1];
        return null;
    }
}