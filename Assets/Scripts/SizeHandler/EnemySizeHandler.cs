using System;
using UnityEngine.SceneManagement;
using UnityEngine;

public class EnemySizeHandler : UnitSizeHandler
{
    [Header("Feedback Settings")] 
    [SerializeField] protected Animator anim;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    
    protected override void Consume(GameObject unit)
    {
        if (unit.CompareTag("Player"))
        {
            TriggerBite();
            // SA1: show death feedback + Game Over screen instead of an instant silent reload
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.PlayerDied(unit);
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
        else
        {
            satiationValue += 25f;
            TriggerBite();
            base.Consume(unit);
        }
        
    }

    // SA1: an Animator that hasn't been evaluated yet (e.g. off-screen / just spawned) logs
    // "Animator is not playing an AnimatorController" on SetTrigger - rebind it first.
    private void TriggerBite()
    {
        if (anim == null) anim = GetComponent<Animator>();
        if (anim == null || anim.runtimeAnimatorController == null || !anim.isActiveAndEnabled) return;
        if (!anim.isInitialized) anim.Rebind();
        anim.SetTrigger("Bite");
    }

    private void OnDestroy()
    {
        if (SpawnManager.Instance != null) // SA1: guard during scene unload
            SpawnManager.Instance.CleanEnemyArray(this);
    }
}
