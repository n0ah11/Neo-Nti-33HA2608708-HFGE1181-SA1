using UnityEngine;

public class PlayerSizeHandler : UnitSizeHandler
{
    [Header("Feedback Settings")] 
    [SerializeField] protected Animator anim;

    // SA1: cap so the player still fits through the larger building doorways late in a 10 minute run
    [Header("Growth Limit")]
    [SerializeField] private float maxSize = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    protected override void HandleSize()
    {
        if (satiationValue >= 100f)
        {
            satiationValue = 0f;
            if (size < maxSize)
            {
                size = Mathf.Min(size + .25f, maxSize);
                anim.SetTrigger("Grow");
            }
        }
    }

    protected override void MoveTowardsCenter(GameObject target)
    {
        if (target.CompareTag("Enemy"))
        {
            base.MoveTowardsCenter(target);
        }
        else if (target.CompareTag("Food"))
        {
            // the player is never pulled into food; food is pulled into the player (FoodSizeHandler)
        }
        
    }

    protected override void Consume(GameObject unit)
    {
        if (unit.GetComponent<SizeHandler>().GetSize() < size) // SA1: direct size check (isSmaller flag could be stale)
        {
            satiationValue += 25f;
            anim.SetTrigger("Bite");
            Destroy(unit);
        }
    }
    
}
