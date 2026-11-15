using UnityEngine;

/// SA1 addition: each food picks one of the provided crumb/glaze sprites and a random spin.
public class FoodVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] variants;
    [SerializeField] private float bobAmount = 0.06f;
    [SerializeField] private float bobSpeed = 3f;
    private float phase;
    private Vector3 baseScale;

    private void Start()
    {
        if (variants != null && variants.Length > 0)
            spriteRenderer.sprite = variants[Random.Range(0, variants.Length)];
        spriteRenderer.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        phase = Random.Range(0f, 10f);
        baseScale = spriteRenderer.transform.localScale;
    }

    private void Update()
    {
        float s = 1f + Mathf.Sin((Time.time + phase) * bobSpeed) * bobAmount;
        spriteRenderer.transform.localScale = baseScale * s;
    }
}
