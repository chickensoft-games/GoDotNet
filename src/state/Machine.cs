namespace GoDotNet {
  using System;
  using System.Collections.Generic;

  /// <summary>
  /// Exception thrown when attempting to transition between states
  /// that are incompatible.
  /// </summary>
  public class InvalidStateTransition<TState> : Exception {
    /// <summary>Current state.</summary>
    public TState Current;
    /// <summary>Attempted next state which was invalid.</summary>
    public TState Desired;

    /// <summary>
    /// Creates a new invalid state transition exception.
    /// </summary>
    /// <param name="current">Current state.</param>
    /// <param name="desired">Attempted next state which was invalid.</param>
    /// <returns></returns>
    public InvalidStateTransition(TState current, TState desired) :
      base($"Invalid state transition between ${current} and ${desired}.") {
      Current = current;
      Desired = desired;
    }
  }

  /// <summary>
  /// A record type that all machine states must inherit from.
  ///
  /// Because records are reference types with value-based equality, states
  /// can be compared easily by the state machine.
  ///
  /// All state types must implement `CanTransitionTo` which returns true
  /// if a given state can be transitioned to from the current state.
  /// </summary>
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
  /// A simple implementation of a state machine. Events an emit when the state
  /// is changed.
  ///
  /// Not intended to be subclassed — instead, use instances of this in a
  /// compositional pattern.
  ///
  /// States can implement `CanTransitionTo` to prevent transitions to invalid
  /// states.
  /// </summary>
  /// <typeparam name="TState">Type of state used by the machine.</typeparam>
  public sealed class Machine<TState> where TState : IMachineState<TState> {
    /// <summary>
    /// The current state of the machine.
    /// </summary>
    public TState State { get; private set; }

    /// <summary>
    /// Event handler for when the machine's state changes.
    /// </summary>
    /// <param name="state">The new state of the machine.</param>
    public delegate void Changed(TState state);

    /// <summary>Event emitted when the machine's state changes.</summary>
    public event Changed? OnChanged;

    private Queue<TState> _pendingTransitions { get; set; }
      = new Queue<TState>();

    /// <summary>
    /// Whether we're currently in the process of changing the state (or not).
    /// </summary>
    public bool IsBusy { get; private set; }

    /// <summary>
    /// Creates a new machine with the given initial state.
    /// </summary>
    /// <param name="state">Initial state of the machine.</param>
    /// <param name="onChanged"></param>
    public Machine(TState state, Changed? onChanged = null) {
      State = state;
      if (onChanged != null) { OnChanged += onChanged; }
      Announce();
    }

    private void Announce() => OnChanged?.Invoke(State);

    /// <summary>
    /// Adds a value to the queue of pending transitions. If the next state
    /// is equivalent to the current state, the state will not be changed.
    /// If the next state cannot be transitioned to from the current state,
    /// the state will not be changed and a warning will be issued before
    /// attempting to transition to any subsequent queued states.
    /// </summary>
    /// <param name="value">State for the machine to transition to.</param>
    public void Update(TState value) {
      // Because machine state can be updated when firing state events from
      // previous state updates, we need to make sure we don't allow another
      // announce loop to begin while we're already announcing state updates.
      //
      // Instead, we just make sure we add the transition to the list of
      // pending transitions. State machines are guaranteed to enter each state
      // requested in the order they are requested (or throw an error if the
      // requested sequence is not comprised of valid transitions).
      _pendingTransitions.Enqueue(value);

      if (IsBusy) {
        return;
      }

      IsBusy = true;

      while (_pendingTransitions.Count > 0) {
        var state = _pendingTransitions.Dequeue();
        if (State.Equals(state)) {
          continue;
        }

        if (State.CanTransitionTo(state)) {
          State = state;
          Announce();
        }
        else {
          IsBusy = false;
          throw new InvalidStateTransition<TState>(State, state);
        }
      }

      IsBusy = false;
    }
  }
}
