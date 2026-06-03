using VContainer;
using VContainer.Unity;
using Game.Services;

namespace Game
{
    /// <summary>
    /// VContainer 组合根（DI 容器）。把它挂到场景中的一个 GameObject 上即可生效。
    /// 注册可注入服务与入口点；MonoBehaviour 仅作组合根，不含业务逻辑。
    /// </summary>
    public class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<IGreetingService, GreetingService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<GameBootstrap>();
        }
    }
}
