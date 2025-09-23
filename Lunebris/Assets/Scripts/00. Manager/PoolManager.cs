//System
using System.Collections.Generic;

// Engine
using UnityEngine;

[DisallowMultipleComponent]
public class PoolManager : MonoBehaviour
{
    // 추가: 적 전용 프리팹 (0-7번)
    [Header("Enemy Prefabs (0-7)")]
    [SerializeField] public GameObject[] enemyPrefabs; // 8개 적 프리팹
    [SerializeField] List<GameObject>[] enemyPools;

    // 추가: 총알 전용 프리팹
    [Header("Bullet Prefabs")]
    [SerializeField] public GameObject[] bulletPrefabs; // 총알 프리팹들
    [SerializeField] List<GameObject>[] bulletPools;

    // 추가: 보스 전용 프리팹
    [Header("Boss Prefabs")]
    [SerializeField] public GameObject midBoss1Prefab; // 중간보스1
    [SerializeField] public GameObject midBoss2Prefab; // 중간보스2
    [SerializeField] public GameObject finalBossPrefab; // 최종보스

    // 추가: 보스 전용 풀
    [SerializeField] List<GameObject> midBoss1Pool;
    [SerializeField] List<GameObject> midBoss2Pool;
    [SerializeField] List<GameObject> finalBossPool;

    // 풀 정리를 위한 타이머
    private float lastPoolCleanupTime = 0f;
    private const float POOL_CLEANUP_INTERVAL = 30f; // 30초마다 정리

    private void Awake()
    {
        // 추가: 적 풀 초기화
        enemyPools = new List<GameObject>[enemyPrefabs.Length];
        for (int i = 0; i < enemyPools.Length; i++)
        {
            enemyPools[i] = new List<GameObject>();
        }

        // 추가: 총알 풀 초기화
        bulletPools = new List<GameObject>[bulletPrefabs.Length];
        for (int i = 0; i < bulletPools.Length; i++)
        {
            bulletPools[i] = new List<GameObject>();
        }

        // 추가: 보스 풀 초기화
        midBoss1Pool = new List<GameObject>();
        midBoss2Pool = new List<GameObject>();
        finalBossPool = new List<GameObject>();
    }

    private void Update()
    {
        // 주기적으로 풀 정리
        if (Time.time - lastPoolCleanupTime > POOL_CLEANUP_INTERVAL)
        {
            CleanAllDestroyedObjects();
            lastPoolCleanupTime = Time.time;
        }
    }

    // 수정: 기존 Get 메소드를 총알용으로 변경 (하위 호환성)
    public GameObject Get(int index)
    {
        // PlayerAttack에서 사용하는 기존 코드 호환을 위해 GetBullet으로 연결
        return GetBullet(index);
    }

    // MODIFIED: 안전한 적 전용 Get 메소드
    /// <summary>
    /// Get enemy from enemy pools (0-7) with null safety
    /// </summary>
    /// <param name="index">Enemy index (0-7)</param>
    /// <returns></returns>
    public GameObject GetEnemy(int index)
    {
        if (index < 0 || index >= enemyPrefabs.Length)
        {
            Debug.LogError($"Invalid enemy index: {index}. Use 0-{enemyPrefabs.Length - 1}");
            return null;
        }

        if (enemyPrefabs[index] == null)
        {
            Debug.LogError($"Enemy prefab at index {index} is null!");
            return null;
        }

        GameObject select = null;

        // 파괴된 오브젝트들 먼저 정리
        CleanDestroyedEnemies(index);

        // 비활성 상태인 적을 찾아서 재사용
        foreach (GameObject item in enemyPools[index])
        {
            // 오브젝트가 존재하고 비활성 상태인지 확인
            if (item != null && !item.activeSelf)
            {
                select = item;
                select.SetActive(true);

                // 재활성화 메서드 호출
                Enemy_Base enemyScript = select.GetComponent<Enemy_Base>();
                if (enemyScript != null)
                {
                    enemyScript.OnPoolReactivated();
                }

                break;
            }
        }

        // 재사용할 수 있는 적이 없으면 새로 생성
        if (select == null)
        {
            select = Instantiate(enemyPrefabs[index], transform);
            enemyPools[index].Add(select);

            Debug.Log($"Created new enemy (index {index}). Pool size: {enemyPools[index].Count}");
        }

        return select;
    }

    // 추가: 총알 전용 Get 메소드
    /// <summary>
    /// Get bullet from bullet pools
    /// </summary>
    /// <param name="index">Bullet index</param>
    /// <returns></returns>
    public GameObject GetBullet(int index)
    {
        if (index < 0 || index >= bulletPrefabs.Length)
        {
            Debug.LogError($"Invalid bullet index: {index}. Use 0-{bulletPrefabs.Length - 1}");
            return null;
        }

        if (bulletPrefabs[index] == null)
        {
            Debug.LogError($"Bullet prefab at index {index} is null!");
            return null;
        }

        GameObject select = null;

        // 파괴된 오브젝트들 먼저 정리
        CleanDestroyedBullets(index);

        foreach (GameObject item in bulletPools[index])
        {
            if (item != null && !item.activeSelf)
            {
                select = item;
                select.SetActive(true);
                break;
            }
        }

        if (select == null)
        {
            select = Instantiate(bulletPrefabs[index], transform);
            bulletPools[index].Add(select);
        }

        return select;
    }

    // 기존: 보스 전용 Get 메소드 (안전성 추가)
    /// <summary>
    /// Get boss from separate boss pools
    /// </summary>
    /// <param name="bossID">8 for mid boss1, 9 for mid boss2, 10 for final boss</param>
    /// <returns></returns>
    public GameObject GetBoss(int bossID)
    {
        GameObject select = null;
        List<GameObject> targetPool = null;
        GameObject targetPrefab = null;

        switch (bossID)
        {
            case 8: // 중간보스1
                targetPool = midBoss1Pool;
                targetPrefab = midBoss1Prefab;
                break;
            case 9: // 중간보스2
                targetPool = midBoss2Pool;
                targetPrefab = midBoss2Prefab;
                break;
            case 10: // 최종보스
                targetPool = finalBossPool;
                targetPrefab = finalBossPrefab;
                break;
            default:
                Debug.LogError($"Invalid boss ID: {bossID}. Use 8 for MidBoss1, 9 for MidBoss2, 10 for FinalBoss");
                return null;
        }

        if (targetPrefab == null)
        {
            Debug.LogError($"Boss prefab for ID {bossID} is null!");
            return null;
        }

        // 파괴된 보스들 정리
        CleanDestroyedBosses(bossID);

        foreach (GameObject boss in targetPool)
        {
            if (boss != null && !boss.activeSelf)
            {
                select = boss;
                select.SetActive(true);

                // 보스도 Enemy_Base를 상속받는 경우 재활성화 메서드 호출
                Enemy_Base bossScript = select.GetComponent<Enemy_Base>();
                if (bossScript != null)
                {
                    bossScript.OnPoolReactivated();
                }

                break;
            }
        }

        if (select == null)
        {
            select = Instantiate(targetPrefab, transform);
            targetPool.Add(select);
        }

        return select;
    }

    // NEW: 파괴된 적들 정리
    /// <summary>
    /// Clean up destroyed objects from enemy pools
    /// </summary>
    /// <param name="poolIndex">Pool index to clean</param>
    private void CleanDestroyedEnemies(int poolIndex)
    {
        if (poolIndex < 0 || poolIndex >= enemyPools.Length) return;

        List<GameObject> enemiesToRemove = new List<GameObject>();

        foreach (GameObject enemy in enemyPools[poolIndex])
        {
            if (enemy == null)
            {
                enemiesToRemove.Add(enemy);
            }
        }

        foreach (GameObject enemy in enemiesToRemove)
        {
            enemyPools[poolIndex].Remove(enemy);
        }

        if (enemiesToRemove.Count > 0)
        {
            Debug.LogWarning($"Cleaned {enemiesToRemove.Count} destroyed enemies from pool {poolIndex}");
        }
    }

    // NEW: 파괴된 총알들 정리
    /// <summary>
    /// Clean up destroyed objects from bullet pools
    /// </summary>
    /// <param name="poolIndex">Pool index to clean</param>
    private void CleanDestroyedBullets(int poolIndex)
    {
        if (poolIndex < 0 || poolIndex >= bulletPools.Length) return;

        List<GameObject> bulletsToRemove = new List<GameObject>();

        foreach (GameObject bullet in bulletPools[poolIndex])
        {
            if (bullet == null)
            {
                bulletsToRemove.Add(bullet);
            }
        }

        foreach (GameObject bullet in bulletsToRemove)
        {
            bulletPools[poolIndex].Remove(bullet);
        }

        if (bulletsToRemove.Count > 0)
        {
            Debug.LogWarning($"Cleaned {bulletsToRemove.Count} destroyed bullets from pool {poolIndex}");
        }
    }

    // NEW: 파괴된 보스들 정리
    /// <summary>
    /// Clean up destroyed bosses from boss pools
    /// </summary>
    /// <param name="bossID">Boss ID to clean</param>
    private void CleanDestroyedBosses(int bossID)
    {
        List<GameObject> targetPool = null;

        switch (bossID)
        {
            case 8:
                targetPool = midBoss1Pool;
                break;
            case 9:
                targetPool = midBoss2Pool;
                break;
            case 10:
                targetPool = finalBossPool;
                break;
            default:
                return;
        }

        List<GameObject> bossesToRemove = new List<GameObject>();

        foreach (GameObject boss in targetPool)
        {
            if (boss == null)
            {
                bossesToRemove.Add(boss);
            }
        }

        foreach (GameObject boss in bossesToRemove)
        {
            targetPool.Remove(boss);
        }

        if (bossesToRemove.Count > 0)
        {
            Debug.LogWarning($"Cleaned {bossesToRemove.Count} destroyed bosses from boss pool {bossID}");
        }
    }

    // NEW: 모든 파괴된 오브젝트들 정리
    /// <summary>
    /// Clean all destroyed objects from all pools - call this periodically
    /// </summary>
    public void CleanAllDestroyedObjects()
    {
        int totalCleaned = 0;

        // 적 풀 정리
        for (int i = 0; i < enemyPools.Length; i++)
        {
            int beforeCount = enemyPools[i].Count;
            CleanDestroyedEnemies(i);
            totalCleaned += beforeCount - enemyPools[i].Count;
        }

        // 총알 풀 정리
        for (int i = 0; i < bulletPools.Length; i++)
        {
            int beforeCount = bulletPools[i].Count;
            CleanDestroyedBullets(i);
            totalCleaned += beforeCount - bulletPools[i].Count;
        }

        // 보스 풀 정리
        CleanDestroyedBosses(8);
        CleanDestroyedBosses(9);
        CleanDestroyedBosses(10);

        if (totalCleaned > 0)
        {
            Debug.Log($"Pool cleanup completed. Cleaned {totalCleaned} destroyed objects total.");
        }
    }

    // 추가: 적만 제거
    /// <summary>
    /// Clear all enemies (enemy pools only)
    /// </summary>
    public void ClearAllEnemies()
    {
        for (int i = 0; i < enemyPools.Length; i++)
        {
            foreach (GameObject enemy in enemyPools[i])
            {
                if (enemy != null && enemy.activeSelf)
                {
                    enemy.SetActive(false);
                }
            }
        }
    }

    // 추가: 총알만 제거
    /// <summary>
    /// Clear all bullets (bullet pools only)
    /// </summary>
    public void ClearAllBullets()
    {
        for (int i = 0; i < bulletPools.Length; i++)
        {
            foreach (GameObject bullet in bulletPools[i])
            {
                if (bullet != null && bullet.activeSelf)
                {
                    bullet.SetActive(false);
                }
            }
        }
    }

    // 추가: 보스만 제거
    /// <summary>
    /// Clear all bosses (boss pools only)
    /// </summary>
    public void ClearAllBosses()
    {
        foreach (GameObject boss in midBoss1Pool)
        {
            if (boss != null && boss.activeSelf)
            {
                boss.SetActive(false);
            }
        }

        foreach (GameObject boss in midBoss2Pool)
        {
            if (boss != null && boss.activeSelf)
            {
                boss.SetActive(false);
            }
        }

        foreach (GameObject boss in finalBossPool)
        {
            if (boss != null && boss.activeSelf)
            {
                boss.SetActive(false);
            }
        }
    }

    // 추가: 유틸리티 메소드들
    /// <summary>
    /// Get active enemy count
    /// </summary>
    public int GetActiveEnemyCount()
    {
        int count = 0;
        for (int i = 0; i < enemyPools.Length; i++)
        {
            foreach (GameObject enemy in enemyPools[i])
            {
                if (enemy != null && enemy.activeSelf)
                    count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Get active bullet count
    /// </summary>
    public int GetActiveBulletCount()
    {
        int count = 0;
        for (int i = 0; i < bulletPools.Length; i++)
        {
            foreach (GameObject bullet in bulletPools[i])
            {
                if (bullet != null && bullet.activeSelf)
                    count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Get active boss count
    /// </summary>
    public int GetActiveBossCount()
    {
        int count = 0;

        foreach (GameObject boss in midBoss1Pool)
        {
            if (boss != null && boss.activeSelf) count++;
        }

        foreach (GameObject boss in midBoss2Pool)
        {
            if (boss != null && boss.activeSelf) count++;
        }

        foreach (GameObject boss in finalBossPool)
        {
            if (boss != null && boss.activeSelf) count++;
        }

        return count;
    }

    /// <summary>
    /// Get total pool sizes (for debugging)
    /// </summary>
    public void LogPoolStatus()
    {
        Debug.Log("=== Pool Manager Status ===");

        for (int i = 0; i < enemyPools.Length; i++)
        {
            int active = 0, inactive = 0, nullRefs = 0;

            foreach (var enemy in enemyPools[i])
            {
                if (enemy == null) nullRefs++;
                else if (enemy.activeSelf) active++;
                else inactive++;
            }

            Debug.Log($"Enemy Pool {i}: Active={active}, Inactive={inactive}, Null={nullRefs}, Total={enemyPools[i].Count}");
        }

        for (int i = 0; i < bulletPools.Length; i++)
        {
            int active = 0, inactive = 0, nullRefs = 0;

            foreach (var bullet in bulletPools[i])
            {
                if (bullet == null) nullRefs++;
                else if (bullet.activeSelf) active++;
                else inactive++;
            }

            Debug.Log($"Bullet Pool {i}: Active={active}, Inactive={inactive}, Null={nullRefs}, Total={bulletPools[i].Count}");
        }

        Debug.Log($"Boss Pools - MidBoss1: {midBoss1Pool.Count}, MidBoss2: {midBoss2Pool.Count}, FinalBoss: {finalBossPool.Count}");
    }

    /// <summary>
    /// Clear everything except effects
    /// </summary>
    public void ClearAllCombatObjects()
    {
        ClearAllEnemies();
        ClearAllBullets();
        ClearAllBosses();
    }

    // 디버깅용 컨텍스트 메뉴
    [ContextMenu("Debug Pool Status")]
    public void DebugPoolStatus()
    {
        LogPoolStatus();
    }

    [ContextMenu("Clean All Destroyed Objects")]
    public void ForceCleanAllDestroyedObjects()
    {
        CleanAllDestroyedObjects();
    }

    [ContextMenu("Clear All Active Objects")]
    public void ForceClearAllActiveObjects()
    {
        ClearAllCombatObjects();
    }
}