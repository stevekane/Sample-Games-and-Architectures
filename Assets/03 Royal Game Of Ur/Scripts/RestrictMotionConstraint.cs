using System;
using Unity.Burst;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace CustomConstraints {
  [BurstCompile]
  public struct RestrictMotionJob : IWeightedAnimationJob {
    public FloatProperty jobWeight { get; set; }
    public ReadWriteTransformHandle driven;
    public float minimumHeight;

    public void ProcessRootMotion(AnimationStream stream) {}

    public void ProcessAnimation(AnimationStream stream) {
      var weight = jobWeight.Get(stream);
      var currentPosition = driven.GetPosition(stream);
      var nextPosition = currentPosition;

      nextPosition.y = Math.Max(nextPosition.y, minimumHeight);
      driven.SetPosition(stream, Vector3.Lerp(currentPosition, nextPosition, weight));
    }
  }

  public interface IRestrictMotionData {
    Transform ConstrainedObject { get; }
    float MinimumHeight { get; }
  }

  public class RestrictMotionJobBinder<T> : AnimationJobBinder<RestrictMotionJob, T> 
  where T : struct, IAnimationJobData, IRestrictMotionData {
    public override RestrictMotionJob Create(Animator animator, ref T data, Component component) {
      var job = new RestrictMotionJob {
        driven = ReadWriteTransformHandle.Bind(animator, data.ConstrainedObject),
        minimumHeight = data.MinimumHeight
      };

      return job;
    }

    public override void Destroy(RestrictMotionJob job) {}
  }

  [Serializable]
  public struct RestrictMotionData : IAnimationJobData, IRestrictMotionData {
    [SyncSceneToStream, SerializeField] Transform constrainedObject;
    [SyncSceneToStream, SerializeField] float minimumHeight;

    public Transform ConstrainedObject { get => constrainedObject; }
    public float MinimumHeight { get => minimumHeight; }
    public bool IsValid() => constrainedObject != null;
    public void SetDefaultValues() {
      constrainedObject = null;
      minimumHeight = 0;
    }
  }

  [AddComponentMenu("Custom Animation Rigging/Restrict Motion")]
  public class RestrictMotionConstraint : RigConstraint<RestrictMotionJob, RestrictMotionData, RestrictMotionJobBinder<RestrictMotionData>> {}
}