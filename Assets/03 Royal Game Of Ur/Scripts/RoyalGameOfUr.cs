using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using static RoyalGameOfUr.Extensions;

namespace RoyalGameOfUr {
  [Serializable]
  public class InputSystem {
    [SerializeField] Camera Camera;

    public InputSystem(Camera camera) {
      Camera = camera;
    }

    public void Step(RoyalGameOfUr game, float dt) {
      var screenRay = Camera.ScreenPointToRay(Input.mousePosition);
      var player = game.IsPlayer1Turn ? game.Player1 : game.Player2;
      var otherPlayer = game.IsPlayer1Turn ? game.Player2 : game.Player1;

      if (Input.GetMouseButtonDown(0) && Physics.Raycast(screenRay, out RaycastHit hit)) {
        var position = RoundToInt(hit.point);
        var pieceIndex = Array.IndexOf(player.PiecePositions, position);

        if (pieceIndex >= 0) {
          var routeIndex = Array.IndexOf(player.Route, position);
          var candidateRouteIndex = routeIndex + game.DiceCount;
          var candidatePositionOnPlayerRoute = candidateRouteIndex < player.Route.Length;

          if (candidatePositionOnPlayerRoute) {
            var candidatePosition = player.Route[candidateRouteIndex];

            // TODO: May not be important now. The route begins/ends off the board so this maybe should be lifted
            // as in some sense pieces may move outside the board as well
            if (game.PositionToTile.TryGetValue(candidatePosition, out var tile)) {
              var occupiedByOwnPiece = player.PiecePositions.Contains(candidatePosition);
              var occupiedByEnemyPiece = otherPlayer.PiecePositions.Contains(candidatePosition);
              var isRosette = tile.IsRosette;

              if (!occupiedByOwnPiece || (occupiedByEnemyPiece && !isRosette)) {
                game.MovePiece(player, pieceIndex, candidateRouteIndex);
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
  public class RenderPlayer {
    public Transform[] Route;
    public RenderPiece[] Pieces;
  }

  [Serializable]
  public class Renderer {
    public RenderPlayer RenderPlayer1;
    public RenderPlayer RenderPlayer2;
    [SerializeField] Text DiceCountText;

    public void Step(RoyalGameOfUr game, float dt) {
      for (var i = 0; i < RenderPlayer1.Pieces.Length; i++) {
        RenderPlayer1.Pieces[i].transform.position = ToWorldPosition(game.Player1.PiecePositions[i]);
      }
      for (var i = 0; i < RenderPlayer2.Pieces.Length; i++) {
        RenderPlayer2.Pieces[i].transform.position = ToWorldPosition(game.Player2.PiecePositions[i]);
      }
      DiceCountText.text = game.DiceCount.ToString();
    }
  }

  [Serializable]
  public struct Tile {
    public bool IsRosette;
  }

  [Serializable]
  public class Player {
    public Vector2Int[] Route;
    public Vector2Int[] PiecePositions;

    public Player(RenderPlayer renderPlayer) {
      Route = renderPlayer.Route.Select(p => RoundToInt(p.position)).ToArray();
      PiecePositions = renderPlayer.Pieces.Select(p => RoundToInt(p.transform.position)).ToArray();
    }
  }

  enum State { TakingTurn, GameOver }

  public class RoyalGameOfUr : MonoBehaviour {
    // Run-time logical state
    public bool[] Dice = new bool[4];
    public Dictionary<Vector2Int, Tile> PositionToTile;
    public Player Player1;
    public Player Player2;
    public bool IsPlayer1Turn;
    State CurrenState;

    // Editor-configured initial state
    [SerializeField] int InitialRandomSeed = 1;
    [SerializeField] InputSystem InputSystem = null;
    [SerializeField] Renderer Renderer = null;

    public int DiceCount {
      get {
        return Dice.Count(true.Equals); // ew david...ew
      }
    }

    public void RollDice() {
      for (var i = 0; i < Dice.Length; i++) {
        Dice[i] = Random.Range(0, 2) == 1;
      }
    }

    // TODO: Add essential queries about game state to the game
    // TODO: Add winning condition checks
    // TODO: Add checks for whether a player has a play or not
    // TODO: Add alert state that times out a player's turn after 3 seconds when they have no play?
    // TODO: How to handle turns when the player rolls zero? This means they have no moves.
    // TODO: How to handle turns when the player has no moves based on their current roll?
    // TODO: Add handling of rosettes properly and the safe cell

    // TODO: This currently assumes play always changes between players regardless of landing on rosettes
    // eventually, this should only change players if the space landed on does not contain a rosette
    // TODO: Should check if the game has ended here as well.
    public void MovePiece(Player player, int pieceIndex, int routeIndex) {
      var piece = player.PiecePositions[pieceIndex];
      var routePosition = player.Route[routeIndex];

      player.PiecePositions[pieceIndex] = player.Route[routeIndex];
      do {
        IsPlayer1Turn = !IsPlayer1Turn;
        RollDice();
      } while (DiceCount == 0);
    }

    void Awake() {
      var authoredTiles = FindObjectsOfType<RenderTile>();

      Random.InitState(InitialRandomSeed);
      PositionToTile = FromAuthoredTiles(authoredTiles);
      Player1 = new Player(Renderer.RenderPlayer1);
      Player2 = new Player(Renderer.RenderPlayer2);
      CurrenState = State.TakingTurn;
      IsPlayer1Turn = false;
      do {
        IsPlayer1Turn = !IsPlayer1Turn;
        RollDice();
      } while (DiceCount == 0);
    }

    void Update() {
      switch (CurrenState) {
        case State.TakingTurn: {
          InputSystem.Step(this, Time.deltaTime);
          Renderer.Step(this, Time.deltaTime);
        }
        break;

        case State.GameOver: {

        }
        break;
      }
    }
  }
}