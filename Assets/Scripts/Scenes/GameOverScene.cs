using UnityEngine;
using UnityEngine.UI;

namespace Demo2D
{
    public class GameOverScene : MonoBehaviour
    {
        public int nextSceneIndex;
        public Text scoreText;

        public Animator animator;
        public AudioSource gameOverSound;

        private bool isBacking = false;

        public void Update()
        {
            if (Input.anyKeyDown && !isBacking)
            {
                isBacking = true;
                GameManager.Instance.BackToMainMenu();
            }
        }

        public void Open(int score)
        {
            scoreText.text = score.ToString();
            animator.Play("Open",0,0);
            gameOverSound.Play();

        }
    }
}