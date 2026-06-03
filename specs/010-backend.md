# 010 — M7：轻量后端（云存档 + 排行榜）

- 状态：已确认（RPG-MVP-PLAN M7）

## 目标
一个可本地运行的后端，提供云存档与排行榜 API；Unity 客户端可调用。

## 范围
- 后端 `server/`（ASP.NET Core Minimal API, net9.0, 零额外 NuGet 包）：
  - `RpgServer.Core`：`LeaderboardService`(Top-N 纯函数) + `JsonFileStore`(id 防穿越)。
  - `RpgServer`：端点 `GET /health`、`POST /save/{id}`、`GET /save/{id}`、`POST /score`、`GET /leaderboard?top=`。
  - `RpgServer.Tests`：零依赖控制台测试运行器（排行榜/清洗/存档往返）。
- Unity 客户端 `BackendClient`（UnityWebRequest + UniTask）：PostScore/GetLeaderboard/CloudSave；JSON 构造为纯函数。
- 不做：账号鉴权、数据库(用 JSON 文件)、排行榜持久化(内存，重启清空)。

## 验收
- [x] 后端 `dotnet build` 0 错误；`server/test.ps1` → RESULT: PASS（9 项）
- [ ] Unity 编译 Success；EditMode 含 BackendClient JSON 用例
- [ ] DoD DONE
- [ ] （人工 e2e）`dotnet run --project server/RpgServer` 起服务，客户端 PostScore/GetLeaderboard 成功

## 运行后端
```powershell
dotnet run --project server/RpgServer    # 默认 http://localhost:5000
```
> 客户端经本机代理时，UnityWebRequest 对 127.0.0.1 一般直连；若被代理影响，参照已知代理坑处理。
