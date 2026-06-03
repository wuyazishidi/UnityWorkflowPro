namespace RpgServer.Core;

/// <summary>云存档存储：每个存档 id 一个 JSON 文件。id 清洗为纯函数，防路径穿越。</summary>
public class JsonFileStore
{
    private readonly string _dir;

    public JsonFileStore(string dir)
    {
        _dir = dir;
        Directory.CreateDirectory(dir);
    }

    public void Put(string id, string json) => File.WriteAllText(PathFor(id), json);

    public string? Get(string id)
    {
        var p = PathFor(id);
        return File.Exists(p) ? File.ReadAllText(p) : null;
    }

    private string PathFor(string id) => Path.Combine(_dir, SanitizedId(id) + ".json");

    /// <summary>纯函数：只保留字母数字与 - _，剔除路径分隔等危险字符。</summary>
    public static string SanitizedId(string id) =>
        string.Concat((id ?? "").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
}
