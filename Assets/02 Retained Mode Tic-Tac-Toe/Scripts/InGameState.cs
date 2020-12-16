using System;
using UnityEngine;
using static RetainedModeTicTacToe.Extensions;

namespace RetainedModeTicTacToe {
  [Serializable]
  public struct Board {
    public enum CellState { Empty, X, O }

    public int Size;
    public CellState[] CellStates;

    public Board(int size) {
      Size = size;
      CellStates = new CellState[size * size];
    }

    public bool GameIsADraw() {
      return All(CellStates, cs => cs != CellState.Empty);
    }

    public bool PlayerWon(bool isPlayer1) {
      for (var i = 0; i < Size; i++) {
        var wonHorizontal = true;
        for (var j = 0; j < Size; j++) {
          wonHorizontal &= IsOccupiedBy(i, j, isPlayer1);
        }
        if (wonHorizontal) {
          return true;
        }
      }

      for (var j = 0; j < Size; j++) {
        var wonVertical = true;
        for (var i = 0; i < Size; i++) {
          wonVertical &= IsOccupiedBy(i, j, isPlayer1);
        }
        if (wonVertical) {
          return true;
        }
      }

      var wonBackslash = true;
      for (var i = 0; i < Size; i++) {
        wonBackslash &= IsOccupiedBy(i, i, isPlayer1);
      }
      if (wonBackslash) {
        return true;
      }

      var wonSlash = true;
      for (var i = 0; i < Size; i++) {
        wonSlash &= IsOccupiedBy(i, Size - 1 - i, isPlayer1);
      }
      if (wonSlash) {
        return true;
      }

      return false;
    }

    public bool Occupied(in int x, in int y) {
      return CellStates[To1DIndex(Size, x, y)] != CellState.Empty;
    }

    public bool IsOccupiedBy(in int x, in int y, in bool isPlayer1) {
      return CellStates[To1DIndex(Size, x, y)].Equals(isPlayer1 ? CellState.X : CellState.O);
    }

    public void OccupyCell(in int x, in int y, in bool isPlayer1) {
      CellStates[To1DIndex(Size, x, y)] = isPlayer1 ? CellState.X : CellState.O;
    }
  }

  [Serializable]
  public class BoardRenderer {
    public int Size;
    public CellRenderer[] CellRenderers;
    public float InterpolationEpsilon = .01f;

    public void Step(in float dt) {
      for (var i = 0; i < CellRenderers.Length; i++) {
        CellRenderers[i].RotateTowardsTarget(dt, InterpolationEpsilon);
      }
    }

    public void OccupyCell(in int x, in int y, in bool isPlayer1) {
      var index = To1DIndex(Size, x, y);
      var forward = isPlayer1 ? Vector3.left : Vector3.right;

      CellRenderers[index].TargetRotation = Quaternion.LookRotation(forward, Vector3.up);
    }
  }

  public class InGameState : GameState {
    public enum State { Play, Victory, Draw }

    public State CurrentState;
    public Board Board = new Board(3);
    public BoardRenderer BoardRenderer;
    public Camera Camera;
    public bool IsPlayer1Turn;

    public override void Step(RetainedModeTicTacToe game, float dt) {
      BoardRenderer.Step(dt);
      switch (CurrentState) {
        case State.Play: {
          var screenRay = Camera.ScreenPointToRay(Input.mousePosition);

          if (Input.GetMouseButtonDown(0) && Physics.Raycast(screenRay, out RaycastHit hit)) {
            var x = Mathf.RoundToInt(hit.point.x);
            var y = Mathf.RoundToInt(hit.point.y);

            if (!Board.Occupied(x, y)) {
              Board.OccupyCell(x, y, IsPlayer1Turn);
              BoardRenderer.OccupyCell(x, y, IsPlayer1Turn);
              if (Board.PlayerWon(IsPlayer1Turn)) {
                CurrentState = State.Victory;
              } else if (Board.GameIsADraw()) {
                CurrentState = State.Draw;
              } else {
                IsPlayer1Turn = !IsPlayer1Turn;
              }
            }
          }
        }
        break;

        case State.Victory: {

        }
        break;

        case State.Draw: {

        }
        break;
      }
    }
  }
}