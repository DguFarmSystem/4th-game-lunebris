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

    // 수정: 기존 Get 메소드를 총알용으로 변경 (하위 호환성)
    public GameObject Get(int index)
    {
        // PlayerAttack에서 사용하는 기존 코드 호환을 위해 GetBullet으로 연결
        return GetBullet(index);
    }

    // 추가: 적 전용 Get 메소드
    /// <summary>
    /// Get enemy from enemy pools (0-7)
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

        GameObject select = null;

        foreach (GameObject item in enemyPools[index])
        {
            if (!item.activeSelf)
            {
                select = item;
                select.SetActive(true);
                break;
            }
        }

        if (select == null)
        {
            select = Instantiate(enemyPrefabs[index], transform);
            enemyPools[index].Add(select);
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

        GameObject select = null;

        foreach (GameObject item in bulletPools[index])
        {
            if (!item.activeSelf)
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

    // 기존: 보스 전용 Get 메소드
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

        foreach (GameObject boss in targetPool)
        {
            if (!boss.activeSelf)
            {
                select = boss;
                select.SetActive(true);
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
                if (enemy.activeSelf)
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
                if (bullet.activeSelf)
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
            if (boss.activeSelf)
            {
                boss.SetActive(false);
            }
        }

        foreach (GameObject boss in midBoss2Pool)
        {
            if (boss.activeSelf)
            {
                boss.SetActive(false);
            }
        }

        foreach (GameObject boss in finalBossPool)
        {
            if (boss.activeSelf)
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
                if (enemy.activeSelf)
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
                if (bullet.activeSelf)
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
            if (boss.activeSelf) count++;
        }

        foreach (GameObject boss in midBoss2Pool)
        {
            if (boss.activeSelf) count++;
        }

        foreach (GameObject boss in finalBossPool)
        {
            if (boss.activeSelf) count++;
        }

        return count;
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
}