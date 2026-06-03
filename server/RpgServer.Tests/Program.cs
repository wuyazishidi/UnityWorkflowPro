using RpgServer.Core;

// 零依赖的轻量测试运行器（避免引入 xUnit/NuGet 还原，规避代理风险）。退出码 0=PASS。
int failed = 0;
void Check(bool cond, string msg)
{
    Console.WriteLine((cond ? "ok  : " : "FAIL: ") + msg);
    if (!cond) failed++;
}

// 1) 排行榜 Top-N：降序 + 限量 + 同分按名字升序
var data = new[]
{
    new ScoreEntry("alice", 10),
    new ScoreEntry("bob", 30),
    new ScoreEntry("carl", 20),
    new ScoreEntry("amy", 30),
};
var top3 = LeaderboardService.TopOf(data, 3);
Check(top3.Count == 3, "TopOf 限量为 3");
Check(top3[0].Name == "amy" && top3[1].Name == "bob", "同分(30) 按名字升序: amy 在 bob 前");
Check(top3[2].Name == "carl", "第三名 carl(20)");
Check(LeaderboardService.TopOf(data, 0).Count == 0, "Top0 为空");

// 2) LeaderboardService 实例累积
var board = new LeaderboardService();
board.Add("x", 5);
board.Add("y", 99);
Check(board.Top(1)[0].Name == "y", "实例 Top1 = 最高分");

// 3) id 清洗防路径穿越
Check(JsonFileStore.SanitizedId("../../etc/passwd") == "etcpasswd", "SanitizedId 去除路径字符");
Check(JsonFileStore.SanitizedId("player_01-A") == "player_01-A", "保留字母数字-_");

// 4) 文件存储往返
var dir = Path.Combine(Path.GetTempPath(), "rpgsrv_test_" + Guid.NewGuid().ToString("N"));
var store = new JsonFileStore(dir);
store.Put("p1", "{\"hp\":50}");
Check(store.Get("p1") == "{\"hp\":50}", "存档往返一致");
Check(store.Get("missing") == null, "不存在返回 null");
try { Directory.Delete(dir, true); } catch { }

Console.WriteLine(failed == 0 ? "RESULT: PASS" : $"RESULT: FAIL ({failed})");
return failed == 0 ? 0 : 1;
