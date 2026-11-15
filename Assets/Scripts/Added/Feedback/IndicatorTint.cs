using UnityEngine;

/// SA1 addition: colours a TargetIndicator arrow - red = bigger than you (danger), green = you can eat it.
public class IndicatorTint : MonoBehaviour
{
    [SerializeField] private TargetIndicator indicator;
    [SerializeField] private SpriteRenderer[] renderers;
    [SerializeField] private Color dangerColour = new Color(1f, 0.25f, 0.3f);
    [SerializeField] private Color preyColour = new Color(0.35f, 1f, 0.45f);
    private SizeHandler enemy;
    private SizeHandler player;

    private void Start()
    {
        var target = indicator != null ? indicator.GetTarget() : null;
        if (target) enemy = target.GetComponent<SizeHandler>();
        var follow = transform.parent != null ? transform.parent.GetComponent<FollowTarget>() : null;
        if (follow && follow.GetTarget()) player = follow.GetTarget().GetComponent<SizeHandler>();
    }

    private void Update()
    {
        if (enemy == null || player == null) return;
        // keep the arrow readable as the camera zooms out with the player's size
        transform.localScale = Vector3.one * Mathf.Sqrt(player.GetSize());
        Color c = enemy.GetSize() > player.GetSize() ? dangerColour : preyColour;
        foreach (var r in renderers) if (r) r.color = c;
    }
}
