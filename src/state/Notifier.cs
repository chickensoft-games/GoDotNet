namespace GoDotNet;
using System;

/// <summary>
/// An object which stores a value and emits an event whenever the value
/// changes.
/// </summary>
/// <typeparam name="TData">Type of data emitted.</typeparam>
public sealed class Notifier<TData> {

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
    Value = initialValue;
    Announce(default);
  }

  /// <summary>
  /// Event emitted when the current notifier value has changed.
  /// </summary>
  public event Changed? OnChanged;

  /// <summary>Current notifier value.</summary>
  public TData Value { get; private set; }

  /// <summary>
  /// Updates the notifier value. Any listeners will be called with the
  /// new and previous values if the new value is not equal to the old value
  /// (as determined by Object.Equals).
  /// </summary>
  /// <param name="value">New value of the notifier.</param>
  public void Update(TData value) {
    var hasChanged = !Object.Equals(Value, value);
    var previous = Value;
    Value = value;
    if (hasChanged) {
      Announce(previous);
    }
  }

  private void Announce(TData? previous)
    => OnChanged?.Invoke(Value, previous);
}
