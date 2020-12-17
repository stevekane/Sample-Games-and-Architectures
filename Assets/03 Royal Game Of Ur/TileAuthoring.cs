using UnityEngine;

namespace RoyalGameOfUr {
  public class TileAuthoring : MonoBehaviour {
    public bool IsRosette;

    public void OnDrawGizmos() {
      Gizmos.color = IsRosette ? Color.red : Color.white; 
      Gizmos.DrawCube(transform.position, new Vector3(.9f, .1f, .9f));
    }
  }
}