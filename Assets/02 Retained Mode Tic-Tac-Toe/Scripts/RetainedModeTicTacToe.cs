using UnityEngine;
using UnityEngine.SceneManagement;

namespace RetainedModeTicTacToe {
  public abstract class GameState : MonoBehaviour {
    public virtual void OnEnter(RetainedModeTicTacToe game) {}
    public virtual void OnExit(RetainedModeTicTacToe game) {}
    public abstract void Step(RetainedModeTicTacToe game, float dt);
  }

  public class RetainedModeTicTacToe : MonoBehaviour {
    public string TitleScreenSceneName;
    public AsyncOperation SceneLoad;
    public AsyncOperation SceneUnload;

    GameState GameState;
    string OutgoingSceneName;
    string IncomingSceneName;

    public void LoadNewGameStateScene(string sceneName) {
      OutgoingSceneName = IncomingSceneName;
      IncomingSceneName = sceneName;
      SceneLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }

    // TODO: Decide wtf to do here...
    public void ReloadCurrentGameStateScene(string sceneName) {
      Debug.LogError("NOT IMPLEMENTED RELOAD CURRENT GAME STATE SCENE");
    }

    public void SetGameState(GameState gameState) {
      GameState.OnExit(this);
      GameState = gameState;
      GameState.OnEnter(this);
    }

    public GameState GameStateFromScene(string sceneName) {
      foreach (var gameObject in SceneManager.GetSceneByName(IncomingSceneName).GetRootGameObjects()) {
        if (gameObject.TryGetComponent(out GameState gameState)) {
          return gameState;
        }
      }
      return null;
    }

    void Awake() {
      LoadNewGameStateScene(TitleScreenSceneName);
    }

    void Update() {
      if (SceneLoad != null && SceneLoad.isDone) {
        SceneLoad = null;
        if (GameState) {
          GameState.OnExit(this);
        }
        GameState = GameStateFromScene(IncomingSceneName);
        GameState.OnEnter(this);
        if (OutgoingSceneName != null && OutgoingSceneName != "") {
          Debug.Log(OutgoingSceneName);
          SceneUnload = SceneManager.UnloadSceneAsync(OutgoingSceneName);
        }
      }

      if (SceneUnload != null && SceneUnload.isDone) {
        SceneUnload = null;
      }

      if (GameState != null) {
        GameState.Step(this, Time.deltaTime);
      }
    }
  }
}