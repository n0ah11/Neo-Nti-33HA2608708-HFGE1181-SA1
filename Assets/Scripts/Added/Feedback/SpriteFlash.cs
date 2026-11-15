using System.Collections;
using UnityEngine;

/// SA1 addition: short colour tint on a sprite for hit feedback.
public class SpriteFlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer target;
    [SerializeField] private float duration = 0.18f;
    private Coroutine running;

    public void Flash(Color c)
    {
        if (target == null) return;
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(DoFlash(c));
    }

    private IEnumerator DoFlash(Color c)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            target.color = Color.Lerp(c, Color.white, t / duration);
            yield return null;
        }
        target.color = Color.white;
    }
}
