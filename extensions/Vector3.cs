namespace GoDotNet {
  using Godot;

  /// <summary>
  /// Vector3 extensions.
  /// </summary>
  public static class Vector3X {
    /// <summary>
    /// Returns the result of the linear interpolation between
    /// this vector and <paramref name="to"/> by amount
    /// <paramref name="weight"/>.
    /// </summary>
    /// <param name="vector"></param>
    /// <param name="to">The destination vector for interpolation.</param>
    /// <param name="weight">A value on the range of 0.0 to 1.0, representing
    /// the amount of interpolation.</param>
    /// <returns>The resulting vector of the interpolation.</returns>
    public static Vector3 Lerp(this Vector3 vector, Vector3 to, float weight)
       => new(
          Mathf.Lerp(vector.x, to.x, weight),
          Mathf.Lerp(vector.y, to.y, weight),
          Mathf.Lerp(vector.z, to.z, weight)
      );
  }
}
