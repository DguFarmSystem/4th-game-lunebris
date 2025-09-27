using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 내 모든 아이템 스폰을 관리하는 싱글톤 클래스입니다.
/// </summary>
public class ItemSpawner : MonoBehaviour
{
    // 1. 싱글톤 인스턴스: 어디서든 ItemSpawner.Instance 로 접근 가능
    public static ItemSpawner Instance { get; private set; }

    [Header("--- 아이템 프리팹 목록 ---")]
    [Tooltip("일반 몬스터가 드랍할 일시적 아이템 프리팹들을 여기에 등록하세요.")]
    [SerializeField] private List<GameObject> temporaryItemPrefabs;

    [Tooltip("보스가 드랍할 장기적 아이템 프리팹들을 여기에 등록하세요.")]
    [SerializeField] private List<GameObject> legacyItemPrefabs;


    [Header("--- 드랍 확률 설정 ---")]
    [Tooltip("일시적 아이템이 드랍될 확률을 % 단위로 설정합니다. (예: 10 = 10%)")]
    [Range(0f, 100f)]
    [SerializeField] private float temporaryItemDropChance = 15f;


    private void Awake()
    {
        // 2. 싱글톤 패턴 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 이 오브젝트는 파괴되지 않음
        }
        else
        {
            Destroy(gameObject); // 이미 인스턴스가 있다면 새로 생긴 것은 파괴
        }
    }

    /// <summary>
    /// [외부 호출용] 일반 몬스터가 죽었을 때 호출할 함수입니다.
    /// 설정된 확률에 따라 일시적 아이템 1개를 스폰합니다.
    /// </summary>
    /// <param name="spawnPosition">아이템이 생성될 위치 (보통 몬스터가 죽은 위치)</param>
    public void ItemSpawn(Vector3 spawnPosition)
    {
        // 프리팹 목록이 비어있으면 함수를 즉시 종료
        if (temporaryItemPrefabs == null || temporaryItemPrefabs.Count == 0)
        {
            Debug.LogWarning("ItemSpawner: 일시적 아이템 프리팹 목록이 비어있습니다!");
            return;
        }

        // 3. 확률 계산 (0~100 사이의 랜덤 숫자 뽑기)
        if (Random.Range(0f, 100f) <= temporaryItemDropChance)
        {
            // 4. 아이템 랜덤 선택 및 생성
            // 리스트 인덱스는 0부터 시작하므로, 0 ~ (리스트 크기 - 1) 사이의 정수를 뽑음
            int randomIndex = Random.Range(0, temporaryItemPrefabs.Count);
            GameObject itemToSpawn = temporaryItemPrefabs[randomIndex];

            // 선택된 아이템을 지정된 위치에 생성
            Instantiate(itemToSpawn, spawnPosition, Quaternion.identity);
        }
    }

    /// <summary>
    /// [외부 호출용] 보스가 죽었을 때 호출할 함수입니다.
    /// 장기적 아이템 5개를 랜덤하게 스폰합니다.
    /// </summary>
    /// <param name="spawnPosition">아이템들이 생성될 중심 위치</param>
    public void LegacySpawn(Vector3 spawnPosition)
    {
        if (legacyItemPrefabs == null || legacyItemPrefabs.Count == 0)
        {
            Debug.LogWarning("ItemSpawner: 장기적 아이템 프리팹 목록이 비어있습니다!");
            return;
        }

        // 5. 기획에 따라 5번 반복
        for (int i = 0; i < 5; i++)
        {
            // 장기적 아이템 목록에서 랜덤으로 하나 선택
            int randomIndex = Random.Range(0, legacyItemPrefabs.Count);
            GameObject itemToSpawn = legacyItemPrefabs[randomIndex];

            // 6. 아이템이 겹치지 않게 주변에 흩뿌리기 위한 랜덤 위치 오프셋
            Vector3 randomOffset = new Vector3(Random.Range(-1.5f, 1.5f), 0.5f, Random.Range(-1.5f, 1.5f));

            // 중심 위치 + 랜덤 오프셋 위치에 아이템 생성
            Instantiate(itemToSpawn, spawnPosition + randomOffset, Quaternion.identity);
        }
    }
}