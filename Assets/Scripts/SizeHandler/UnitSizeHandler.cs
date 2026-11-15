using UnityEngine;

public abstract class UnitSizeHandler : SizeHandler
{
    protected override void Update()
    {
        transform.localScale = new Vector3(size, size, size);
        HandleSize();
    }

    protected virtual void HandleSize()
    {
        if (satiationValue >= 100f)
        {
            size += .25f;
            satiationValue = 0f;
        }
    }
    
    protected virtual void Consume(GameObject unit)
    {
        // SA1: size is already checked by the caller (OnTriggerStay2D), so the stale isSmaller flag is not used here
        {
            satiationValue += 25f;
            Destroy(unit);
        }
    }
    
    protected override void OnTriggerStay2D(Collider2D col)
    {
        SizeHandler other = col.gameObject.GetComponent<SizeHandler>();
        if (other == null) return; // SA1: null guard

        if (other.GetSize() > size)
        {
            MoveTowardsCenter(col.gameObject);
        }
        
        // SA1: only a LARGER unit may consume. The shared isSmaller flag is reset by ANY trigger exit,
        // which let a small player "eat" a big enemy (and food larger than itself) in crowded areas.
        // SA1: "reached the centre" uses a small size-relative tolerance instead of a fixed 0.015 units.
        // The food snaps to the centre in its own trigger callback, but when the eater moves, its callback can
        // run first each physics step, so a fixed 0.015 check only ever passed while standing still.
        float centreTolerance = Mathf.Max(0.015f, 0.08f * size);
        if (other.GetSize() < size && Vector2.Distance(transform.position, col.transform.position) <= centreTolerance)
        {
            Consume(col.gameObject);
        }
    }
    
}
