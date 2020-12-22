using System;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using static Unity.Mathematics.math;

namespace CustomConstraints {
  [BurstCompile]
  public struct PBDBonesJob : IWeightedAnimationJob {
    public FloatProperty jobWeight { get; set; }
    public NativeArray<ReadWriteTransformHandle> boneChain;
    public NativeArray<float3> positions;

    [ReadOnly] public NativeArray<float> restLengths;
    [ReadOnly] public ReadOnlyTransformHandle tipTarget;
    [ReadOnly] public float minimumHeight;
    [ReadOnly] public int iterationCount;

    public void ProcessAnimation(AnimationStream stream) {
      var jointCount = positions.Length;
      var boneCount = jointCount - 1;

      // fill the local buffer of positions
      for (var i = 0; i < jointCount; i++) {
        positions[i] = boneChain[i].GetPosition(stream);
      }

      // Perform solver iterations
      for (var i = 0; i < iterationCount; i++) {
        // project distance constraints
        for (var j = 0; j < boneCount; j++) {
          var invMass0 = j == 0 ? 0f : 1f;
          var invMass1 = j == boneCount ? 0 : 1f;
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

        // force final position to the tip target... this is fuckin wrong... think this through
        positions[positions.Length - 1] = tipTarget.GetPosition(stream);

        // project collisions
        for (var j = 0; j < jointCount; j++) {
          var p = positions[j];

          p.y = max(p.y, minimumHeight);
          positions[j] = p;
        }
      }

      // Write local buffer's data back to the transforms
      for (var i = 0; i < positions.Length; i++) {
        boneChain[i].SetPosition(stream, positions[i]);
      }
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
        positions = new NativeArray<float3>(data.BoneChain.Length, Allocator.Persistent),
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
      job.positions.Dispose();
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