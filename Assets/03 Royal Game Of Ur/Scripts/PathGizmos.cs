using UnityEngine;

public class PathGizmos : MonoBehaviour {
  public Color PathColor;
  public Transform[] Positions;
  public Vector3 RenderingOffset = Vector3.up;
  public float RenderingRadius = .25f;

  public void OnDrawGizmos() {
    Gizmos.color = PathColor;

    for (var i = 0; i < Positions.Length; i++) {
      Gizmos.DrawSphere(Positions[i].position + RenderingOffset, RenderingRadius);
    }

    for (var i = 0; i < Positions.Length - 1; i++) {
      Gizmos.DrawLine(Positions[i].position + RenderingOffset, Positions[i+1].position + RenderingOffset);
    }
  }
}