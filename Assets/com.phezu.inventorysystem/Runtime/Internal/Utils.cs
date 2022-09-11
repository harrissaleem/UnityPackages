using System.Collections;
using UnityEngine;

namespace Phezu.InventorySystem.Internal
{
    public readonly struct Utils
    {
        public static IEnumerator TweenScaleIn(GameObject obj, float durationInFrames, Vector3 maxScale) 
        {
            Transform tf = obj.transform;
            tf.localScale = Vector3.zero;
            tf.gameObject.SetActive(true);

            float frame = 0;
            while (frame <= durationInFrames) 
            {
                tf.localScale = Vector3.Lerp(Vector3.zero, maxScale, frame / durationInFrames);
                frame++;
                yield return null;
            }
        }
        public static IEnumerator TweenScaleOut(GameObject obj, float durationInFrames, bool destroy)
        {
            float frame = 0;
            while (frame < durationInFrames)
            {
                if (obj != null)
                {
                    obj.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, frame / durationInFrames);
                }
                frame++;
                yield return null;
            }
            if (obj)
            {
                if (!destroy) obj.SetActive(false);
                else GameObject.Destroy(obj);
            }
        }
    }
}
