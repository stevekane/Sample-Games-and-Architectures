using System.Collections.Generic;
using UnityEngine;

namespace RoyalGameOfUr {
  public static class Extensions {
    public static Vector2Int RoundToInt(in Vector3 v) {
      return new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.z));
    }

    public static Vector3 ToWorldPosition(in Vector2Int vi) {
      return new Vector3(vi.x, 0, vi.y);
    }

    public static Dictionary<Vector2Int, Tile> FromAuthoredTiles(RenderTile[] authoredTiles) {
      var dict = new Dictionary<Vector2Int, Tile>(authoredTiles.Length);
      foreach (var authoredTile in authoredTiles) {
        var position = RoundToInt(authoredTile.transform.position);
        var tile = new Tile { IsRosette = authoredTile.IsRosette };

        dict.Add(position, tile);
      }
      return dict;
    }
  }
}