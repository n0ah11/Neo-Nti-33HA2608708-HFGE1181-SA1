using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;
    
    [Header("General Settings")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private UnitSizeHandler playerSizeHandler;
    [SerializeField] private Vector2 spawnOffsetMinimum;
    [SerializeField] private Vector2 spawnOffsetMaximum;
    [SerializeField] private float spawnTime;
    [SerializeField] private LayerMask overlapLayers;
    private float timer;
    
    [Header("Enemy Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float enemyMaximumSize;
    [SerializeField] private float enemyMinimumSize;
    [SerializeField] private GameObject targetIndicatorPrefab;
    [SerializeField] private Transform targetIndicatorParent;
    [SerializeField] private float targetIndicatorOffset;
    

    
    [Header("Food Spawn Settings")]
    [SerializeField] private GameObject foodPrefab;
    [SerializeField] private float foodMaximumSize;
    [SerializeField] private float foodMinimumSize;
    
    // SA1: caps so a 10 minute run doesn't flood the level (and the phone) with hundreds of objects
    [Header("SA1 Limits")]
    [SerializeField] private int maxEnemies = 14;
    [SerializeField] private int maxFood = 30;
    [SerializeField] private int maxSpawnAttempts = 25;
    private int spawnAttempts;

    [Header("Indexing")]
    [SerializeField] private List<UnitSizeHandler> enemySizeHandlers;
    [SerializeField]private List<SizeHandler> foodSizeHandlers;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        
        if (GameStateManager.Instance.GetGameState() != GameState.Playing) // SA1: also stop after Game Over
        {
            return;
        }
        
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = spawnTime;
            spawnAttempts = 0;
            if (enemySizeHandlers.Count < maxEnemies) SpawnEnemy();
            spawnAttempts = 0;
            if (foodSizeHandlers.Count < maxFood) SpawnFood();
            spawnAttempts = 0;
            if (foodSizeHandlers.Count < maxFood) SpawnFood(); // food spawns twice as often as enemies
        }
        
        CheckEnemySize();
        CheckFoodSize();
    }

    public void CleanEnemyArray(UnitSizeHandler e)
    {
        enemySizeHandlers.Remove(e);
    }
    
    public void CheckEnemySize()
    {
        foreach (var e in enemySizeHandlers)
        {
            if (e.GetSize() < playerSizeHandler.GetSize() * enemyMinimumSize)
            {
                DestroyEnemy(e);
            }
        }
        
    }

    public void SpawnEnemy()
    {
        Vector2 randomPosition = GetRandomPosition();
        float randomSpawnSize = Random.Range(playerSizeHandler.GetSize() * enemyMinimumSize, playerSizeHandler.GetSize() * enemyMaximumSize);
        if (CheckIfOccupied(randomPosition, randomSpawnSize) || IsNearPlayer(randomPosition))
        {
            if (++spawnAttempts < maxSpawnAttempts) SpawnEnemy(); // SA1: recursion guard
        }
        else
        {
            GameObject enemy = Instantiate(enemyPrefab, randomPosition, Quaternion.identity);
        
            enemy.GetComponent<SizeHandler>().SetSize(randomSpawnSize);
            enemySizeHandlers.Add(enemy.GetComponent<UnitSizeHandler>());
            
            SpawnTargetIndicator(enemy.transform);
        }
    }

    public void SpawnTargetIndicator(Transform target)
    {
        GameObject targetIndicator = Instantiate(targetIndicatorPrefab, targetIndicatorParent.position, Quaternion.identity);
        targetIndicator.transform.SetParent(targetIndicatorParent);
        targetIndicator.GetComponent<TargetIndicator>().SetTarget(target);
        
    }

    public void DestroyEnemy(UnitSizeHandler enemyToBeDestroyed)
    {
        Destroy(enemyToBeDestroyed.gameObject, 1f);
    }

    public void CleanFoodArray(SizeHandler f)
    {
        foodSizeHandlers.Remove(f);
    }

    public void CheckFoodSize()
    {
        foreach (var f in foodSizeHandlers)
        {
            if (f.GetSize() < playerSizeHandler.GetSize() * foodMinimumSize)
            {
                DestroyFood(f);
            }
        }
    }
    
    public void SpawnFood()
    {
        Vector2 randomPosition = GetRandomPosition();
        float randomSpawnSize = Random.Range(playerSizeHandler.GetSize() * foodMinimumSize, playerSizeHandler.GetSize() * foodMaximumSize);
        if (CheckIfOccupied(randomPosition, randomSpawnSize))
        {
            if (++spawnAttempts < maxSpawnAttempts) SpawnFood(); // SA1: recursion guard
        }
        else
        {
            GameObject food = Instantiate(foodPrefab, randomPosition, Quaternion.identity);
            food.GetComponent<SizeHandler>().SetSize(randomSpawnSize);
            foodSizeHandlers.Add(food.GetComponent<SizeHandler>());
        }
    }

    public void DestroyFood(SizeHandler foodToBeDestroyed)
    {
        Destroy(foodToBeDestroyed.gameObject, 1f);
    }

    private Vector2 GetRandomPosition()
    {
        return new Vector2(Random.Range(spawnOffsetMinimum.x, spawnOffsetMaximum.x), Random.Range(spawnOffsetMinimum.y, spawnOffsetMaximum.y));
    }

    // SA1: never drop an enemy right on top of the player
    private bool IsNearPlayer(Vector2 position)
    {
        return Vector2.Distance(position, playerSizeHandler.transform.position) < 4f + playerSizeHandler.GetSize() * 2f;
    }

    public Transform GetSpawnPoint()
    {
        return spawnPoint;
    }

    private bool CheckIfOccupied(Vector2 randomPosition, float size)
    {
        Collider2D hit = Physics2D.OverlapCircle(randomPosition, Mathf.Max(size/2, 0.5f), overlapLayers); // SA1: min radius keeps tiny spawns off wall edges
        if (hit == null)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

}
