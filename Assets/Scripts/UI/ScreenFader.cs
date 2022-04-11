using System.Collections;
using UnityEngine;

namespace Demo2D
{
    public class ScreenFader : MonoBehaviour
    {
        public CanvasGroup canvasGroup;
        public float fadeInTime;
        public float fadeOutTime;

        public void FadeIn()
        {
            StartCoroutine(FadeInOutCoroutine(true, fadeInTime));
        }

        public void FadeOut()
        {
            StartCoroutine(FadeInOutCoroutine(false, fadeOutTime));
        }


        public IEnumerator FadeInOutCoroutine(bool fadeIn, float fadeTime)
        {
            float time = fadeTime;

            if (fadeIn)
            {
                //Debug.Log("fade in");
                canvasGroup.alpha = 1f;
                while (time >= 0)
                {
                    canvasGroup.alpha -= 1 / fadeTime * Time.deltaTime;
                    time -= Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                canvasGroup.alpha = 0f;
                //Debug.Log("fade out");
                while (time >= 0)
                {
                    canvasGroup.alpha += 1 / fadeTime * Time.deltaTime;
                    time -= Time.deltaTime;
                    yield return null;
                }
            }

        }

    }
}