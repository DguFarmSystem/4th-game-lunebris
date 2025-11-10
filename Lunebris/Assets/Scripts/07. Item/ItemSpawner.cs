using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance { get; private set; }

    [Header("--- 아이템 프리팹 목록 ---")]
    [Tooltip("일시적 아이템 프리팹 등록")]
    [SerializeField] private List<GameObject> temporaryItemPrefabs;

    [Tooltip("장기적 아이템 프리팹 등록")]
    [SerializeField] private List<GameObject> legacyItemPrefabs;

     
    [Header("--- 드랍 확률 설정 ---")]
    [Tooltip("일시적 아이템이 드랍될 확률(%)")]
    [Range(0f, 100f)]
    [SerializeField] private float temporaryItemDropChance = 15f;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // 이미 인스턴스가 있다면 새로 생긴 것은 파괴
        }
    }

    public void ItemSpawn(Vector3 spawnPosition) //일시적 아이템 드랍
    {
        if (temporaryItemPrefabs == null || temporaryItemPrefabs.Count == 0) //아이템이 없다면
        {
            Debug.LogWarning("ItemSpawner: 일시적 아이템 프리팹 목록이 비어있습니다!");
            return;
        }

        if (Random.Range(0f, 100f) <= temporaryItemDropChance)
        {
            int randomIndex = Random.Range(0, temporaryItemPrefabs.Count);
            GameObject itemToSpawn = temporaryItemPrefabs[randomIndex]; //랜덤 아이템

            Instantiate(itemToSpawn, spawnPosition, Quaternion.identity); //아이템 생성
        }
    }

    public void LegacySpawn(Vector3 spawnPosition) //장기적 아이템 드랍
    {
        if (legacyItemPrefabs == null || legacyItemPrefabs.Count == 0)
        {
            Debug.LogWarning("ItemSpawner: 장기적 아이템 프리팹 목록이 비어있습니다!");
            return;
        }

        for (int i = 0; i < 5; i++) //5개 드랍
        {
            // 장기적 아이템 목록에서 랜덤으로 하나 선택
            int randomIndex = Random.Range(0, legacyItemPrefabs.Count);
            GameObject itemToSpawn = legacyItemPrefabs[randomIndex];

            Vector3 randomOffset = new Vector3(Random.Range(-1.5f, 1.5f), 0.5f, Random.Range(-1.5f, 1.5f)); //흩뿌리기
            Instantiate(itemToSpawn, spawnPosition + randomOffset, Quaternion.identity); //아이템 생성
        }
    }
}