using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    [Header("기본 설정")]
    [SerializeField] private PoolManager pool;
    [SerializeField] private Transform centerPoint; // 보스 스폰 위치 (없으면 자동 설정)

    [Header("일반 몬스터")]
    [SerializeField] private int[] regularEnemyIDs = { 1, 2, 3, 4 };
    [SerializeField] private float spawnInterval = 2f; // 스폰 간격
    [SerializeField] private int maxEnemies = 20; // 최대 몬스터 수
    [SerializeField] private float spawnRadius = 15f; // 스폰 반지름

    [Header("보스 타이밍")]
    [SerializeField] private int[] middleBossIDs = { 101, 102 };
    [SerializeField] private float middleBossInterval = 300f; // 5분 (300초)
    [SerializeField] private int[] finalBossIDs = { 201, 202, 203 };
    [SerializeField] private float finalBossTime = 1800f; // 30분 (1800초)

    // 내부 변수
    private float gameStartTime;
    private float lastMiddleBossTime;
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private bool finalBossSpawned = false;

    #region 초기화

    private void Start()
    {
        gameStartTime = Time.time;
        lastMiddleBossTime = gameStartTime;

        // 자동 설정
        if (pool == null) pool = FindObjectOfType<PoolManager>();
        if (centerPoint == null) centerPoint = transform;

        // 스폰 시작
        StartCoroutine(SpawnRoutine());
        StartCoroutine(BossRoutine());
    }

    #endregion

    #region 메인 스폰 루틴

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            // 최종보스 있으면 일반 몬스터 스폰 중지
            if (finalBossSpawned)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            // 최대 몬스터 수 체크
            CleanupDeadEnemies();
            if (spawnedEnemies.Count < maxEnemies)
            {
                SpawnRandomEnemy();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private IEnumerator BossRoutine()
    {
        while (!finalBossSpawned)
        {
            float gameTime = Time.time - gameStartTime;

            // 최종보스 시간 체크 (30분)
            if (gameTime >= finalBossTime)
            {
                SpawnFinalBoss();
                break;
            }

            // 중간보스 시간 체크 (5분마다)
            if (Time.time - lastMiddleBossTime >= middleBossInterval)
            {
                SpawnMiddleBoss();
                lastMiddleBossTime = Time.time;
            }

            yield return new WaitForSeconds(10f); // 10초마다 체크
        }
    }

    #endregion

    #region 스폰 메서드

    private void SpawnRandomEnemy()
    {
        if (pool == null || regularEnemyIDs.Length == 0) return;

        // 랜덤 몬스터 선택
        int randomID = regularEnemyIDs[Random.Range(0, regularEnemyIDs.Length)];

        // 랜덤 위치 생성
        Vector2 randomPos = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(randomPos.x, 0, randomPos.y);

        // 몬스터 스폰
        GameObject enemy = pool.Get(randomID);
        if (enemy != null)
        {
            enemy.transform.position = spawnPos;
            spawnedEnemies.Add(enemy);
        }
    }

    private void SpawnMiddleBoss()
    {
        if (pool == null || middleBossIDs.Length == 0) return;

        // 랜덤 중간보스 선택
        int randomID = middleBossIDs[Random.Range(0, middleBossIDs.Length)];

        // 원거리 위치 생성
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector3 spawnPos = transform.position + new Vector3(randomDir.x, 0, randomDir.y) * 25f;

        // 중간보스 스폰
        GameObject boss = pool.Get(randomID);
        if (boss != null)
        {
            boss.transform.position = spawnPos;
            Debug.Log($"중간보스 등장! ID: {randomID}");
        }
    }

    private void SpawnFinalBoss()
    {
        if (pool == null || finalBossIDs.Length == 0) return;

        // 모든 몬스터 제거
        ClearAllEnemies();

        // 중립 보스 스폰 (첫 번째)
        int finalBossID = finalBossIDs[0];
        GameObject finalBoss = pool.Get(finalBossID);

        if (finalBoss != null)
        {
            finalBoss.transform.position = centerPoint.position;
            finalBossSpawned = true;
            Debug.Log($"최종보스 등장!! ID: {finalBossID}");
        }
    }

    #endregion

    #region 정리

    private void CleanupDeadEnemies()
    {
        spawnedEnemies.RemoveAll(enemy => enemy == null || !enemy.activeInHierarchy);
    }

    public void ClearAllEnemies()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null && enemy.activeInHierarchy)
            {
                enemy.SetActive(false);
            }
        }
        spawnedEnemies.Clear();
    }

    #endregion

    #region 유틸리티

    /// <summary>
    /// 현재 게임 시간 (분:초)
    /// </summary>
    public string GetGameTimeString()
    {
        float gameTime = Time.time - gameStartTime;
        int minutes = Mathf.FloorToInt(gameTime / 60f);
        int seconds = Mathf.FloorToInt(gameTime % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// 다음 중간보스까지 남은 시간 (초)
    /// </summary>
    public float GetTimeToNextBoss()
    {
        if (finalBossSpawned) return 0f;
        return Mathf.Max(0, middleBossInterval - (Time.time - lastMiddleBossTime));
    }

    /// <summary>
    /// 최종보스까지 남은 시간 (초)
    /// </summary>
    public float GetTimeToFinalBoss()
    {
        if (finalBossSpawned) return 0f;
        float gameTime = Time.time - gameStartTime;
        return Mathf.Max(0, finalBossTime - gameTime);
    }

    // 정보 확인용
    public int GetEnemyCount() => spawnedEnemies.Count;
    public bool IsFinalBossActive() => finalBossSpawned;

    #endregion

    #region 디버그 (Context Menu)

    [ContextMenu("Spawn Middle Boss")]
    public void DebugSpawnMiddleBoss() => SpawnMiddleBoss();

    [ContextMenu("Spawn Final Boss")]
    public void DebugSpawnFinalBoss() => SpawnFinalBoss();

    [ContextMenu("Clear All")]
    public void DebugClearAll() => ClearAllEnemies();

    #endregion
}