using UnityEngine;

namespace RetainedModeTicTacToe {
  public static class Extensions {
    public static int To1DIndex(in int width, in int x, in int y) {
      return y * width + x;
    }
    
    public static bool All<T>(T[] xs, System.Predicate<T> predicate) {
      var result = true;

      for (var i = 0; i < xs.Length; i++) {
        result &= predicate(xs[i]);
      }
      return result;
    }

    public static Quaternion ExponentialSlerp(Quaternion a, Quaternion b, in float dt, in float epsilon) {
      return Quaternion.Slerp(a, b, 1.0f - Mathf.Pow(epsilon, dt));
    }

    public static float ExponentialLerp(float a, float b, in float dt, in float epsilon) {
      return Mathf.Lerp(a, b, 1.0f - Mathf.Pow(epsilon, dt));
    }
  }
}