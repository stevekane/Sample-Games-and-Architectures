using System.Collections.Generic;
using UnityEngine;

namespace RoyalGameOfUr {
  public static class Extensions {
    public static Vector3Int RoundToInt(in Vector3 v) {
      return new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
    }

    public static Dictionary<Vector3Int, Tile> FromAuthoredTiles(TileAuthoring[] authoredTiles) {
      var dict = new Dictionary<Vector3Int, Tile>(authoredTiles.Length);
      foreach (var authoredTile in authoredTiles) {
        var position = RoundToInt(authoredTile.transform.position);
        var tile = new Tile { IsRosette = authoredTile.IsRosette };

        dict.Add(position, tile);
      }
      return dict;
    }
  }
}