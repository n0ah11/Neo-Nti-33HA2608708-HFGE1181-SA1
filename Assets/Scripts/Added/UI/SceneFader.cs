using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// SA1 addition: fade-to-black scene transition. Creates itself on first use and survives scene loads.
public class SceneFader : MonoBehaviour
{
    private static SceneFader instance;
    private Image overlay;
    private bool busy;
    private const float FadeTime = 0.45f;

    private static SceneFader Get()
    {
        if (instance != null) return instance;
        var go = new GameObject("SceneFader");
        DontDestroyOnLoad(go);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        go.AddComponent<CanvasScaler>();
        var img = new GameObject("Overlay").AddComponent<Image>();
        img.transform.SetParent(go.transform, false);
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        img.color = new Color(0.16f, 0.07f, 0.02f, 0f);
        img.raycastTarget = false;
        instance = go.AddComponent<SceneFader>();
        instance.overlay = img;
        return instance;
    }

    public static void FadeToScene(string sceneName)
    {
        var f = Get();
        if (!f.busy) f.StartCoroutine(f.Run(sceneName));
    }

    // fade in when a scene opens directly (e.g. first launch)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnFirstScene()
    {
        var f = Get();
        f.overlay.color = new Color(f.overlay.color.r, f.overlay.color.g, f.overlay.color.b, 1f);
        f.StartCoroutine(f.Fade(1f, 0f));
    }

    private IEnumerator Run(string sceneName)
    {
        busy = true;
        overlay.raycastTarget = true; // block double clicks
        yield return Fade(0f, 1f);
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync(sceneName);
        yield return Fade(1f, 0f);
        overlay.raycastTarget = false;
        busy = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        Color c = overlay.color;
        while (t < FadeTime)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(from, to, t / FadeTime);
            overlay.color = c;
            yield return null;
        }
        c.a = to;
        overlay.color = c;
    }
}
