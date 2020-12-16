using UnityEngine;
using static RetainedModeTicTacToe.Extensions;

namespace RetainedModeTicTacToe {
  public class CellRenderer : MonoBehaviour {
    public Quaternion TargetRotation;

    public void RotateTowardsTarget(in float dt, in float epsilon) {
      transform.rotation = ExponentialSlerp(transform.rotation, TargetRotation, dt, epsilon);
    }
  }
}