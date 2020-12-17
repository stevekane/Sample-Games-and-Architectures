using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using static RoyalGameOfUr.Extensions;

namespace RoyalGameOfUr {
  [Serializable]
  public class InputSystem {
    Camera Camera;

    public InputSystem(Camera camera) {
      Camera = camera;
    }

    public void Step(RoyalGameOfUr game, float dt) {
      var screenRay = Camera.ScreenPointToRay(Input.mousePosition);
      var player = game.IsPlayer1Turn ? game.Player1 : game.Player2;
      var otherPlayer = game.IsPlayer1Turn ? game.Player2 : game.Player1;

      if (Input.GetMouseButtonDown(0) && Physics.Raycast(screenRay, out RaycastHit hit)) {
        var position = RoundToInt(hit.point);

        if (player.BoardPositions.Contains(position)) {
          var routeIndex = Array.IndexOf(player.Route, position);
          var nextCandidateRouteIndex = routeIndex + game.DiceCount;
          var candidatePositionOnPlayerRoute = nextCandidateRouteIndex < player.Route.Length;

          if (candidatePositionOnPlayerRoute) {
            var candidatePosition = player.Route[nextCandidateRouteIndex];
            
            if (game.PositionToTile.TryGetValue(candidatePosition, out var tile)) {
              var occupiedByOwnPiece = player.BoardPositions.Contains(candidatePosition);
              var occupiedByEnemyPiece = otherPlayer.BoardPositions.Contains(candidatePosition);
              var isRosette = tile.IsRosette;

              if (!occupiedByOwnPiece || (occupiedByEnemyPiece && !isRosette)) {
                Debug.Log("You can actually make this move!");
              } else {
                Debug.Log("This move is not allowed");
              }
            } else {
              Debug.LogError("Position does not seem to correspond to a tile position. should not happen!");
            }
          } else {
            Debug.Log("Not on the board");
          }
        } else {
          Debug.Log("This is not an area containing the player's piece");
        }
      }
    }
  }

  [Serializable]
  public struct Tile {
    public bool IsRosette;
  }

  [Serializable]
  public class Player {
    public Vector3Int[] Route;
    public HashSet<Vector3Int> BoardPositions;
    public int PiecesAround;

    public Player(PlayerPathAuthoring authoringPath) {
      Route = authoringPath.Positions.Select(p => RoundToInt(p.position)).ToArray();
      BoardPositions = new HashSet<Vector3Int>();
      PiecesAround = 0;
    }
  }

  enum State { TakingTurn, GameOver }

  public class RoyalGameOfUr : MonoBehaviour {
    // Run-time
    public bool[] Dice = new bool[4];
    public Dictionary<Vector3Int, Tile> PositionToTile;
    public Player Player1;
    public Player Player2;
    public bool IsPlayer1Turn;
    State CurrenState;
    InputSystem InputSystem;

    // Authoring
    [SerializeField] int InitialRandomSeed = 1;
    [SerializeField] Camera Camera = null;
    [SerializeField] PlayerPathAuthoring Player1AuthoredPath = null;
    [SerializeField] PlayerPathAuthoring Player2AuthoredPath = null;

    public int DiceCount {
      get {
        return Dice.Count(d => d == true);
      }
    }

    public void BeginTurn(bool isPlayer1Turn) {
      IsPlayer1Turn = isPlayer1Turn;
      CurrenState = State.TakingTurn;
      for (var i = 0; i < Dice.Length; i++) {
        Dice[i] = Random.Range(0, 2) == 1;
      }
    }

    void Awake() {
      var authoredTiles = FindObjectsOfType<TileAuthoring>();

      Random.InitState(InitialRandomSeed);
      PositionToTile = FromAuthoredTiles(authoredTiles);
      Player1 = new Player(Player1AuthoredPath);
      Player2 = new Player(Player2AuthoredPath);
      InputSystem = new InputSystem(Camera);
      BeginTurn(isPlayer1Turn: true);
    }

    void Update() {
      switch (CurrenState) {
        case State.TakingTurn: {
          InputSystem.Step(this, Time.deltaTime);
        }
        break;

        case State.GameOver: {

        }
        break;
      }
    }
  }
}