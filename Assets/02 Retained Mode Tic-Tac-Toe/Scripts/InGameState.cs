using System;
using UnityEngine;
using UnityEngine.UI;
using static RetainedModeTicTacToe.Extensions;

namespace RetainedModeTicTacToe {
  public enum CellState { Empty, X, O }

  [Serializable]
  public struct Board {
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

    public void OccupyCell(in int x, in int y, in bool isPlayer1) {
      CellRenderers[To1DIndex(Size, x, y)].Animator.SetInteger("State", isPlayer1 ? 1 : 2);
    }
  }

  [Serializable]
  public class UI {
    [SerializeField] Animator GameOverOverlayAnimator = null;
    [SerializeField] Text GameOverOverlayText = null;
    [SerializeField] Texture2D XCursor = null;
    [SerializeField] Texture2D OCursor = null;

    public void OnGameStart(bool player1Turn) {
      GameOverOverlayAnimator.SetBool("Hidden", true);
      Cursor.SetCursor(player1Turn ? XCursor : OCursor, Vector2.zero, CursorMode.Auto);
    }

    public void OnChangePlayerTurn(bool player1Turn) {
      Cursor.SetCursor(player1Turn ? XCursor : OCursor, Vector2.zero, CursorMode.Auto);
    }

    public void OnVictory(bool player1Won) {
      var symbol = player1Won ? "X" : "O";

      GameOverOverlayText.text = $"{symbol} Wins!";
      GameOverOverlayAnimator.SetBool("Hidden", false);
    }

    public void OnDraw() {
      GameOverOverlayText.text = $"It's a draw!";
      GameOverOverlayAnimator.SetBool("Hidden", false);
    }
  }

  [Serializable]
  public class AudioSystem {
    [SerializeField] AudioClip[] TurnSounds = null;
    [SerializeField] AudioClip InGameMusic = null;
    [SerializeField] AudioClip DrawMusic = null;
    [SerializeField] AudioClip VictoryMusic = null;
    [SerializeField] AudioSource MusicSource = null;

    public void OnGameStart() {
      MusicSource.Play(InGameMusic);
    }

    public void OnPlayerTurn(bool isPlayer1) {
      AudioSource.PlayClipAtPoint(RandomFrom(TurnSounds), Vector3.zero);
    }

    public void OnDraw() {
      MusicSource.Play(DrawMusic);
    }

    public void OnVictory() {
      MusicSource.Play(VictoryMusic);
    }
  }

  public class InGameState : GameState {
    public enum State { Play, Victory, Draw }

    [SerializeField] string OwningSceneName = null;
    [SerializeField] Board Board = new Board(3);
    [SerializeField] BoardRenderer BoardRenderer = null;
    [SerializeField] Camera Camera = null;
    [SerializeField] UI UI = null;
    [SerializeField] AudioSystem AudioSystem = null;

    State CurrentState;
    bool IsPlayer1Turn = true;

    public override void OnEnter(RetainedModeTicTacToe game) {
      UI.OnGameStart(IsPlayer1Turn);
      AudioSystem.OnGameStart();
    }

    public override void Step(RetainedModeTicTacToe game, float dt) {
      switch (CurrentState) {
        case State.Play: {
          var screenRay = Camera.ScreenPointToRay(Input.mousePosition);

          if (Input.GetMouseButtonDown(0) && Physics.Raycast(screenRay, out RaycastHit hit)) {
            var x = Mathf.RoundToInt(hit.point.x);
            var y = Mathf.RoundToInt(hit.point.y);

            if (!Board.Occupied(x, y)) {
              Board.OccupyCell(x, y, IsPlayer1Turn);
              BoardRenderer.OccupyCell(x, y, IsPlayer1Turn);
              AudioSystem.OnPlayerTurn(IsPlayer1Turn);
              if (Board.PlayerWon(IsPlayer1Turn)) {
                UI.OnVictory(IsPlayer1Turn);
                AudioSystem.OnVictory();
                CurrentState = State.Victory;
              } else if (Board.GameIsADraw()) {
                UI.OnDraw();
                AudioSystem.OnDraw();
                CurrentState = State.Draw;
              } else {
                IsPlayer1Turn = !IsPlayer1Turn;
                UI.OnChangePlayerTurn(IsPlayer1Turn);
              }
            }
          }
        }
        break;

        case State.Draw:
        case State.Victory: {
          if (Input.anyKeyDown) {
            game.LoadScene(OwningSceneName);
          }
        }
        break;
      }
    }
  }
}