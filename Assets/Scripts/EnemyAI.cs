using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyAI : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private float wanderDistance;
    [SerializeField] private float wanderSpeed;
    [SerializeField] private float idleTime;

    // SA1: enemies move with physics so the wall colliders keep them inside the level,
    // pick destinations inside the playable area, give up on a target if blocked, and face where they go.
    [Header("SA1 Navigation")]
    [SerializeField] private Rect playArea = new Rect(2.5f, 2.5f, 67f, 43f);
    [SerializeField] private float stuckTime = 1.5f;
    [SerializeField] private float turnSpeed = 8f;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        StartCoroutine(IdleAtLocation());
    }

    public Vector3 SelectNewLocation()
    {
        
        Vector3 newLocation = new Vector3(Random.Range(transform.position.x - wanderDistance, transform.position.x + wanderDistance),
            Random.Range(transform.position.y - wanderDistance, transform.position.y + wanderDistance),0);
        newLocation.x = Mathf.Clamp(newLocation.x, playArea.xMin, playArea.xMax);
        newLocation.y = Mathf.Clamp(newLocation.y, playArea.yMin, playArea.yMax);
        return newLocation;
    }

    public IEnumerator IdleAtLocation()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;
        yield return new WaitForSecondsRealtime(idleTime * Random.Range(0.6f, 1.4f));
        StartCoroutine(MoveTowardsLocation());
    }
    
    public IEnumerator MoveTowardsLocation()
    {
        
        Vector3 newLocation = SelectNewLocation();
        float blockedTimer = 0f;
        float lastDistance = float.MaxValue;
        while (Vector2.Distance(transform.position, newLocation) > 0.1f)
        {
            if (GameStateManager.Instance.GetGameState() == GameState.Paused && rb != null) rb.linearVelocity = Vector2.zero;
            yield return new WaitWhile(() => GameStateManager.Instance.GetGameState() == GameState.Paused); 

            Vector2 dir = ((Vector2)(newLocation - transform.position)).normalized;
            float speed = wanderSpeed * (0.75f + 0.25f * transform.localScale.x);
            if (rb != null) rb.linearVelocity = dir * speed;
            else transform.position = Vector3.MoveTowards(transform.position, newLocation, speed * Time.deltaTime);

            if (dir.sqrMagnitude > 0.001f)
                transform.up = Vector3.Slerp(transform.up, dir, turnSpeed * Time.deltaTime);

            // blocked by a wall? pick somewhere else
            float d = Vector2.Distance(transform.position, newLocation);
            if (d > lastDistance - 0.002f) blockedTimer += Time.deltaTime; else blockedTimer = 0f;
            lastDistance = d;
            if (blockedTimer > stuckTime) break;
            yield return null;
        }
        
        yield return StartCoroutine(IdleAtLocation());    
    }


    
}
