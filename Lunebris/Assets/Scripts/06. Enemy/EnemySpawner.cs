// System
using System.Collections;

// Unity
using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private PoolManager pool;
    [SerializeField] private int enemyPrefabID;

    [SerializeField] private Transform[] spawnPoints;

    [SerializeField] private float minSpawnTime = 3f;
    [SerializeField] private float maxSpawnTime = 5f;

    private void Start()
    {
        enemyPrefabID = 1;
        StartCoroutine(SpawnEnemy());
    }

    private IEnumerator SpawnEnemy()
    {
        while (true)
        {
            float randomSpawnTime = Random.Range(minSpawnTime, maxSpawnTime);

            yield return new WaitForSeconds(randomSpawnTime);

            int randomIndex = Random.Range(0, spawnPoints.Length);

            GameObject gameObject = pool.Get(enemyPrefabID);
            gameObject.transform.position = spawnPoints[randomIndex].position;
        }
    }
}
