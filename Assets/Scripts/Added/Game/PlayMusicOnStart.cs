using UnityEngine;

/// SA1 addition: starts a looping SoundManager track when the scene starts.
public class PlayMusicOnStart : MonoBehaviour
{
    [SerializeField] private string soundName = "Music";

    private void Start()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.Play(soundName);
    }
}
