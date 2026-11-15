using System;
using UnityEngine;

public abstract class SizeHandler : MonoBehaviour
{
    [Header("Size Parameters")]
    [SerializeField] protected float size;
    [SerializeField] protected bool isSmaller = false;
    protected float satiationValue = 0f;

    // SA1: tunable attraction strength so pull speed can be balanced per prefab in the Inspector
    [Header("Attraction")]
    [SerializeField] protected float pullStrength = 1f;

    protected virtual void Update()
    {
        transform.localScale = new Vector3(size, size, size);
    }

    public virtual float GetSize()
    {
        return size;
    }

    public virtual void SetSize(float newSize)
    {
        size = newSize;
    }

    // SA1: exposed so the HUD can show progress towards the next growth
    public virtual float GetSatiation()
    {
        return satiationValue;
    }
    
    protected virtual void MoveTowardsCenter(GameObject target)
    {
        SizeHandler targetSize = target.GetComponent<SizeHandler>();
        if (targetSize == null) return; // SA1: null guard
        float distance = Vector3.Distance(transform.position, target.transform.position);
        // SA1: original "/distance*distance" cancelled itself out (operator precedence), so the pull never
        // got stronger near the centre. Brackets restore the intended inverse-square pull; distance is
        // clamped so the speed can't become infinite. MoveTowards never overshoots the centre.
        float d = Mathf.Max(distance, 0.1f);
        transform.position = Vector3.MoveTowards(transform.position, target.transform.position, 
            pullStrength * ((size * targetSize.GetSize())/(d*d)) * Time.deltaTime);
    }
    
    protected virtual void OnTriggerEnter2D(Collider2D col)
    {
        SizeHandler other = col.gameObject.GetComponent<SizeHandler>();
        if (!other)
        {
            return;
        }
        
        if (other.GetSize() > size)
        {
            isSmaller = true;
        }
    }
    
    protected virtual void OnTriggerStay2D(Collider2D col)
    {
        SizeHandler other = col.gameObject.GetComponent<SizeHandler>();
        if (other == null) return; // SA1: walls/UI colliders have no SizeHandler (was a NullReferenceException)
        if (other.GetSize() > size)
        {
            MoveTowardsCenter(col.gameObject);
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D col)
    {
        isSmaller = false;
    }
}
