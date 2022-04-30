namespace GoDotNet {
  using System;
  using System.Collections.Generic;

  /// <summary>
  /// Essentially a typedef for a with specific values dictionary.
  /// </summary>
  public class Dependencies : Dictionary<Type, IDependency> { }

  /// <summary>
  /// Represents a node which can depend on values provided by other nodes.
  /// </summary>
  public interface IDependent {
    /// <summary>
    /// A dictionary of dependencies, keyed by dependency type. Dependent nodes
    /// simply need to implement this as follows:
    ///
    /// <code>public Dependencies Deps { get; } = new();</code>
    ///
    /// This dictionary of dependencies is automatically managed by the
    /// dependency system.
    /// </summary>
    /// <value></value>
    public Dependencies Deps { get; }
  }
}
