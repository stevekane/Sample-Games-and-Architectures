using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using static RoyalGameOfUr.Extensions;

namespace RoyalGameOfUr {
  [Serializable]
  public class InputSystem {
    public enum State { None, AwaitingDiceRoll, AwaitingPieceSelection }

    const float MAX_DISTANCE = 1000f;

    [SerializeField] Camera Camera = null;
    [SerializeField] LayerMask DiceLayerMask = default;
    [SerializeField] LayerMask BoardLayerMask = default;

    public State CurrentState;

    public void Step(RoyalGameOfUr game, float dt) {
      var screenRay = Camera.ScreenPointToRay(Input.mousePosition);
      var player = game.IsPlayer1Turn ? game.Player1 : game.Player2;

      switch (CurrentState) {
        case State.None: {

        }
        break;

        case State.AwaitingDiceRoll: {
          if (Input.GetMouseButtonDown(0) && Physics.Raycast(screenRay, out var hit, MAX_DISTANCE, DiceLayerMask)) {
            game.RollDice();
          }
        }
        break;

        case State.AwaitingPieceSelection: {
          if (Input.GetMouseButtonDown(0) && Physics.Raycast(screenRay, out var hit, MAX_DISTANCE, BoardLayerMask)) {
            if (game.TryGetPieceIndex(player, ToLogicalPosition(hit.point), out var pieceIndex)) {
              if (game.IsStillInPlay(player, pieceIndex)) {
                if (game.TryGetMove(player, pieceIndex, game.DiceCount, out var position)) {
                  game.MovePiece(player, pieceIndex, position);
                }
              }
            }
          }
        }
        break;
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
      enum State { MovingArm, MovingPiece, Done }

      AnimationCurve Curve;
      float MoveArmDuration;
      float MovePieceDuration;
      float Remaining;
      RenderArm Arm;
      RenderPiece Piece;
      Vector3 StartingPosition;
      Vector3 TargetPosition;
      State CurrentState;

      public MovePieceTransaction(AnimationCurve curve, in float duration, RenderArm arm, RenderPiece piece, in Vector2Int position) {
        Curve = curve;
        MoveArmDuration = duration / 2f;
        MovePieceDuration = duration / 2f;
        Remaining = MoveArmDuration;
        Arm = arm;
        Piece = piece;
        TargetPosition = ToWorldPosition(position);
      }

      public override bool Complete(Renderer renderer) {
        return CurrentState == State.Done;
      }

      public override void OnStart(Renderer renderer) {
        StartingPosition = Piece.transform.position;
        Arm.SetNextTarget(Piece.transform);
        Arm.BlendToNextTarget(interpolant: 0);
        Arm.Step(dt: 0);
        CurrentState = State.MovingArm;
      }

      public override void Execute(Renderer renderer, in float dt) {
        switch (CurrentState) {
          case State.MovingArm:
            Arm.Step(dt: dt);
            Arm.BlendToNextTarget(interpolant: Remaining / MoveArmDuration);
            Remaining = Mathf.Max(0, Remaining - dt);
            if (Remaining <= 0) {
              Remaining = MovePieceDuration;
              CurrentState = State.MovingPiece;
            }
          break;

          case State.MovingPiece:
            Piece.transform.position = Vector3.Lerp(StartingPosition, TargetPosition, Curve.Evaluate(1 - Remaining / MovePieceDuration));
            Arm.Step(dt: dt);
            Remaining = Mathf.Max(0, Remaining - dt);
            if (Remaining <= 0) {
              CurrentState = State.Done;
            }
          break;
        }
      }
    }

    class RollDiceTransaction : Transaction {
      bool[] Dice;
      RenderDice[] RenderDice;

      public RollDiceTransaction(bool[] dice, RenderDice[] renderDice) {
        Dice = new bool[dice.Length];
        dice.CopyTo(Dice, 0);
        RenderDice = renderDice;
      }

      public override bool Complete(Renderer renderer) {
        return true;
      }

      public override void OnStart(Renderer renderer) {
        for (var i = 0; i < Dice.Length; i++) {
          if (Dice[i]) {
            RenderDice[i].transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
          } else {
            RenderDice[i].transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
          }
        }
      }
    }

    class SetTurnTransaction : Transaction {
      public RenderPlayer ActivePlayer;
      public RenderPlayer InactivePlayer;

      public SetTurnTransaction(RenderPlayer activePlayer, RenderPlayer inactivePlayer) {
        ActivePlayer = activePlayer;
        InactivePlayer = inactivePlayer;
      }

      public override bool Complete(Renderer renderer) {
        return true;
      }

      public override void OnStart(Renderer renderer) {
        foreach (var piece in ActivePlayer.Pieces) {
          foreach (var meshRenderer in piece.GetComponentsInChildren<MeshRenderer>()) {
            meshRenderer.material.EnableKeyword("_EMISSION");
          }
        }

        foreach (var piece in InactivePlayer.Pieces) {
          foreach (var meshRenderer in piece.GetComponentsInChildren<MeshRenderer>()) {
            meshRenderer.material.DisableKeyword("_EMISSION");
          }
        }
      }
    }

    [Serializable]
    public class RenderPlayer {
      public Transform[] Route;
      public RenderPiece[] Pieces;
      public RenderDice[] RenderDice;
      public RenderArm RenderArm;
    }

    public RenderPlayer RenderPlayer1;
    public RenderPlayer RenderPlayer2;

    [SerializeField] AnimationCurve MoveCurve = null;
    [SerializeField] float MoveDuration = 1f;

    Queue<Transaction> Transactions = new Queue<Transaction>(INITIAL_TRANSACTION_QUEUE_SIZE);
    Transaction CurrentTransaction;

    public void MovePiece(in bool isPlayer1, in int pieceIndex, Vector2Int position) {
      var player = isPlayer1 ? RenderPlayer1 : RenderPlayer2;
      var piece = player.Pieces[pieceIndex];

      Transactions.Enqueue(new MovePieceTransaction(MoveCurve, MoveDuration, player.RenderArm, piece, position));
    }

    public void RollDice(bool isPlayer1, in bool[] dice) {
      var player = isPlayer1 ? RenderPlayer1 : RenderPlayer2;

      Transactions.Enqueue(new RollDiceTransaction(dice, player.RenderDice));
    }

    public void SetTurn(bool isPlayer1) {
      var activePlayer = isPlayer1 ? RenderPlayer1 : RenderPlayer2;
      var inactivePlayer = isPlayer1 ? RenderPlayer2 : RenderPlayer1;

      Transactions.Enqueue(new SetTurnTransaction(activePlayer, inactivePlayer));
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

  public class RoyalGameOfUr : MonoBehaviour {
    enum State { RollingDice, ChoosingMove, GameOver }

    // Run-time logical state
    public bool[] Dice = new bool[4];
    public Dictionary<Vector2Int, Tile> PositionToTile;
    public Player Player1;
    public Player Player2;
    public bool IsPlayer1Turn;
    State CurrentState;

    // Editor-configured initial state
    [SerializeField] int InitialRandomSeed = 1;
    [SerializeField] InputSystem InputSystem = null;
    [SerializeField] Renderer Renderer = null;

    // Queries
    public int DiceCount {
      get { return Dice.Count(true.Equals); }
    }

    public Player ActivePlayer {
      get { return IsPlayer1Turn ? Player1 : Player2; }
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
        } else {
          position = candidateRoutePosition;
          return true;
        }
      }
      position = default;
      return false;
    }

    public bool PlayerHasMove(Player player, in int amount) {
      for (var i = 0; i < player.PiecePositions.Length; i++) {
        if (TryGetMove(player, i, amount, out var position)) {
          return true;
        }
      }
      return false;
    }

    // Transactions
    public void RollDice() {
      for (var i = 0; i < Dice.Length; i++) {
        Dice[i] = Random.Range(0, 2) == 1;
      }
      Renderer.RollDice(IsPlayer1Turn, Dice);

      if (DiceCount == 0 || !PlayerHasMove(ActivePlayer, DiceCount)) {
        IsPlayer1Turn = !IsPlayer1Turn;
        Renderer.SetTurn(IsPlayer1Turn);
      } else {
        CurrentState = State.ChoosingMove;
        InputSystem.CurrentState = InputSystem.State.AwaitingPieceSelection;
      }
    }

    public void MovePiece(Player player, int pieceIndex, Vector2Int position) {
      var otherPlayer = player == Player1 ? Player2 : Player1;

      player.PiecePositions[pieceIndex] = position;
      Renderer.MovePiece(player == Player1, pieceIndex, position);
      if (TryGetPieceIndex(otherPlayer, position, out var index)) {
        otherPlayer.PiecePositions[index] = otherPlayer.Route[0];
        Renderer.MovePiece(player != Player1, index, otherPlayer.Route[0]);
      }
      if (PositionToTile.TryGetValue(position, out var tile)) {
        if (tile.IsRosette) {
          CurrentState = State.RollingDice;
          InputSystem.CurrentState = InputSystem.State.AwaitingDiceRoll;
        } else {
          IsPlayer1Turn = !IsPlayer1Turn;
          InputSystem.CurrentState = InputSystem.State.AwaitingDiceRoll;
          Renderer.SetTurn(IsPlayer1Turn);
        }
      }
    }

    // LifeCycle 
    void Awake() {
      var authoredTiles = FindObjectsOfType<RenderTile>();

      Random.InitState(InitialRandomSeed);
      PositionToTile = FromAuthoredTiles(authoredTiles);
      Player1 = new Player(Renderer.RenderPlayer1);
      Player2 = new Player(Renderer.RenderPlayer2);
      IsPlayer1Turn = true;
      CurrentState = State.RollingDice;
      InputSystem.CurrentState = InputSystem.State.AwaitingDiceRoll;
    }

    void Update() {
      switch (CurrentState) {
        case State.RollingDice: {
          InputSystem.Step(this, Time.deltaTime);
          Renderer.Step(Time.deltaTime);
        }
        break;

        case State.ChoosingMove: {
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