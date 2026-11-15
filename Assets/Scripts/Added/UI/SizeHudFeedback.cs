using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// SA1 addition: HUD feedback - satiation bar towards the next growth and a "punch" on the size text when you grow.
public class SizeHudFeedback : MonoBehaviour
{
    [SerializeField] private PlayerSizeHandler player;
    [SerializeField] private Image satiationFill;
    [SerializeField] private RectTransform sizeText;
    [SerializeField] private TextMeshProUGUI growPopup;
    private float lastSize;
    private float punch;
    private float popupTimer;

    private void Start()
    {
        lastSize = player.GetSize();
        if (growPopup) growPopup.alpha = 0f;
    }

    private void Update()
    {
        if (satiationFill)
            satiationFill.fillAmount = Mathf.Lerp(satiationFill.fillAmount, player.GetSatiation() / 100f, 12f * Time.unscaledDeltaTime);

        if (player.GetSize() > lastSize + 0.001f)
        {
            punch = 1f;
            popupTimer = 1.2f;
            if (satiationFill) satiationFill.fillAmount = 0f;
        }
        lastSize = player.GetSize();

        punch = Mathf.MoveTowards(punch, 0f, 3f * Time.unscaledDeltaTime);
        if (sizeText) sizeText.localScale = Vector3.one * (1f + 0.35f * punch);

        if (growPopup)
        {
            popupTimer -= Time.unscaledDeltaTime;
            growPopup.alpha = Mathf.Clamp01(popupTimer);
        }
    }
}
