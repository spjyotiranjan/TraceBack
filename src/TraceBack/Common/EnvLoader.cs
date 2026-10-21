namespace TraceBack.Common;

/// <summary>
/// Lightweight loader for .env environment configuration files.
/// Searches current and parent directory hierarchy and injects key-value pairs into the process environment.
/// </summary>
public static class EnvLoader
{
    public static void Load(string? explicitPath = null)
    {
        var envPath = ResolveEnvPath(explicitPath);
        if (string.IsNullOrWhiteSpace(envPath) || !File.Exists(envPath))
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(envPath))
        {
            var line = rawLine.Trim();

            // Skip empty lines and comment lines
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#') || line.StartsWith("//"))
            {
                continue;
            }

            var delimiterIndex = line.IndexOf('=');
            if (delimiterIndex <= 0)
            {
                continue;
            }

            var key = line[..delimiterIndex].Trim();
            var value = line[(delimiterIndex + 1)..].Trim();

            // Strip surrounding double or single quotes if present
            if (value.Length >= 2 &&
                ((value.StartsWith('"') && value.EndsWith('"')) ||
                 (value.StartsWith('\'') && value.EndsWith('\''))))
            {
                value = value[1..^1];
            }

            if (!string.IsNullOrWhiteSpace(key))
            {
                // Set into process environment variable
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private static string? ResolveEnvPath(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath))
        {
            return Path.GetFullPath(explicitPath);
        }

        var candidatePaths = new List<string>
        {
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(AppContext.BaseDirectory, ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "TraceBack", ".env")
        };

        // Traverse upwards from current directory to repository root
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (var i = 0; i < 4 && dir is not null; i++)
        {
            candidatePaths.Add(Path.Combine(dir.FullName, ".env"));
            dir = dir.Parent;
        }

        return candidatePaths.FirstOrDefault(File.Exists);
    }
}

