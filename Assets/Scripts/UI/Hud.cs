using Cysharp.Threading.Tasks;
using UnityEngine;
using Game.Combat;
using Game.Core;
using Game.Items;
using Game.Net;

namespace Game.UI
{
    /// <summary>
    /// IMGUI HUD：显示 HP/物品/得分/操作提示，并提供 F7 提交分数、F8 拉取排行榜。
    /// 用 OnGUI 免 Canvas 布线，按 Play 即显示。
    /// </summary>
    public class Hud : MonoBehaviour
    {
        private Health _hp;
        private InventoryHolder _inv;
        private ScoreKeeper _score;
        private BackendClient _client;
        private string _status = "F7 交分 / F8 排行榜";

        public void Bind(Health hp, InventoryHolder inv, ScoreKeeper score)
        {
            _hp = hp;
            _inv = inv;
            _score = score;
            _client = new BackendClient();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F7)) SubmitScore().Forget();
            if (Input.GetKeyDown(KeyCode.F8)) RefreshBoard().Forget();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200), GUI.skin.box);
            if (_hp != null) GUILayout.Label(HudFormat.HealthText(_hp.Current, _hp.Max));
            if (_inv != null) GUILayout.Label($"物品: {_inv.Inventory.Total}");
            if (_score != null) GUILayout.Label($"得分: {_score.Score}");
            GUILayout.Label("WASD 移动 / 空格攻击 / F5 存 F9 读");
            GUILayout.Label(_status);
            GUILayout.EndArea();
        }

        private async UniTaskVoid SubmitScore()
        {
            int s = _score != null ? _score.Score : 0;
            bool ok = await _client.PostScoreAsync("player", s);
            _status = ok ? $"已提交分数 {s}" : "提交失败（后端未启动?）";
        }

        private async UniTaskVoid RefreshBoard()
        {
            string board = await _client.GetLeaderboardAsync(5);
            _status = board ?? "排行榜获取失败（后端未启动?）";
        }
    }
}
