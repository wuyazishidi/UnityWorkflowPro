namespace Game.Services
{
    /// <summary>
    /// IGreetingService 的默认实现：纯逻辑，无 Unity 依赖，可直接 new 出来单元测试。
    /// 空/空白名回退为 "World"，并去除首尾空白。
    /// </summary>
    public class GreetingService : IGreetingService
    {
        public string Compose(string name)
        {
            string who = string.IsNullOrWhiteSpace(name) ? "World" : name.Trim();
            return $"Hello, {who}!";
        }
    }
}
