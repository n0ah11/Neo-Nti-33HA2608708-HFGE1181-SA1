using UnityEngine;

/// SA1 addition: turns the on-screen joystick + pause button on for phones (or when simulating in the editor).
/// The joystick itself uses the Input System's OnScreenStick, which feeds the Move action through <Gamepad>/leftStick.
public class MobileControls : MonoBehaviour
{
    public static bool UseTouchControls { get; private set; }

    [SerializeField] private bool simulateInEditor = false;
    [SerializeField] private GameObject[] touchOnlyObjects;
    [SerializeField] private GameObject[] desktopOnlyObjects;

    private void Awake()
    {
        UseTouchControls = Application.isMobilePlatform || (Application.isEditor && simulateInEditor);
        foreach (var go in touchOnlyObjects) if (go) go.SetActive(UseTouchControls);
        foreach (var go in desktopOnlyObjects) if (go) go.SetActive(!UseTouchControls);
    }
}
