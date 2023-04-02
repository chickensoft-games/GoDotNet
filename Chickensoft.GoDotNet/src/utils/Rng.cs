namespace Chickensoft.GoDotNet;

/// <summary>
/// A random number generator that wraps <see cref="System.Random"/> and
/// provides additional convenience functions for working with floats, doubles,
/// and other types.
/// </summary>
public interface IRng {
  /// <summary>
  /// Seed used for the random number generator.
  /// </summary>
  int Seed { get; }
  /// <summary>
  /// Random number generator used to produce random numbers.
  /// </summary>
  System.Random Generator { get; }
  /// <summary>
  /// Returns a non-negative random integer.
  /// </summary>
  /// <returns>A 32-bit signed integer that is greater than or equal to 0 and
  /// less than int.MaxValue</returns>
  int NextInt();
  /// <summary>
  /// Returns a random floating-point number that is greater than or equal to
  /// 0.0, and less than 1.0.
  /// </summary>
  float NextFloat();
  /// <summary>
  /// Returns a random floating-point number that is greater than or equal to
  /// 0.0, and less than 1.0.
  /// </summary>
  double NextDouble();
  /// <summary>
  /// Generates a random integer between min (inclusive) and max (exclusive).
  /// </summary>
  /// <param name="min">Minimum value (inclusive).</param>
  /// <param name="max">Maximum value (exclusive).</param>
  /// <returns>An integer between [min, max)</returns>
  int RangeInt(int min, int max);
  /// <summary>
  /// Generates a random float between min (inclusive) and max (exclusive).
  /// </summary>
  /// <param name="min">Minimum value (inclusive).</param>
  /// <param name="max">Maximum value (exclusive).</param>
  /// <returns>A float between [min, max)</returns>
  float RangeFloat(float min, float max);
  /// <summary>
  /// Generates a random double between min (inclusive) and max (exclusive).
  /// </summary>
  /// <param name="min">Minimum value (inclusive).</param>
  /// <param name="max">Maximum value (exclusive).</param>
  /// <returns>A double between [min, max)</returns>
  double RangeDouble(double min, double max);
}

/// <summary>
/// Random number generator default implementation.
/// </summary>
public record Rng : IRng {
  /// <inheritdoc/>
  public int Seed { get; init; }
  /// <inheritdoc/>
  public System.Random Generator { get; private set; }

  /// <summary>
  /// Creates a new random number generator with the current value of
  /// <see cref="System.Environment.TickCount"/> as the seed.
  /// </summary>
  public Rng() {
    Seed = System.Environment.TickCount;
    Generator = new System.Random(Seed);
  }

  /// <summary>
  /// Creates a new random number generator with the given seed.
  /// </summary>
  /// <param name="seed">Random number generator seed.</param>
  public Rng(int seed) {
    Generator = new System.Random(seed);
    Seed = seed;
  }

  /// <summary>
  /// Creates a new random number generator with the given seed and generator.
  /// </summary>
  /// <param name="seed">Random number generator seed.</param>
  /// <param name="generator">Random number generator.</param>
  public Rng(int seed, System.Random generator) {
    Generator = generator;
    Seed = seed;
  }

  /// <inheritdoc/>
  public int NextInt() => Generator.Next();

  /// <inheritdoc/>
  public float NextFloat() => (float)Generator.NextDouble();

  /// <inheritdoc/>
  public double NextDouble() => Generator.NextDouble();

  /// <inheritdoc/>
  public int RangeInt(int min, int max) => Generator.Next(min, max);

  /// <inheritdoc/>
  public float RangeFloat(float min, float max)
    => (float)(
      (Generator.NextDouble() * ((double)max - (double)min)) + (double)min
    );

  /// <inheritdoc/>
  public double RangeDouble(double min, double max)
    => (Generator.NextDouble() * (max - min)) + min;
}
