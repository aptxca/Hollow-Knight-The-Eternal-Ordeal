using System.Collections;
using System.Collections.Generic;
using Demo2D;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;


namespace Demo2D
{
    public class ChallengeStatue : Interactable
    {
        public Light2D light2D;
        public float lightChangeSpeed;
        public float maxLightIntensity;

        public override bool CheckCondition()
        {
            return PlayerInput.Instance.LookInput > 0.5f;
        }

        public override void OnTriggerStay2D(Collider2D col)
        {
            base.OnTriggerStay2D(col);
            if (light2D.intensity<maxLightIntensity)
            {
                light2D.intensity += lightChangeSpeed * Time.fixedDeltaTime;
            }
        }

        public override void OnTriggerExit2D(Collider2D col)
        {
            base.OnTriggerExit2D(col);
            StartCoroutine(LightFade());

        }

        IEnumerator LightFade()
        {
            while (light2D.intensity > 0.01f)
            {
                light2D.intensity -= lightChangeSpeed * Time.deltaTime;
                yield return null;
            }

        }
    }
}