using UnityEngine;
using UnityEngine.UI;

namespace Demo2D
{
    public class Hud : MenuController
    {
        public HealthBar healthBar;
        public GameObject scoreCounter;
        private Text _scoreText;

        protected override void init()
        {
            base.init();
            _scoreText = scoreCounter.GetComponentInChildren<Text>(true);
            if (_scoreText == null)
            {
                Debug.Log("找不到得分Text组件");
            }
        }

        public void RegisterPlayerStatus(Damageable damageable)
        {
            if (healthBar == null)
            {
                Debug.Log("找不到血条UI组件");
                return;
            }
            healthBar.Init(damageable);
        }
        


        public void OnScoreChange(int score)
        {
            if (score == 0)
            {
                if (scoreCounter.activeInHierarchy)
                {
                    scoreCounter.SetActive(false);
                }
            }
            else
            {
                if (!scoreCounter.activeInHierarchy)
                {
                    scoreCounter.SetActive(true);
                }

                if (_scoreText != null)
                {
                    _scoreText.text = score.ToString();
                }
            }

        }
    }
}