using UnityEngine;

namespace RetainedModeTicTacToe {
  public class TitleScreenState : GameState {
    public override void Step(RetainedModeTicTacToe game, float dt) {
      if (Input.anyKeyDown) {
        game.LoadScene("RetainedMode TicTacToe InGame");
      }
    }
  }
}