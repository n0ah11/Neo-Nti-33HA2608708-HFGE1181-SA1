using UnityEngine;

public class SpawnParticleOnExit:StateMachineBehaviour
{
    public float duration;
    public GameObject particlePrefab;
    public Transform spawnPoint;

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // SA1: a StateMachineBehaviour lives on the Animator Controller asset and cannot reference a scene/prefab
        // Transform, so fall back to the animated object's own position when spawnPoint is empty.
        Vector3 position = spawnPoint != null ? spawnPoint.position : animator.transform.position;
        GameObject particle = Instantiate(particlePrefab, position, Quaternion.identity);
        particle.transform.localScale = animator.transform.localScale; // scale the burst with the donut
        Destroy(particle,duration);
    }
}
