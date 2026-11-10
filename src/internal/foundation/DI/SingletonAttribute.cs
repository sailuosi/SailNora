namespace SailNora.Foundation.DI;

/// <summary>
/// 标记类以 Singleton 生命周期注册。可由基类继承。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class SingletonAttribute : Attribute;
