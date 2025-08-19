using UnityEngine;
using Enemy;

/// <summary>
/// 분열된 작은 탄환 - 폭발 탄환에서 분열되어 나오는 서브 탄환
/// </summary>
public class Enemy_Ranged_SubBullet : MonoBehaviour
{
    [Header("탄환 설정")]
    public float bulletSpeed = 8f;               // 탄환 속도 (메인보다 약간 느림)
    public float lifetime = 3f;                  // 탄환 수명

    [Header("시각적 효과")]
    public GameObject hitEffect;                 // 충돌 시 이펙트
    public AudioClip hitSound;                   // 충돌 시 사운드

    // Initialize에서 설정되는 값들
    private float bulletDamage;                  // 탄환 데미지
    private ElementType bulletElementType;       // 탄환 속성
    private DamageType bulletDamageType;         // 데미지 타입

    // 내부 컴포넌트
    private Rigidbody bulletRigidbody;
    private bool hasHit = false;                 // 충돌 완료 여부

    #region Unity Lifecycle

    private void Awake()
    {
        bulletRigidbody = GetComponent<Rigidbody>();

        // 수명 제한
        Destroy(gameObject, lifetime);
    }

    private void Start()
    {
        // 이미 ExplosiveBullet에서 velocity가 설정되므로 여기서는 추가 설정 불필요
        // 하지만 Rigidbody가 없을 경우를 대비한 fallback
        if (bulletRigidbody != null && bulletRigidbody.velocity.magnitude < 0.1f)
        {
            bulletRigidbody.velocity = transform.forward * bulletSpeed;
        }
    }

    #endregion

    #region 초기화

    /// <summary>
    /// 분열 탄환 초기화
    /// </summary>
    public void Initialize(float damage, ElementType elementType, DamageType damageType)
    {
        bulletDamage = damage;
        bulletElementType = elementType;
        bulletDamageType = damageType;

        Debug.Log($"분열 탄환 초기화: 데미지={bulletDamage}, 속성={bulletElementType}, 타입={bulletDamageType}");
    }

    #endregion

    #region 충돌 처리

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return; // 중복 충돌 방지

        // 플레이어와 충돌
        if (other.CompareTag("Player"))
        {
            HitPlayer(other);
        }
        // 벽이나 장애물과 충돌
        else if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            HitObstacle();
        }
    }

    /// <summary>
    /// 플레이어와 충돌 시 처리
    /// </summary>
    private void HitPlayer(Collider playerCollider)
    {
        hasHit = true;

        // 플레이어 데미지 처리
        var player = playerCollider.GetComponent<Player.Player>();
        if (player != null)
        {
            // 속성별 데미지 계산 (필요시 확장)
            float finalDamage = CalculateFinalDamage();
            player.DecreaseHP(finalDamage);

            Debug.Log($"분열 탄환 명중! 데미지: {finalDamage} ({bulletElementType} {bulletDamageType})");
        }

        // 충돌 이펙트
        PlayHitEffects();

        // 탄환 제거
        DestroyBullet();
    }

    /// <summary>
    /// 장애물과 충돌 시 처리
    /// </summary>
    private void HitObstacle()
    {
        hasHit = true;

        Debug.Log("분열 탄환이 장애물에 충돌!");

        // 충돌 이펙트
        PlayHitEffects();

        // 탄환 제거
        DestroyBullet();
    }

    /// <summary>
    /// 최종 데미지 계산 (속성 효과 등 적용 가능)
    /// </summary>
    private float CalculateFinalDamage()
    {
        float finalDamage = bulletDamage;

        return finalDamage;
    }

    /// <summary>
    /// 충돌 이펙트 재생
    /// </summary>
    private void PlayHitEffects()
    {
        // 충돌 이펙트
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // 충돌 사운드
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }
    }

    /// <summary>
    /// 탄환 제거
    /// </summary>
    private void DestroyBullet()
    {
        // 즉시 제거하지 않고 약간의 딜레이 (이펙트 재생 시간 확보)
        Destroy(gameObject, 0.1f);
    }

    #endregion

    #region 기즈모 (디버그용)

    private void OnDrawGizmosSelected()
    {
        // 탄환 진행 방향 표시
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);

        // 탄환 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }

    #endregion

    #region 에디터용 디버그 정보

    [Header("실시간 정보 (읽기전용)")]
    [SerializeField] private float currentDamage;
    [SerializeField] private ElementType currentElementType;
    [SerializeField] private DamageType currentDamageType;
    [SerializeField] private bool isInitialized;

    private void Update()
    {
        // 에디터에서 실시간 정보 확인용
        if (Application.isEditor)
        {
            currentDamage = bulletDamage;
            currentElementType = bulletElementType;
            currentDamageType = bulletDamageType;
            isInitialized = bulletDamage > 0;
        }
    }

    #endregion
}