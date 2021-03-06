﻿using UnityEngine;
using UnityEngine.Animations.Rigging;
using CustomConstraints;

public class RenderArm : MonoBehaviour {
  [SerializeField] PBDBonesConstraint CurrentConstraint = null;
  [SerializeField] PBDBonesConstraint NextConstraint = null;
  [SerializeField] Transform CurrentTargetTransform = null;
  [SerializeField] Transform NextTargetTransform = null;

  Transform CurrentTransform;
  Transform NextTransform;

  public void SetNextTarget(Transform nextTarget) {
    CurrentTransform = NextTransform;
    NextTransform = nextTarget;
  }

  public void BlendToNextTarget(in float interpolant) {
    CurrentConstraint.weight = interpolant;
    NextConstraint.weight = 1 - interpolant;
  }

  public void Step(in float dt) {
    if (CurrentTransform) {
      CurrentTargetTransform.position = CurrentTransform.position;
    }
    if (NextTransform) {
      NextTargetTransform.position = NextTransform.position;
    }
  }
}