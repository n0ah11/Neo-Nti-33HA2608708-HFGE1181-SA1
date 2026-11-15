using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigation : MonoBehaviour
{
    [SerializeField] private string sceneName;

    public void LoadScene()
    {
        // SA1: fade out/in transition (also lets the button click sound finish before the scene changes)
        SceneFader.FadeToScene(sceneName);
    }

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // SA1: so Exit visibly works when testing in the editor
#endif
    }
}
