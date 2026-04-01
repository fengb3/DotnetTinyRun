using UnityGameLib.Core;
using UnityGameLib.Math;

namespace UnityGameLib.Core;

/// <summary>Unity-style GameObject — a named entity with a Transform and optional components.</summary>
public class GameObject
{
    public string Name { get; set; }
    public bool IsActive { get; set; } = true;
    public Transform Transform { get; } = new();
    private readonly Dictionary<Type, object> _components = new();

    public GameObject(string name) => Name = name;

    public T AddComponent<T>() where T : new()
    {
        var component = new T();
        _components[typeof(T)] = component;
        return component;
    }

    public T? GetComponent<T>() =>
        _components.TryGetValue(typeof(T), out var c) ? (T)c : default;

    public bool HasComponent<T>() => _components.ContainsKey(typeof(T));

    public override string ToString() =>
        $"GameObject '{Name}' [active={IsActive}] at {Transform.Position}";
}
