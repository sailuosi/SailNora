namespace SailNora.Foundation.DI;

/// <summary>
/// 标记类以 Transient 生命周期注册。可由基类继承。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class TransientAttribute : Attribute;
