using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RetainedModeTicTacToe {
  public abstract class GameState : MonoBehaviour {
    public virtual void OnEnter(RetainedModeTicTacToe game) {}
    public virtual void OnExit(RetainedModeTicTacToe game) {}
    public abstract void Step(RetainedModeTicTacToe game, float dt);
  }
  
  public class RetainedModeTicTacToe : MonoBehaviour {
    public enum State { Base, Unloading, Loading }

    public string TitleScreenSceneName;

    Queue<string> SceneNamesToLoad = new Queue<string>(1);
    string SceneNameToLoad;
    AsyncOperation LoadingSceneOperation;
    string LoadedSceneName;
    GameState GameState;
    State CurrentState;

    public void LoadScene(string sceneName) {
      SceneNamesToLoad.Enqueue(sceneName);
    }

    void Start() {
      LoadScene(TitleScreenSceneName);
    }

    void Update() {
      switch (CurrentState) {
        case State.Base: {
          if (SceneNamesToLoad.Count > 0) {
            if (GameState) {
              GameState.OnExit(this);
              SceneNameToLoad = SceneNamesToLoad.Dequeue();
              LoadingSceneOperation = SceneManager.UnloadSceneAsync(LoadedSceneName);
              CurrentState = State.Unloading;
            } else {
              SceneNameToLoad = SceneNamesToLoad.Dequeue();
              LoadingSceneOperation = SceneManager.LoadSceneAsync(SceneNameToLoad, LoadSceneMode.Additive);
              CurrentState = State.Loading;
            }
          } else {
            GameState.Step(this, Time.deltaTime);
          }
        }
        break;

        case State.Unloading: {
          if (LoadingSceneOperation.isDone) {
            LoadingSceneOperation = SceneManager.LoadSceneAsync(SceneNameToLoad, LoadSceneMode.Additive);
            CurrentState = State.Loading;
          }
        }
        break;

        case State.Loading: {
          if (LoadingSceneOperation.isDone) {
            LoadingSceneOperation = null;
            LoadedSceneName = SceneNameToLoad;
            GameState = GameStateFromScene(LoadedSceneName);
            GameState.OnEnter(this);
            CurrentState = State.Base;
          }
        }
        break;
      }
    }

    GameState GameStateFromScene(string sceneName) {
      foreach (var gameObject in SceneManager.GetSceneByName(sceneName).GetRootGameObjects()) {
        if (gameObject.TryGetComponent(out GameState gameState)) {
          return gameState;
        }
      }
      return null;
    }
  }
}