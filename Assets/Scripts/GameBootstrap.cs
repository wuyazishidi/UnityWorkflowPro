using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using Game.Services;

namespace Game
{
    /// <summary>
    /// VContainer 入口点（构造注入）。用 IStartable（始终可用，不依赖 UniTask 集成宏），
    /// 并在 Start 中以 UniTask 演示异步初始化（fire-and-forget）。
    /// </summary>
    public class GameBootstrap : IStartable
    {
        private readonly IGreetingService _greeting;

        public GameBootstrap(IGreetingService greeting)
        {
            _greeting = greeting;
        }

        public void Start()
        {
            InitAsync().Forget();
        }

        private async UniTaskVoid InitAsync()
        {
            // 演示异步初始化点：让出一帧（实际项目可在此 await 资源加载/网络等）
            await UniTask.Yield();
            Debug.Log(_greeting.Compose("UnityWorkflowPro"));
        }
    }
}
