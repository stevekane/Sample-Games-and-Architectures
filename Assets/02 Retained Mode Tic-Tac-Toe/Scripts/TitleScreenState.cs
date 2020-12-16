using UnityEngine;

namespace RetainedModeTicTacToe {
  public class TitleScreenState : GameState {
    public override void Step(RetainedModeTicTacToe game, float dt) {
      if (game.SceneLoad == null && Input.anyKeyDown) {
        game.LoadNewGameStateScene("RetainedMode TicTacToe InGame");
      }
    }
  }
}