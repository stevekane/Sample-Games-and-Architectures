using System.Collections;
using UnityEngine;

public class Board {
  const int BITS_PER_CELL = 2;

  BitArray Cells;
  public int Size;

  public Board(in int size) {
    Size = size;
    Cells = new BitArray(size * size * BITS_PER_CELL);
  }

  public bool Occupied(in int x, in int y) {
    return Cells[To1DIndex(x, y) * BITS_PER_CELL];
  }

  public bool IsOccupiedBy(in int x, in int y, bool isPlayer1) {
    var offset = To1DIndex(x, y) * BITS_PER_CELL;

    return Cells[offset] && (Cells[offset + 1] == isPlayer1);
  }

  public void Occupy(in int x, in int y, bool isFirstPlayer) {
    var offset = To1DIndex(x, y) * BITS_PER_CELL;

    Cells[offset] = true;
    Cells[offset + 1] = isFirstPlayer;
  }

  int To1DIndex(in int x, in int y) {
    return Size * y + x;
  }
}

public class ImmediateModeTicTacToe : MonoBehaviour {
  enum State { TitleScreen, InGame, Victory, Draw }

  [SerializeField] AudioClip[] OnPlayAudioClips = null;
  [SerializeField] Texture2D XCursor = null;
  [SerializeField] Texture2D OCursor = null;
  [SerializeField] AudioSource TitleScreenMusic = null;
  [SerializeField] AudioSource InGameMusic = null;
  [SerializeField] AudioSource VictoryMusic = null;
  [SerializeField] AudioSource DrawMusic = null;

  Board Board;
  State CurrentState;
  bool IsPlayer1Turn;

  bool GameIsADraw {
    get {
      var isOver = true;
      for (var i = 0; i < Board.Size; i++) {
        for (var j = 0; j < Board.Size; j++) {
          isOver &= Board.Occupied(i, j);
        }
      }
      return isOver;
    }
  }

  bool PlayerWon {
    get {
      for (var i = 0; i < Board.Size; i++) {
        var wonHorizontal = true;
        for (var j = 0; j < Board.Size; j++) {
          wonHorizontal &= Board.IsOccupiedBy(i, j, IsPlayer1Turn);
        }
        if (wonHorizontal) {
          return true;
        }
      }

      for (var j = 0; j < Board.Size; j++) {
        var wonVertical = true;
        for (var i = 0; i < Board.Size; i++) {
          wonVertical &= Board.IsOccupiedBy(i, j, IsPlayer1Turn);
        }
        if (wonVertical) {
          return true;
        }
      }

      var wonBackslash = true;
      for (var i = 0; i < Board.Size; i++) {
        wonBackslash &= Board.IsOccupiedBy(i, i, IsPlayer1Turn);
      }
      if (wonBackslash) {
        return true;
      }

      var wonSlash = true;
      for (var i = 0; i < Board.Size; i++) {
        wonSlash &= Board.IsOccupiedBy(i, Board.Size - 1 - i, IsPlayer1Turn);
      }
      if (wonSlash) {
        return true;
      }

      return false;
    }
  }

  void Awake() {
    EnterTitleScreen();
  }

  void EnterTitleScreen() {
    TitleScreenMusic.Play();
    CurrentState = State.TitleScreen;
  }

  void StartNewGame() {
    Board = new Board(3);
    IsPlayer1Turn = true;
    DrawMusic.Stop();
    VictoryMusic.Stop();
    InGameMusic.Play();
    CurrentState = State.InGame;
  }

  void DeclareVictory() {
    InGameMusic.Stop();
    VictoryMusic.Play();
    CurrentState = State.Victory;
  }

  void DeclareADraw() {
    InGameMusic.Stop();
    DrawMusic.Play();
    CurrentState = State.Draw;
  }

  void ChangePlayerTurn() {
    IsPlayer1Turn = !IsPlayer1Turn;
  }

  void SelectCell(in int x, in int y) {
    Board.Occupy(x, y, IsPlayer1Turn);
    if (PlayerWon) {
      DeclareVictory();
    } else if (GameIsADraw) {
      DeclareADraw();
    } else {
      AudioSource.PlayClipAtPoint(OnPlayAudioClips[Random.Range(0, OnPlayAudioClips.Length)], Vector3.zero);
      ChangePlayerTurn();
    }
  }

  void OnGUI() {
    const string X = "X";
    const string O = "O";

    var rect = new Rect(0, 0, Screen.width, Screen.height);
    var style = new GUIStyle {
      alignment = TextAnchor.MiddleCenter,
      fontSize = 64
    };

    switch (CurrentState) {
      case State.TitleScreen: {
        if (GUI.Button(rect, $"Tic-Tac-Toe!!!\nClick to play", style)) {
          StartNewGame();
        }
      }
      break;

      case State.InGame: {
        for (var i = 0; i < Board.Size; i++) {
          for (var j = 0; j < Board.Size; j++) {
            RenderCell(i, j);
          }
        }
        Cursor.SetCursor(IsPlayer1Turn ? XCursor : OCursor, Vector2.zero, CursorMode.Auto);
      }
      break;

      case State.Victory: {
        var symbol = IsPlayer1Turn ? X : O;

        if (GUI.Button(rect, $"{symbol} won!\nStart a new game", style)) {
          StartNewGame();
        }
      }
      break;

      case State.Draw: {
        if (GUI.Button(rect, "It's a draw!\nStart a new game", style)) {
          StartNewGame();
        }
      }
      break;
    }
  }

  void RenderCell(in int x, in int y) {
    const string X = "X";
    const string O = "O";

    var width = Screen.width / Board.Size;
    var height = Screen.height / Board.Size;
    var rect = new Rect(x * width, y * height, width, height);
    var style = new GUIStyle {
      alignment = TextAnchor.MiddleCenter,
      fontSize = 128
    };

    if (Board.Occupied(x, y)) {
      GUI.Box(rect, Board.IsOccupiedBy(x, y, isPlayer1: true) ? X : O, style);
    } else {
      if (GUI.Button(rect, "")) {
        SelectCell(x, y);
      }
    }
  }
}