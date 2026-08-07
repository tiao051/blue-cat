namespace DailyTracker.Api;

/// <summary>
/// Nạp file .env vào environment variables của process (biến đã có sẵn thì giữ nguyên —
/// env thật luôn thắng file). Đủ dùng cho local dev; trên server dùng env vars của compose.
/// </summary>
public static class EnvFile
{
    public static void Load(string path)
    {
        if (!File.Exists(path)) return;

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            var idx = line.IndexOf('=');
            if (idx <= 0) continue;

            var key = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim().Trim('"');

            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, value);
        }
    }
}
