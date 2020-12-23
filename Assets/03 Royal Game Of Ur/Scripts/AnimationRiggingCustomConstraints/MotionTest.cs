using UnityEngine;

public class MotionTest : MonoBehaviour {
  [Range(0, Mathf.PI / 4f)] public float Radians = Mathf.PI / 4f;
  [Range(.5f, 1f)]          public float Radius = 1f;
  [Range(0, 10f)]           public float Frequency = 1f;

  public Vector3 AdditionalRotationEulerAngles = new Vector3(0, -90, 0);
  public Transform Driven;

  void Update() {
    var radians = Radians * Mathf.Sin(Time.time * Frequency);
    var x = Mathf.Sin(radians) * Radius;
    var z = Mathf.Cos(radians) * Radius;
    var position = new Vector3(x, transform.position.y, z);
    var baseRotation = Quaternion.LookRotation(position - transform.position, transform.up);
    var localRotation = Quaternion.Euler(AdditionalRotationEulerAngles.x, AdditionalRotationEulerAngles.y, AdditionalRotationEulerAngles.z);
    var rotation = baseRotation * localRotation;

    Driven.SetPositionAndRotation(position, rotation);
  }
}
