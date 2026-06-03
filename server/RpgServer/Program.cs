using RpgServer.Core;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var store = new JsonFileStore(Path.Combine(AppContext.BaseDirectory, "data", "saves"));
var board = new LeaderboardService();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// 云存档：写入任意 JSON 字符串，按 id 取回
app.MapPost("/save/{id}", async (string id, HttpRequest req) =>
{
    using var reader = new StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();
    store.Put(id, body);
    return Results.Ok(new { saved = id });
});

app.MapGet("/save/{id}", (string id) =>
{
    var data = store.Get(id);
    return data is null ? Results.NotFound() : Results.Content(data, "application/json");
});

// 排行榜
app.MapPost("/score", (ScoreEntry entry) =>
{
    board.Add(entry.Name, entry.Score);
    return Results.Ok(new { entry.Name, entry.Score });
});

app.MapGet("/leaderboard", (int? top) => Results.Ok(board.Top(top ?? 10)));

app.Run();
