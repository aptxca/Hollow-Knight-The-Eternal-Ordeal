using System;
using System.Collections;
using Demo2D.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Demo2D
{
    public enum GameState
    {
        MainMenu,
        Gaming,
        Pause,
        Loading,
    }
    [DefaultExecutionOrder(999)]
    public class GameManager : MonoSingleton<GameManager>
    {
        public GameSettings gameSettings;
        public GameState state;
        public override void Awake()
        {
            if (!CheckSingleton(true))
            {
                return;
            }

            ApplyGameSettings(gameSettings);
            UIManager.Instance.Init();
            OnSceneLoaded(-1,SceneManager.GetActiveScene().buildIndex);
        }

        private int _finalScore;
        private bool _resetDamage;
        private bool _changingState = false;
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)&&!_changingState)
            {
                switch (state)
                {
                    case GameState.Gaming:
                        _changingState = true;
                        PauseGame();
                        break;
                    case GameState.Pause:
                        _changingState = true;
                        ResumeGame();
                        break;
                    case GameState.MainMenu:
                        break;
                }

            }

        }

        private void ApplyGameSettings(GameSettings gameSettings)
        {
            if (gameSettings==null)
            {
                Debug.Log("使用默认配置");
                Application.targetFrameRate = 60;
                return;
            }

            Application.targetFrameRate = gameSettings.frameRate;
        }



        public void LoadScene(int index, bool resetDamage)
        {
            _resetDamage = resetDamage;
            LoadSceneAsync(index);
        }

        private void LoadSceneAsync(int index)
        {
            StartCoroutine(LoadSceneCoroutine(index));
        }

        public IEnumerator LoadSceneCoroutine(int index)
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            BeforeSceneLoad(currentSceneIndex,index);
            ScreenFader faderOut = FindObjectOfType<ScreenFader>();
            if (faderOut!=null)
            {
                yield return faderOut.FadeInOutCoroutine(false, 1f);
            }

            AsyncOperation loadAsync = SceneManager.LoadSceneAsync(index);
            SceneManager.LoadScene("Loading", LoadSceneMode.Additive);
            StartCoroutine(GameObjectManager.Instance.ClearCacheAsync());

            while (!loadAsync.isDone)
            {
                yield return null;
            }

            SceneManager.UnloadSceneAsync("Loading");
            ScreenFader faderIn = FindObjectOfType<ScreenFader>();
            if (faderIn != null)
            {
                StartCoroutine(faderIn.FadeInOutCoroutine(true, 2f));
            }
            OnSceneLoaded(currentSceneIndex,index);
        }

        private void BeforeSceneLoad(int currentIndex, int nextIndex)
        {
            Time.timeScale = 1;
            state = GameState.Loading;
            if (nextIndex ==0||nextIndex==4)
            {
                if (PlayerController.Instance != null)
                {
                    PlayerController.Instance.gameObject.SetActive(false);
                }
            }
        }

        private void OnSceneLoaded(int prevIndex,int currentIndex)
        {
            if (currentIndex ==0)//主界面
            {
                UIManager.Instance.OpenMenu(MenuType.MainMenu);
                state = GameState.MainMenu;
            }
            else if(currentIndex==4)//游戏结算
            {
                Debug.Log("gameover: " + _finalScore);

                GameObject scoreObj = GameObject.Find("/GameOverSence");
                if (scoreObj!=null)
                {
                    GameOverScene gameOver = scoreObj.GetComponent<GameOverScene>();
                    if (gameOver!=null)
                    {
                        Debug.Log("_finalScore: "+ _finalScore);
                        gameOver.Open(_finalScore);
                    }
                }
            }
            else
            {
                GameSceneManager.Instance.StartGameScene(_resetDamage);
                UIManager.Instance.OpenMenu(MenuType.Hud);
                state = GameState.Gaming;
            }
        }

        public void processUIRequest(UiButtonCmd cmd)
        {
            switch (cmd)
            {
                case UiButtonCmd.Start:
                    StartGame();
                    break;
                case UiButtonCmd.Resume:
                    ResumeGame();
                    break;
                case UiButtonCmd.BackToMainMenu:
                    BackToMainMenu();
                    break;
                case UiButtonCmd.ExitGame:
                    ExitGame();
                    break;
            }
        }

        public void StartGame()
        {
            UIManager.Instance.CloseAllMenu(() => LoadScene(2, true));
        }

        public void BackToMainMenu()
        {
            UIManager.Instance.CloseAllMenu(() => LoadScene(0, true));
        }

        public void PauseGame()
        {
            UIManager.Instance.Pause(() =>
            {
                Time.timeScale = 0;
                state = GameState.Pause;
                _changingState = false;
                if (PlayerInput.Instance != null)
                {
                    PlayerInput.Instance.BlockInput = true;
                }
            });
        }

        public void GameOver(int killCount)
        {
            _finalScore = killCount;
            UIManager.Instance.CloseAllMenu(() => LoadScene(4, true));
        }

        public void ResumeGame()
        {
            UIManager.Instance.Resume(() =>
            {
                Time.timeScale = 1;
                state = GameState.Gaming;
                _changingState = false;
                if (PlayerInput.Instance != null)
                {
                    PlayerInput.Instance.BlockInput = false;
                }

            });
        }

        public void ExitGame()
        {
            Application.Quit();
        }
    }
}