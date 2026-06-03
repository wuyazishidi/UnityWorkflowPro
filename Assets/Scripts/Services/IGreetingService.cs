namespace Game.Services
{
    /// <summary>
    /// 可注入的问候服务接口。纯逻辑、不依赖 Unity，便于单元测试与替换实现。
    /// </summary>
    public interface IGreetingService
    {
        string Compose(string name);
    }
}
