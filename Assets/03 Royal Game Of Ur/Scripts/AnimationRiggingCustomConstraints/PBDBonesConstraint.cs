using System;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using static Unity.Mathematics.math;
using static CustomConstraints.CustomConstraintUtils;

namespace CustomConstraints {
  [BurstCompile]
  public struct PBDBonesJob : IWeightedAnimationJob {
    public FloatProperty jobWeight { get; set; }
    public NativeArray<ReadWriteTransformHandle> boneChain;

    [ReadOnly] public NativeArray<float> restLengths;
    [ReadOnly] public ReadOnlyTransformHandle tipTarget;
    [ReadOnly] public float minimumHeight;
    [ReadOnly] public int iterationCount;

    public void ProcessAnimation(AnimationStream stream) {
      var jointCount = boneChain.Length;
      var boneCount = jointCount - 1;
      var tipPosition = tipTarget.GetPosition(stream);
      var weight = jobWeight.Get(stream);
      var positions = new NativeArray<float3>(jointCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
      var tangents = new NativeArray<float3>(jointCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
      var normals = new NativeArray<float3>(jointCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
      var v0 = boneChain[0].GetRotation(stream) * Vector3.up;

      // fill the local buffer of positions
      for (var i = 0; i < jointCount; i++) {
        positions[i] = boneChain[i].GetPosition(stream);
      }

      // perform solver iterations
      for (var i = 0; i < iterationCount; i++) {
        // force final position to the tip target... unsure if this is the best...
        positions[positions.Length - 1] = tipPosition;

        // project distance constraints
        for (var j = 0; j < boneCount; j++) {
          var invMass0 = j == 0 ? 0f : 1f;
          var invMass1 = j == boneCount - 1 ? 0 : 1f;
          var p0 = positions[j+0];
          var p1 = positions[j+1];
          var d = restLengths[j];
          var C = distance(p0, p1) - d;

          if (C < float.Epsilon && C > -float.Epsilon)
            continue;

          var direction = normalize(p0 - p1);
          var lambda = C * direction;

          positions[j+0] -= invMass0 / (invMass0 + invMass1) * lambda;
          positions[j+1] += invMass1 / (invMass0 + invMass1) * lambda;
        }

        // project collisions
        for (var j = 0; j < jointCount; j++) {
          var p = positions[j];

          p.y = max(p.y, minimumHeight);
          positions[j] = p;
        }
      }

      // Set the new bone rotations
      for (var i = 0; i < boneCount; i++) {
        var prevDir = boneChain[i+1].GetPosition(stream) - boneChain[i+0].GetPosition(stream);
        var newDir = positions[i+1] - positions[i];
        var currentRotation = boneChain[i].GetRotation(stream);
        var additionalRotation = QuaternionExt.FromToRotation(prevDir, newDir);

        boneChain[i].SetRotation(stream, Quaternion.Lerp(currentRotation, additionalRotation * currentRotation, weight));
      }

      boneChain[jointCount - 1].SetRotation(stream, tipTarget.GetRotation(stream));

      positions.Dispose();
      tangents.Dispose();
      normals.Dispose();
    }


    public void ProcessRootMotion(AnimationStream stream) {}
  }

  public interface IPBDBonesData {
    Transform[] BoneChain { get; }
    Transform TipTarget { get; }
    float MinimumHeight { get; }
    int IterationCount { get; }
  }

  public class PBDBonesJobBinder<T> : AnimationJobBinder<PBDBonesJob, T>
  where T : struct, IAnimationJobData, IPBDBonesData {
    public override PBDBonesJob Create(Animator animator, ref T data, Component component) {
      var job = new PBDBonesJob {
        boneChain = new NativeArray<ReadWriteTransformHandle>(data.BoneChain.Length, Allocator.Persistent),
        restLengths = new NativeArray<float>(data.BoneChain.Length - 1, Allocator.Persistent),
        tipTarget = ReadOnlyTransformHandle.Bind(animator, data.TipTarget),
        minimumHeight = data.MinimumHeight,
        iterationCount = data.IterationCount
      };

      for (var i = 0; i < data.BoneChain.Length; i++) {
        job.boneChain[i] = ReadWriteTransformHandle.Bind(animator, data.BoneChain[i]);
      }
      for (var i = 0; i < data.BoneChain.Length - 1; i++) {
        job.restLengths[i] = distance(data.BoneChain[i].position, data.BoneChain[i+1].position);
      }
      return job;
    }

    public override void Destroy(PBDBonesJob job) {
      job.boneChain.Dispose();
      job.restLengths.Dispose();
    }
  }

  [Serializable]
  public struct PBDBonesData : IAnimationJobData, IPBDBonesData {
    [SyncSceneToStream, SerializeField] Transform[] boneChain;
    [SyncSceneToStream, SerializeField] Transform tipTarget;
    [SerializeField] float minimumHeight;
    [SerializeField] int iterationCount;

    public Transform[] BoneChain { get => boneChain; }
    public Transform TipTarget { get => tipTarget; }
    public float MinimumHeight { get => minimumHeight; }
    public int IterationCount { get => iterationCount; }

    public bool IsValid() => boneChain.Length > 1 && iterationCount > 0;
    public void SetDefaultValues() {
      boneChain = new Transform[0];
      tipTarget = default;
      minimumHeight = 0;
      iterationCount = 1;
    }
  }

  [AddComponentMenu("Custom Animation Rigging/PBD Bones")]
  public class PBDBonesConstraint : RigConstraint<PBDBonesJob, PBDBonesData, PBDBonesJobBinder<PBDBonesData>> {}
}