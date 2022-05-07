namespace GoDotNet {
  using System;

  /// <summary>
  /// An object which stores a value and emits an event whenever the value
  /// changes.
  /// </summary>
  /// <typeparam name="TData">Type of data emitted.</typeparam>
  public sealed class Notifier<TData> {
    /// <summary>Internal value.</summary>
    private TData _data;

    /// <summary>
    /// Signature of the event handler for when the value changes. The notifier
    /// and the previous event are passed in as arguments to make comparing
    /// state changes simpler.
    /// </summary>
    /// <param name="current">The new value.</param>
    /// <param name="previous">The previous value.</param>
    public delegate void Changed(TData current, TData? previous);

    /// <summary>
    /// Creates a new notifier with the given initial value and optional
    /// event handler.
    /// </summary>
    /// <param name="initialValue">Initial value (should not be null).</param>
    /// <param name="onChanged">Event handler, if any.</param>
    public Notifier(TData initialValue, Changed? onChanged = null) {
      if (onChanged != null) { OnChanged += onChanged; }
      _data = initialValue;
      Announce(default);
    }

    /// <summary>
    /// Event emitted when the current notifier value has changed.
    /// </summary>
    public event Changed? OnChanged;

    /// <summary>Current notifier value.</summary>
    public TData Value {
      get => _data;
      set => Update(_data, value);
    }

    private void Update(TData current, TData value) {
      var hasChanged = !Object.Equals(current, value);
      var previous = current;
      _data = value;
      if (hasChanged) {
        Announce(previous);
      }
    }

    private void Announce(TData? previous)
      => OnChanged?.Invoke(Value, previous);
  }
}
