using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace Game.Net
{
    /// <summary>
    /// 后端客户端：提交分数、拉取排行榜。JSON 构造抽为纯函数便于 EditMode 单测。
    /// 默认对接本地 RpgServer。
    /// </summary>
    public class BackendClient
    {
        private readonly string _baseUrl;

        public BackendClient(string baseUrl = "http://127.0.0.1:5000")
        {
            _baseUrl = baseUrl.TrimEnd('/');
        }

        /// <summary>纯函数：构造 {"name":..,"score":..} JSON（名字做转义）。</summary>
        public static string BuildScoreJson(string name, int score)
        {
            return "{\"name\":" + Quote(name) + ",\"score\":" + score + "}";
        }

        private static string Quote(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in s ?? "")
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            sb.Append("\"");
            return sb.ToString();
        }

        public async UniTask<bool> PostScoreAsync(string name, int score)
        {
            using var req = new UnityWebRequest(_baseUrl + "/score", "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(BuildScoreJson(name, score)));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            await req.SendWebRequest();
            return req.result == UnityWebRequest.Result.Success;
        }

        public async UniTask<string> GetLeaderboardAsync(int top = 10)
        {
            using var req = UnityWebRequest.Get(_baseUrl + "/leaderboard?top=" + top);
            await req.SendWebRequest();
            return req.result == UnityWebRequest.Result.Success ? req.downloadHandler.text : null;
        }

        public async UniTask<bool> CloudSaveAsync(string id, string json)
        {
            using var req = new UnityWebRequest(_baseUrl + "/save/" + id, "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            await req.SendWebRequest();
            return req.result == UnityWebRequest.Result.Success;
        }
    }
}
