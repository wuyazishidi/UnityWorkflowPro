using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>简单计分器：击杀等事件加分。</summary>
    public class ScoreKeeper : MonoBehaviour
    {
        public int Score { get; private set; }
        public event Action<int> OnChanged;

        public void Add(int points)
        {
            if (points <= 0) return;
            Score += points;
            OnChanged?.Invoke(Score);
        }
    }
}
