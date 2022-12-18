namespace GoDotNet;
using System;

/// <summary>
/// Read-only interface for a notifier. Expose notifiers as this interface
/// when you want to allow them to be observed and read, but not updated.
/// </summary>
public interface IReadOnlyNotifier<TData> {
  /// <summary>
  /// Signature of the event handler for when the value changes. The current
  /// and previous values are provided to allow listeners to react to changes.
  /// </summary>
  /// <param name="current">Current value of the notifier.</param>
  /// <param name="previous">Previous value of the notifier.</param>
  delegate void Changed(TData current, TData? previous);

  /// <summary>
  /// Signature of the event handler for when the value changes.
  /// </summary>
  /// <param name="value">Current value of the notifier.</param>
  delegate void Updated(TData value);

  /// <summary>
  /// Event emitted when the current value of the notifier has changed.
  /// Event listeners will receive both the current and previous values.
  /// </summary>
  event Changed? OnChanged;

  /// <summary>
  /// Event emitted when the current value of the notifier has changed.
  /// Event listeners will receive only the current value. Subscribe to
  /// <see cref="OnChanged"/> if you need both the current and previous values.
  /// </summary>
  event Updated? OnUpdated;

  /// <summary>Current notifier value.</summary>
  TData Value { get; }
}

/// <summary>
/// An object which stores a value and emits an event whenever the value
/// changes.
/// </summary>
/// <typeparam name="TData">Type of data emitted.</typeparam>
public sealed class Notifier<TData> : IReadOnlyNotifier<TData> {
  /// <summary>
  /// Creates a new notifier with the given initial value and optional
  /// event handler.
  /// </summary>
  /// <param name="initialValue">Initial value (should not be null).</param>
  /// <param name="onChanged">Event handler, if any.</param>
  public Notifier(
    TData initialValue,
    IReadOnlyNotifier<TData>.Changed? onChanged = null
  ) {
    if (onChanged != null) { OnChanged += onChanged; }
    Value = initialValue;
    Previous = default;
    Announce();
  }

  /// <summary>
  /// Previous value of the notifier, if any. This is `default` when the
  /// notifier has just been created.
  /// </summary>
  public TData? Previous { get; private set; }

  /// <inheritdoc/>
  public TData Value { get; private set; }

  /// <inheritdoc/>
  public event IReadOnlyNotifier<TData>.Changed? OnChanged;

  /// <inheritdoc/>
  public event IReadOnlyNotifier<TData>.Updated? OnUpdated;

  /// <summary>
  /// Updates the notifier value. Any listeners will be called with the
  /// new and previous values if the new value is not equal to the old value
  /// (as determined by Object.Equals).
  /// </summary>
  /// <param name="value">New value of the notifier.</param>
  public void Update(TData value) {
    if (Object.Equals(Value, value)) { return; }
    Previous = Value;
    Value = value;
    Announce();
  }

  /// <summary>
  /// Announces the current and previous values to any listeners.
  /// <see cref="Update"/> calls this automatically if the new value is
  /// different from the previous value.
  /// <br />
  /// Call this whenever you want to force a re-announcement.
  /// </summary>
  public void Announce() {
    OnChanged?.Invoke(Value, Previous);
    OnUpdated?.Invoke(Value);
  }
}
