namespace GoDotNet {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Reflection;
  using System.Runtime.CompilerServices;
  using Fasterflect;
  using Godot;

  /// <summary>
  /// Essentially a typedef for a with specific values dictionary.
  /// </summary>
  public class Dependencies : Dictionary<Type, IDependency> { }

  /// <summary>
  /// Represents a node which can depend on values provided by other nodes.
  /// </summary>
  public interface IDependent {
    /// <summary>
    /// Method that is called when all of the node's dependencies are available
    /// from the providers that it depends on.
    ///
    /// For this method to be called, you must call
    /// <see cref="DependentX.Depend(IDependent)"/> from your dependent node's
    /// _Ready method.
    /// </summary>
    public void Loaded();
  }

  public static class DependentX {
    private static readonly ConditionalWeakTable<IDependent, Dependencies>
      _deps = new();

    private static Dependencies GetState(IDependent dependent) =>
      _deps.GetOrCreateValue(dependent);

    private static void SetState(IDependent dependent, Dependencies deps) =>
      _deps.AddOrUpdate(dependent, deps);

    public static Dependencies GetDeps(this IDependent dependent) =>
      GetState(dependent);

    public static void SetDeps(this IDependent dependent, Dependencies deps) =>
      SetState(dependent, deps);

    /// <summary>
    /// Begins the dependency resolution process for the given node by finding
    /// each provider that it depends on and listening to the provider's
    /// OnProvided event. For this to work, dependencies must be declared with
    /// the <see cref="DependencyAttribute"/> attribute.
    ///
    /// Once providers are looked up, they are cached for future usage.
    /// </summary>
    /// <param name="dependent"></param>
    public static void Depend(this IDependent dependent) {
      var iDependentType = typeof(IDependent);
      var currentType = dependent.GetType();

      // Clear dependency cache.
      dependent.SetDeps(new());

      if (dependent is Node node) {
        var properties = new List<PropertyInfo>();
        // Get all properties tagged with the DeppendencyAttribute.
        // We also have to search all the superclasses that implement
        // IDependent.
        while (currentType != null) {
          if (currentType.GetInterface(nameof(IDependent)) != null) {
            // use fasterflect to grab properties with attribute
            var typeProps = currentType.GetProperties(
              BindingFlags.Instance |
              BindingFlags.Public |
              BindingFlags.NonPublic
            ).Where(
              propertyInfo =>
                propertyInfo.GetCustomAttribute<DependencyAttribute>() != null
            ).ToList();
            properties.AddRange(typeProps);
          }
          currentType = currentType.BaseType;
        }

        if (properties.Count < 1) {
          throw new Exception(
            $"{currentType?.Name} does not have any dependencies. Make " +
            "sure dependencies use the [Dependency] attribute and invoke " +
            "this.DependOn<Value>()."
          );
        }

        var classType = typeof(DependentX);
        var getProviderMethod = classType.GetMethod(
          nameof(DependentX.GetProvider),
          BindingFlags.Static | BindingFlags.NonPublic
        );

        var numberOfPropertiesToDependOn = properties.Count;
        void onDependencyLoaded(IProviderNode provider) {
          numberOfPropertiesToDependOn--;
          if (numberOfPropertiesToDependOn == 0) {
            dependent.Loaded();
          }
          provider.StopListening(onDependencyLoaded);
        };

        foreach (var property in properties) {
          var type = property.PropertyType;

          // Use the fasterflect library to quickly invoke the static, generic
          // GetProvider method declared below. This is a lot faster than
          // invoking the method through the typical C# reflection layer.
          var providerObj = classType.CallMethod(
            new Type[] { type },
            nameof(DependentX.GetProvider),
            new object[] { node }
          );

          if (providerObj is IProviderNode provider) {
            if (provider.HasProvided()) {
              onDependencyLoaded(provider);
            }
            else {
              provider.Listen(onDependencyLoaded);
            }
          }
          else {
            throw new Exception("Unexpected provider type.");
          }
        }
      }
      else {
        throw new Exception("Dependent must be a node.");

      }
    }

    private static IProvider<T> GetProvider<T>(Node node) {
      if (node is IDependent dependent) {
        if (dependent.GetDeps().ContainsKey(typeof(T))) {
          if (dependent.GetDeps()[typeof(T)] is Dependency<T> dependency) {
            return dependency.ResolveProvider(node);
          }
          else {
            throw new Exception("Unexpected dependency provider type.");
          }
        }
        else {
          var dependency = new Dependency<T>();
          dependent.GetDeps().Add(typeof(T), dependency);
          return dependency.ResolveProvider(node);
        }
      }
      throw new Exception(
        "Nodes using dependencies need to implement Dependent."
      );
    }
  }
}
