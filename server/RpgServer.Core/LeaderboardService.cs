namespace RpgServer.Core;

public record ScoreEntry(string Name, int Score);

/// <summary>排行榜：内存累积分数，Top-N 排序逻辑抽为纯静态方法便于单测。</summary>
public class LeaderboardService
{
    private readonly List<ScoreEntry> _scores = new();

    public void Add(string name, int score) => _scores.Add(new ScoreEntry(name, score));

    public IReadOnlyList<ScoreEntry> Top(int n) => TopOf(_scores, n);

    /// <summary>纯函数：按分数降序、同分按名字升序，取前 n 个。</summary>
    public static List<ScoreEntry> TopOf(IEnumerable<ScoreEntry> all, int n)
    {
        if (n < 0) n = 0;
        return all.OrderByDescending(s => s.Score)
                  .ThenBy(s => s.Name, StringComparer.Ordinal)
                  .Take(n)
                  .ToList();
    }
}
