namespace Chickensoft.GoDotNet;

using System;
using System.Collections.Generic;

/// <summary>
/// Exception thrown when attempting to transition between states
/// that are incompatible.
/// </summary>
/// <typeparam name="TState">Machine state.</typeparam>
public class InvalidStateTransitionException<TState> : Exception {
  /// <summary>Current state.</summary>
  public TState Current;
  /// <summary>Attempted next state which was invalid.</summary>
  public TState Desired;

  /// <summary>
  /// Creates a new invalid state transition exception.
  /// </summary>
  /// <param name="current">Current state.</param>
  /// <param name="desired">Attempted next state which was invalid.</param>
  public InvalidStateTransitionException(TState current, TState desired) :
    base($"Invalid state transition between ${current} and ${desired}.") {
    Current = current;
    Desired = desired;
  }
}

/// <summary>
/// <para>A record type that all machine states must inherit from.</para>
/// <para>
/// Because records are reference types with value-based equality, states
/// can be compared easily by the state machine.
/// </para>
/// <para>
/// All state types must implement `CanTransitionTo` which returns true
/// if a given state can be transitioned to from the current state.
/// </para>
/// </summary>
/// <typeparam name="TState">Machine state type.</typeparam>
public interface IMachineState<TState> {
  /// <summary>
  /// Determines whether the given state can be transitioned to from the
  /// current state.
  /// </summary>
  /// <param name="state">The requested next state.</param>
  /// <returns>True to allow the state transition, false to prevent.</returns>
  bool CanTransitionTo(TState state) => true;
}

/// <summary>
/// Read-only interface for a machine. Expose machines as this interface
/// when you want to allow them to be observed and read, but not updated.
/// </summary>
/// <typeparam name="TState"></typeparam>
public interface IReadOnlyMachine<TState> where TState : IMachineState<TState> {
  /// <summary>
  /// Event handler for when the machine's state changes.
  /// </summary>
  /// <param name="state">The new state of the machine.</param>
  delegate void Changed(TState state);

  /// <summary>Event emitted when the machine's state changes.</summary>
  event Changed? OnChanged;

  /// <summary>The current state of the machine.</summary>
  TState State { get; }
}

/// <summary>
/// <para>
/// A simple implementation of a state machine. Events an emit when the state
/// is changed.
/// </para>
/// <para>
/// Not intended to be subclassed — instead, use instances of this in a
/// compositional pattern.
/// </para>
/// <para>
/// States can implement `CanTransitionTo` to prevent transitions to invalid
/// states.
/// </para>
/// </summary>
/// <typeparam name="TState">Type of state used by the machine.</typeparam>
public sealed class Machine<TState> : IReadOnlyMachine<TState>
  where TState : IMachineState<TState> {
  /// <summary>
  /// Creates a new machine with the given initial state.
  /// </summary>
  /// <param name="state">Initial state of the machine.</param>
  /// <param name="onChanged"></param>
  public Machine(
    TState state,
    IReadOnlyMachine<TState>.Changed? onChanged = null
  ) {
    State = state;
    if (onChanged != null) { OnChanged += onChanged; }
    Announce();
  }

  /// <inheritdoc/>
  public TState State { get; private set; }

  /// <inheritdoc/>
  public event IReadOnlyMachine<TState>.Changed? OnChanged;

  /// <summary>
  /// Whether we're currently in the process of changing the state (or not).
  /// </summary>
  public bool IsBusy { get; private set; }

  private Queue<TState> PendingTransitions { get; set; }
    = new Queue<TState>();

  /// <summary>
  /// Adds a value to the queue of pending transitions. If the next state
  /// is equivalent to the current state, the state will not be changed.
  /// If the next state cannot be transitioned to from the current state,
  /// the state will not be changed and a warning will be issued before
  /// attempting to transition to any subsequent queued states.
  /// </summary>
  /// <param name="value">State for the machine to transition to.</param>
  /// <exception cref="InvalidStateTransitionException{TState}"></exception>
  public void Update(TState value) {
    // Because machine state can be updated when firing state events from
    // previous state updates, we need to make sure we don't allow another
    // announce loop to begin while we're already announcing state updates.
    //
    // Instead, we just make sure we add the transition to the list of
    // pending transitions. State machines are guaranteed to enter each state
    // requested in the order they are requested (or throw an error if the
    // requested sequence is not comprised of valid transitions).
    PendingTransitions.Enqueue(value);

    if (IsBusy) {
      return;
    }

    IsBusy = true;

    while (PendingTransitions.Count > 0) {
      var state = PendingTransitions.Dequeue();
      if (State.Equals(state)) {
        continue;
      }

      if (State.CanTransitionTo(state)) {
        State = state;
        Announce();
      }
      else {
        IsBusy = false;
        throw new InvalidStateTransitionException<TState>(State, state);
      }
    }

    IsBusy = false;
  }

  /// <summary>
  /// Announces the current state to any listeners.
  /// <see cref="Update"/> calls this automatically if the new state is
  /// different from the previous state.
  /// <br />
  /// Call this whenever you want to force a re-announcement.
  /// </summary>
  public void Announce() => OnChanged?.Invoke(State);
}
