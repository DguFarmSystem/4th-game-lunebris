// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    [Header("Pool Manager")]
    [SerializeField] private PoolManager pool;

    [Header("Timer")]
    [SerializeField] private Timer timer;

    // Temp
    [Header("Prefab ID")]

    // Tank
    private int luxTankID = 0;
    private int teneTankID = 1;

    // Ranged Dealer
    private int luxRangeDealerID = 2;
    private int teneRangeDealerID = 3;

    // Assassin
    private int luxAssassin = 4;
    private int teneAssassin = 5;

    // Mage
    private int luxMageDealerID = 4;
    private int teneMageDealerID = 5;

    [Range(8,9)]
    [SerializeField] private int middleBoosPrefabID;

    [SerializeField] private int finalBossPrefabID;

    [Header("Spawn Points, Criteria is Player")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Max Min Spawn Time")]
    [SerializeField] private float minSpawnTime = 3f;
    [SerializeField] private float maxSpawnTime = 5f;

    [Header("Phase"), Range(1,6)]
    [SerializeField] private int phase;
    [SerializeField] private float converPhaseTime;

    private float elapsedTime;

    private void Start()
    {
        // Start Spawn Enemy Coroutine
        StartCoroutine(SpawnEnemy());

        // Init Phase
        phase = 1;

        converPhaseTime *= 60; // M2S
    }

    private void Update()
    {
        // Check Phase
        CheckPhase();

        // Adjust Spawn Time (Spawn more frequently as time goes by)
        AdjustSpawnTime();
    }

    /// <summary>
    /// Check Phase
    /// </summary>
    private void CheckPhase()
    {
        elapsedTime = timer.ElapsedTime;

        if (elapsedTime < converPhaseTime) phase = 1;
        else if (elapsedTime < converPhaseTime * 2) phase = 2;
        else phase = 3;
    }

    /// <summary>
    /// Adjust Spawn Time
    /// </summary>
    private void AdjustSpawnTime()
    {
        minSpawnTime = Mathf.Max(0.5f, 3f - (elapsedTime / 300f) * 0.2f);
        maxSpawnTime = Mathf.Max(1f, 5f - (elapsedTime / 300f) * 0.3f);
    }

    /// <summary>
    /// For Enemy Spawn
    /// </summary>
    /// <returns></returns>
    private IEnumerator SpawnEnemy()
    {
        // Base Spawn Routine
        while (true)
        {
            // Random Spawn Time
            float randomSpawnTime = Random.Range(minSpawnTime, maxSpawnTime);

            // Delay
            yield return new WaitForSeconds(randomSpawnTime);

            // Get Index for Random Index
            int randPos = Random.Range(0, spawnPoints.Length);

            // Spawn Random Enemy
            GameObject enemy = pool.Get(GetEnemyID());
            gameObject.transform.position = spawnPoints[randPos].position;
        }
    }

    /// <summary>
    /// Select Enemy according to time
    /// </summary>
    private int GetEnemyID()
    {
        List<int> candidateIDs = new List<int>();

        switch (phase)
        {
            case 1:
                candidateIDs.Add(luxTankID);
                candidateIDs.Add(teneTankID);
                candidateIDs.Add(luxRangeDealerID);
                candidateIDs.Add(teneRangeDealerID);
                break;

            case 2:
                candidateIDs.AddRange(new int[]
                {
                    luxTankID, teneTankID, luxRangeDealerID, teneRangeDealerID,
                    luxAssassin, teneAssassin
                });
                break;

            case 3:
                candidateIDs.AddRange(new int[]
                {
                    luxTankID, teneTankID, luxRangeDealerID, teneRangeDealerID,
                    luxAssassin, teneAssassin, luxMageDealerID, teneMageDealerID
                }) ;
                break;
        }

        int rand = Random.Range(0, candidateIDs.Count);
        return candidateIDs[rand];
    }
}
