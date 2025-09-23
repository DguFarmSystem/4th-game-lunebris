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

    // Tank (근거리)
    private int luxTankID = 0;
    private int teneTankID = 1;

    // Ranged Dealer (원거리)
    private int luxRangeDealerID = 2;
    private int teneRangeDealerID = 3;

    // Assassin (어쌔신)
    private int luxAssassin = 4;
    private int teneAssassin = 5;

    // Mage (마법사)
    private int luxMageDealerID = 6;
    private int teneMageDealerID = 7;

    [Header("Boss Prefab ID")]
    [SerializeField] private int middleBoss1PrefabID = 8;
    [SerializeField] private int middleBoss2PrefabID = 9;
    [SerializeField] private int finalBossPrefabID = 10;

    [Header("Spawn Points, Criteria is Player")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Boss Spawn Point")]
    [SerializeField] private Transform bossSpawnPoint;

    [Header("Max Min Spawn Time")]
    [SerializeField] private float minSpawnTime = 3f;
    [SerializeField] private float maxSpawnTime = 5f;

    [Header("Phase"), Range(1, 3)]
    [SerializeField] private int phase;

    private float elapsedTime;
    private float lastMidBossTime = 0f;
    private bool finalBossSpawned = false;
    private bool gameEnded = false;

    // 일반 몹 추적을 위한 리스트
    private List<GameObject> activeEnemies = new List<GameObject>();

    private void Start()
    {
        // Start Spawn Enemy Coroutine
        StartCoroutine(SpawnEnemy());

        // Init Phase
        phase = 1;
    }

    private void Update()
    {
        // 게임이 끝났으면 업데이트 중단
        if (gameEnded) return;

        // Check Phase
        CheckPhase();

        // Adjust Spawn Time (Spawn more frequently as time goes by)
        AdjustSpawnTime();

        // Check Mid Boss Spawn (every 5 minutes)
        CheckMidBossSpawn();

        // Check Final Boss Spawn (at 30 minutes)
        CheckFinalBossSpawn();
    }

    /// <summary>
    /// Check Phase based on elapsed time
    /// </summary>
    private void CheckPhase()
    {
        elapsedTime = timer.ElapsedTime;

        if (elapsedTime < 600f) // 0-10분
            phase = 1;
        else if (elapsedTime < 1200f) // 10-20분
            phase = 2;
        else if (elapsedTime < 1800f) // 20-30분
            phase = 3;
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
    /// Check if mid boss should spawn (every 5 minutes)
    /// </summary>
    private void CheckMidBossSpawn()
    {
        if (elapsedTime >= 1800f) return; // 30분 후에는 중간보스 소환 중단

        float currentInterval = Mathf.Floor(elapsedTime / 300f); // 5분마다
        float lastInterval = Mathf.Floor(lastMidBossTime / 300f);

        if (currentInterval > lastInterval && currentInterval > 0) // 첫 5분 후부터 시작
        {
            SpawnMidBoss();
            lastMidBossTime = elapsedTime;
        }
    }

    /// <summary>
    /// Check if final boss should spawn (at 30 minutes)
    /// </summary>
    private void CheckFinalBossSpawn()
    {
        if (elapsedTime >= 1800f && !finalBossSpawned) // 30분 = 1800초
        {
            SpawnFinalBoss();
            finalBossSpawned = true;
            gameEnded = true;
        }
    }

    /// <summary>
    /// Spawn Mid Boss (randomly select from 2 types)
    /// </summary>
    private void SpawnMidBoss()
    {
        Transform spawnPoint = bossSpawnPoint != null ? bossSpawnPoint : spawnPoints[0];

        // 2개의 중간보스 중 랜덤 선택
        int randomMidBossID = Random.Range(0, 2) == 0 ? middleBoss1PrefabID : middleBoss2PrefabID;

        GameObject midBoss = pool.GetBoss(randomMidBossID);
        midBoss.transform.position = spawnPoint.position;

        Debug.Log($"Mid Boss (ID: {randomMidBossID}) spawned at {elapsedTime / 60f:F1} minutes");
    }

    /// <summary>
    /// Spawn Final Boss and clear all regular enemies
    /// </summary>
    private void SpawnFinalBoss()
    {
        // 모든 일반 몹 제거
        ClearAllEnemies();

        // 파이널 보스 소환
        Transform spawnPoint = bossSpawnPoint != null ? bossSpawnPoint : spawnPoints[0];
        GameObject finalBoss = pool.GetBoss(finalBossPrefabID);
        finalBoss.transform.position = spawnPoint.position;

        Debug.Log("Final Boss spawned! All regular enemies cleared.");
    }

    /// <summary>
    /// Clear all regular enemies from the scene
    /// </summary>
    private void ClearAllEnemies()
    {
        // Pool에서 활성화된 모든 일반 몹들을 비활성화
        pool.ClearAllEnemies();

        // 활성 적 리스트 클리어
        activeEnemies.Clear();
    }

    /// <summary>
    /// For Enemy Spawn
    /// </summary>
    /// <returns></returns>
    private IEnumerator SpawnEnemy()
    {
        // Base Spawn Routine
        while (!gameEnded)
        {
            // 30분이 지나면 일반 몹 소환 중단
            if (elapsedTime >= 1800f)
            {
                yield break;
            }

            // Random Spawn Time
            float randomSpawnTime = Random.Range(minSpawnTime, maxSpawnTime);

            // Delay
            yield return new WaitForSeconds(randomSpawnTime);

            // 다시 한번 체크 (코루틴 중에 시간이 지날 수 있음)
            if (elapsedTime >= 1800f || gameEnded)
            {
                yield break;
            }

            // Get Index for Random Index
            int randPos = Random.Range(0, spawnPoints.Length);

            // Spawn Random Enemy
            GameObject enemy = pool.GetEnemy(GetEnemyID());
            enemy.transform.position = spawnPoints[randPos].position; // 버그 수정: gameObject -> enemy

            // 활성 적 리스트에 추가
            if (!activeEnemies.Contains(enemy))
            {
                activeEnemies.Add(enemy);
            }
        }
    }

    /// <summary>
    /// Select Enemy according to phase
    /// </summary>
    private int GetEnemyID()
    {
        List<int> candidateIDs = new List<int>();

        switch (phase)
        {
            case 1: // 0-10분: 근거리 + 원거리
                candidateIDs.Add(luxTankID);
                candidateIDs.Add(teneTankID);
                candidateIDs.Add(luxRangeDealerID);
                candidateIDs.Add(teneRangeDealerID);
                break;

            case 2: // 10-20분: 근거리 + 원거리 + 어쌔신
                candidateIDs.AddRange(new int[]
                {
                    luxTankID, teneTankID, luxRangeDealerID, teneRangeDealerID,
                    luxAssassin, teneAssassin
                });
                break;

            case 3: // 20-30분: 근거리 + 원거리 + 어쌔신 + 마법사
                candidateIDs.AddRange(new int[]
                {
                    luxTankID, teneTankID, luxRangeDealerID, teneRangeDealerID,
                    luxAssassin, teneAssassin, luxMageDealerID, teneMageDealerID
                });
                break;
        }

        int rand = Random.Range(0, candidateIDs.Count);
        return candidateIDs[rand];
    }

    /// <summary>
    /// Remove enemy from active list when it's destroyed or deactivated
    /// </summary>
    public void RemoveEnemy(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
    }
}