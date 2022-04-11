using UnityEngine;

namespace Demo2D.Utility
{
    public static class LayMaskExtend
    {
        public static bool ContainLayer(this LayerMask mask, int layer)
        {
            return (1 << layer & mask.value) != 0;
        }
    }

    public static class AnimatorExtend
    {
        public static bool IsCurrentStateEnd(this Animator animator, int layerIndex)
        {
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layerIndex);

            return !current.loop && current.normalizedTime > 1f;
        }
    }
}