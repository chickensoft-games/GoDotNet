namespace GoDotNet {
  using System;

  [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
  public class DependencyAttribute : Attribute { }
}
