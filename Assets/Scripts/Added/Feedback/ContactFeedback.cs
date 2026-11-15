using UnityEngine;

/// SA1 addition: sound + particle when the player touches an enemy (brief: "collides with the enemy").
public class ContactFeedback : MonoBehaviour
{
    [SerializeField] private string soundName = "Hit";
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private float particleLifetime = 1.5f;
    [SerializeField] private float cooldown = 0.35f;
    [SerializeField] private SpriteFlash flash;
    private float lastTime = -10f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        if (Time.time - lastTime < cooldown) return;
        lastTime = Time.time;

        SoundManager.Instance?.Play(soundName);

        if (particlePrefab)
        {
            Vector3 p = Vector3.Lerp(transform.position, other.transform.position, 0.5f);
            GameObject fx = Instantiate(particlePrefab, p, Quaternion.identity);
            fx.transform.localScale = Vector3.one * Mathf.Max(transform.localScale.x, 0.5f);
            Destroy(fx, particleLifetime);
        }

        // red flash if the enemy is the dangerous (bigger) one, white if it's prey
        var mine = GetComponent<SizeHandler>();
        var theirs = other.GetComponent<SizeHandler>();
        if (flash && mine && theirs)
            flash.Flash(theirs.GetSize() > mine.GetSize() ? new Color(1f, 0.35f, 0.35f) : new Color(1f, 1f, 0.7f));
    }
}
