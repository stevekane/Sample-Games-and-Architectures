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

      if (Input.GetMouseButtonDown(0) && Physics.Raycast(screenRay, out RaycastHit hit)) {
        if (game.TryGetPieceIndex(player, ToLogicalPosition(hit.point), out var pieceIndex)) {
          if (game.IsStillInPlay(player, pieceIndex)) {
            if (game.TryGetMove(player, pieceIndex, game.DiceCount, out var position)) {
              game.MovePiece(player, pieceIndex, position);
            }
          }
        }
      }
    }
  }

  [Serializable]
  public class Renderer {
    const int INITIAL_TRANSACTION_QUEUE_SIZE = 9;

    public abstract class Transaction {
      public abstract bool Complete(Renderer renderer);
      public virtual void OnStart(Renderer renderer) {}
      public virtual void OnEnd(Renderer renderer) {}
      public virtual void Execute(Renderer renderer, in float dt) {}
    }

    class MovePieceTransaction : Transaction {
      AnimationCurve Curve;
      float Duration;
      float Remaining;
      RenderPiece Piece;
      Vector3 StartingPosition;
      Vector3 TargetPosition;

      public MovePieceTransaction(AnimationCurve curve, in float duration, RenderPiece piece, in Vector2Int position) {
        Curve = curve;
        Duration = duration;
        Remaining = duration;
        Piece = piece;
        TargetPosition = ToWorldPosition(position);
      }

      public override bool Complete(Renderer renderer) {
        return Remaining <= 0;
      }

      public override void OnStart(Renderer renderer) {
        // TODO: Questionable...
        StartingPosition = ToWorldPosition(ToLogicalPosition(Piece.transform.position));
      }

      public override void Execute(Renderer renderer, in float dt) {
        Piece.transform.position = Vector3.Lerp(StartingPosition, TargetPosition, Curve.Evaluate(1 - Remaining / Duration));
        Remaining = Mathf.Max(0, Remaining - dt);
      }
    }

    class RollDiceTransaction : Transaction {
      bool[] Dice;

      public RollDiceTransaction(bool[] dice) {
        Dice = new bool[dice.Length];
        dice.CopyTo(Dice, 0);
      }

      public override bool Complete(Renderer renderer) {
        return true;
      }

      public override void OnStart(Renderer renderer) {
        renderer.DiceCountText.text = Dice.Count(true.Equals).ToString();
      }
    }

    class SetTurnTransaction : Transaction {
      Light OnLight;
      Light OffLight;

      public SetTurnTransaction(Light onLight, Light offLight) {
        OnLight = onLight;
        OffLight = offLight;
      }

      public override bool Complete(Renderer renderer) {
        return true;
      }

      public override void OnStart(Renderer renderer) {
        OnLight.enabled = true;
        OffLight.enabled = false;
      }
    }

    [Serializable]
    public class RenderPlayer {
      public Transform[] Route;
      public RenderPiece[] Pieces;
    }

    public RenderPlayer RenderPlayer1;
    public RenderPlayer RenderPlayer2;

    [SerializeField] Light Player1Spotlight = null;
    [SerializeField] Light Player2Spotlight = null;
    [SerializeField] Text DiceCountText = null;
    [SerializeField] AnimationCurve MoveCurve = null;
    [SerializeField] float MoveDuration = 1f;
    Queue<Transaction> Transactions = new Queue<Transaction>(INITIAL_TRANSACTION_QUEUE_SIZE);
    Transaction CurrentTransaction;

    public void MovePiece(in bool isPlayer1, in int pieceIndex, Vector2Int position) {
      var player = isPlayer1 ? RenderPlayer1 : RenderPlayer2;
      var piece = player.Pieces[pieceIndex];

      Transactions.Enqueue(new MovePieceTransaction(MoveCurve, MoveDuration, piece, position));
    }

    public void RollDice(in bool[] dice) {
      Transactions.Enqueue(new RollDiceTransaction(dice));
    }

    public void SetTurn(bool isPlayer1) {
      var onLight = isPlayer1 ? Player1Spotlight : Player2Spotlight;
      var offLight = isPlayer1 ? Player2Spotlight : Player1Spotlight;

      Transactions.Enqueue(new SetTurnTransaction(onLight, offLight));
    }

    public void Step(float dt) {
      if (CurrentTransaction != null) {
        if (!CurrentTransaction.Complete(this)) {
          CurrentTransaction.Execute(this, dt);
        } else {
          CurrentTransaction.OnEnd(this);
          CurrentTransaction = null;
        }
      } else {
        if (Transactions.Count > 0) {
          CurrentTransaction = Transactions.Dequeue();
          CurrentTransaction.OnStart(this);
          CurrentTransaction.Execute(this, dt);
        }
      }
    }
  }

  [Serializable]
  public struct Tile {
    public bool IsRosette;
    public bool IsSafe;
  }

  [Serializable]
  public class Player {
    public Vector2Int[] Route;
    public Vector2Int[] PiecePositions;

    public Player(Renderer.RenderPlayer renderPlayer) {
      Route = renderPlayer.Route.Select(p => ToLogicalPosition(p.position)).ToArray();
      PiecePositions = renderPlayer.Pieces.Select(p => ToLogicalPosition(p.transform.position)).ToArray();
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

    // Queries
    public int DiceCount {
      get {
        return Dice.Count(true.Equals); // ew david...ew
      }
    }

    public bool TryGetPieceIndex(Player player, in Vector2Int position, out int index) {
      index = Array.IndexOf(player.PiecePositions, position);
      return index >= 0;
    }

    public bool IsStillInPlay(Player player, in int pieceIndex) {
      var piecePosition = player.PiecePositions[pieceIndex];
      var routeIndexForPosition = Array.IndexOf(player.Route, piecePosition);

      return routeIndexForPosition != player.Route.LastIndex();
    }

    public bool TryGetMove(Player player, in int pieceIndex, in int amount, out Vector2Int position) {
      var otherPlayer = player == Player1 ? Player2 : Player1;
      var piecePosition = player.PiecePositions[pieceIndex];
      var routeIndex = Array.IndexOf(player.Route, piecePosition);
      var candidateRouteIndex = routeIndex + amount;
      var canMoveOnRoute = candidateRouteIndex <= player.Route.LastIndex();

      if (canMoveOnRoute) {
        var candidateRoutePosition = player.Route[candidateRouteIndex];

        if (PositionToTile.TryGetValue(candidateRoutePosition, out Tile tile)) {
          var occupiedByPlayer = Array.IndexOf(player.PiecePositions, candidateRoutePosition) >= 0;
          var occupiedByOpponent = Array.IndexOf(otherPlayer.PiecePositions, candidateRoutePosition) >= 0;
          var canMove = !occupiedByPlayer && (!occupiedByOpponent || (occupiedByOpponent && !tile.IsSafe));

          if (canMove) {
            position = candidateRoutePosition;
            return true;
          }
        }
      }
      position = default;
      return false;
    }

    // Transactions
    public void SetTurn(bool isPlayer1) {
      IsPlayer1Turn = isPlayer1;
      Renderer.SetTurn(IsPlayer1Turn);
    }

    public void RollDice() {
      for (var i = 0; i < Dice.Length; i++) {
        Dice[i] = Random.Range(0, 2) == 1;
      }
      Renderer.RollDice(Dice);
    }

    public void MovePiece(Player player, int pieceIndex, Vector2Int position) {
      var otherPlayer = player == Player1 ? Player2 : Player1;

      player.PiecePositions[pieceIndex] = position;
      Renderer.MovePiece(player == Player1, pieceIndex, position);
      if (TryGetPieceIndex(otherPlayer, position, out var index)) {
        otherPlayer.PiecePositions[index] = otherPlayer.Route[0];
        Renderer.MovePiece(player != Player1, index, otherPlayer.Route[0]);
      }
      if (PositionToTile.TryGetValue(position, out var tile) && !tile.IsRosette) {
        SetTurn(!IsPlayer1Turn);
      }
      RollDice();
      while (DiceCount == 0) {
        SetTurn(!IsPlayer1Turn);
        RollDice();
      }
    }

    // LifeCycle 
    void Awake() {
      var authoredTiles = FindObjectsOfType<RenderTile>();

      Random.InitState(InitialRandomSeed);
      PositionToTile = FromAuthoredTiles(authoredTiles);
      Player1 = new Player(Renderer.RenderPlayer1);
      Player2 = new Player(Renderer.RenderPlayer2);
      CurrenState = State.TakingTurn;
      IsPlayer1Turn = true;
      RollDice();
      while (DiceCount == 0) {
        SetTurn(!IsPlayer1Turn);
        RollDice();
      }
    }

    void Update() {
      switch (CurrenState) {
        case State.TakingTurn: {
          InputSystem.Step(this, Time.deltaTime);
          Renderer.Step(Time.deltaTime);
        }
        break;

        case State.GameOver: {

        }
        break;
      }
    }
  }
}