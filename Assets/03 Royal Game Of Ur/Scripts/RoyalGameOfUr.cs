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
    public bool IsSafe;
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

  /*
  The architecture of these games is as follows:

    The input system queries the RawInputs and its own state to build up player actions.
    These actions are not raw however, because the Input System queries the Game itself
    to ask about the legitimacy of various actions. Once the InputSystem believes it has formulated
    a valid move, it sends it to the Game as a transaction / action / command. At this point,
    several things happen: The game's logical state is updated and the event is sent to any
    connected systems that wish to know about it. This architecture is somewhat like a virtual machine
    in that the "op-codes" for permuting the game's state are the key events in the game and the virtual
    machines that receive are independent responders to these updates. Specifically, the rendering layer
    of the game is decoupled from the logical layer of the game such that logical play may continue
    while smoother visuals are playing out. Additionally, the audio for the game may be separated into
    logical audio events and rendering audio events. What this really means is captured in the flows below:

    InputSystem(LogicalGameState, RawInput) -> Actions

    LogicalGameState(Action)
    LogicalRenderingSystem(Action)
    LogicalAudioSystem(Action)
    RendererState(Action)
      RenderingAudioSystem
    
    Effectively, a single Action produced by the InputSystem will get processed by four independent virtual
    machines that maintain their own state and respond in their own way. These systems should not be aware 
    of one-another to avoid entangling them or sharing state.

    The design goal of this architecture is to decouple the logical rules and data structures for a given game
    from the way it is presented. This allows new games to be prototyped with some ease and also allows the 
    rendering to evolve independently. Most importantly, it allows logical gameplay to proceeed instantly and 
    thus frees the players from needing to wait for the renderers to catch up to their pace of play. This is important
    for creating the illusion or feeling of responsiveness in a game and SHOULD be a benefit of playing a board game
    on a computer: namely that you do not need to perform the mechanical actions to execute a play or wait for 
    these turns to happen arbitrarily.

    From an architectural perspective, this means that the renderer should be an event processor that does not 
    immediately respond to the new events placed in its event queue but rather stores them in a dependency-tree
    and executes them when the actors participating in the event are first available. For example, if we have 
    a game like Chess in which 1 piece might attack another piece, we would specifiy this visual event as 
    "Piece1 attack Piece2". Our virtual machine knows that it will act on both piece1 and piece2 and therefore 
    it must wait to execute this move until both pieces are available. The simplest way to do this of course is 
    serial execution in which events are strictly processed in the order in which they are recieved. You could also
    use concurrent execution which, for example, would allow "Move Piece 3 to Position 1" and "Piece1 attack Piece2"
    to concurrently-execute as they share no state. However, this is easier said than done because it's entirely
    possible that they DO share state due to, for example, their need to move through the board. Perhaps naively 
    executing these two events concurrently would result in passes needing to occupy the same visual spaces during their
    travels. This sort of dependency is much trickier to thoroughly vet though it's certainly entirely possible.

    I do think it makes the most sense to simply assume that the renderer is a serial-execution list and that will
    probably work for most circumstances.

    Now, a final thought about the InputSystem's relationship to the Logical Game State. It would probably make sense
    to regard the InputSystem as a "trusted agent" of the gamestate meaning that it is the last line of defense for
    preventing erroneous moves from being processed by the game state. One could certainly program this relationship
    in a more defensive manner by having the gamestate check all paramaters passed to it to ensure they are logical 
    and valid. However, because the InputSystem needs to restrict player inputs to meaningful actions ( for the benefit 
    of the player ) this checking will already be happening. It feels tedious and redundant to do it over and over so
    I propose that the following fundamental flow of control is what happens even though it is spread across multiple
    systems:

      GameState sets InputSystemState
      InputSystem processes RawInput against validation checks provided by the GameState
      InputSystem sends ValidatedEvent to GameState for processing
  */

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

    public bool CanMove(Player player, in int pieceIndex, in int amount) {
      var piecePosition = player.PiecePositions[pieceIndex];
      var routeIndex = Array.IndexOf(player.Route, piecePosition);
      var candidateRouteIndex = routeIndex + amount;
      var canMoveOnRoute = candidateRouteIndex <= player.Route.LastIndex();

      if (canMoveOnRoute) {
        var candidateRoutePosition = player.Route[candidateRouteIndex];

        if (PositionToTile.TryGetValue(candidateRoutePosition, out Tile tile)) {
          var isNotAlreadyOccupiedByThisPlayer = Array.IndexOf(player.PiecePositions, candidateRoutePosition) < 0;

          if (isNotAlreadyOccupiedByThisPlayer && !tile.IsSafe) {
            return true;
          } else {
            return false;
          }
        } else {
          return true;
        }
      } else {
        return false;
      }
    }

    // Transactions
    public void RollDice() {
      for (var i = 0; i < Dice.Length; i++) {
        Dice[i] = Random.Range(0, 2) == 1;
      }
    }

    public void MovePiece(Player player, int pieceIndex, int routeIndex) {
      var piece = player.PiecePositions[pieceIndex];
      var routePosition = player.Route[routeIndex];

      player.PiecePositions[pieceIndex] = player.Route[routeIndex];
      do {
        IsPlayer1Turn = !IsPlayer1Turn;
        RollDice();
      } while (DiceCount == 0);
    }


    // LifeCycle 
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