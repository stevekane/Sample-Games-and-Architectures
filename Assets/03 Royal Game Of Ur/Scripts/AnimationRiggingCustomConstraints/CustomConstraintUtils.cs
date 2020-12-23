using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;

namespace CustomConstraints {
  public static class CustomConstraintUtils {
    public static void ParallelTransport(in NativeArray<float3> tangents, in float3 normal0, ref NativeArray<float3> normals) {
      var count = normals.Length - 1;

      normals[0] = normal0;
      for (var i = 0; i < count; i++) {
        var bitangent = cross(tangents[i], tangents[i+1]);
        var bitangentNorm = length(bitangent);

        if (bitangentNorm < float.Epsilon && bitangentNorm > -float.Epsilon) {
          normals[i+1] = normals[i];
        } else {
          var bitangentNormalized = bitangent / bitangentNorm;
          var theta = acos(dot(tangents[i], tangents[i+1]));
          var rotation = quaternion.AxisAngle(bitangentNormalized, theta);

          normals[i+1] = mul(rotation, normals[i]);
        }
      }
    }
  }
}