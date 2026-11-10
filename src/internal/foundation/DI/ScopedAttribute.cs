namespace SailNora.Foundation.DI;

/// <summary>
/// 标记类以 Scoped 生命周期注册（默认 Web 请求级）。可由基类继承。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class ScopedAttribute : Attribute;
