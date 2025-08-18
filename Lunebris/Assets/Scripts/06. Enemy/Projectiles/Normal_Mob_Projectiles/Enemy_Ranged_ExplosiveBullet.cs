using UnityEngine;
using Enemy;

/// <summary>
/// 분열형 폭발 탄환 - 일정 시간 후 여러 탄환으로 분열
/// </summary>
public class Enemy_Ranged_ExplosiveBullet : MonoBehaviour
{
    [Header("탄환 기본 설정")]
    public float bulletSpeed = 15f;              // 탄환 속도
    public float explodeTime = 1f;               // 몇 초 후에 터질지
    public float lifetime = 5f;                  // 탄환 수명 (최대 비행 시간)

    [Header("시각적 효과")]
    public GameObject explosionEffect;           // 분열 시 이펙트
    public AudioClip explosionSound;            // 분열 시 사운드

    // Initialize에서 설정되는 값들
    private float subBulletDamage;              // 각 분열 탄환 데미지
    private GameObject subBulletPrefab;         // 분열될 작은 탄환 프리팹
    private int subBulletCount;                 // 분열 탄환 개수
    private float spreadAngle;                  // 퍼짐 각도
    private ElementType bulletElementType;      // 탄환 속성
    private DamageType bulletDamageType;        // 데미지 타입

    // 내부 상태
    private float startTime;                     // 발사 시작 시간
    private bool hasExploded = false;           // 분열 완료 여부
    private bool isInitialized = false;         // 초기화 완료 여부
    private Rigidbody bulletRigidbody;          // 물리 컴포넌트

    #region Unity Lifecycle

    private void Awake()
    {
        bulletRigidbody = GetComponent<Rigidbody>();

        if (bulletRigidbody == null)
        {
            bulletRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        // Rigidbody 설정 (직선 이동용)
        bulletRigidbody.isKinematic = false;
        bulletRigidbody.useGravity = false;      // 중력 없음 (직선 비행)
        bulletRigidbody.drag = 0f;               // 공기저항 없음
        bulletRigidbody.angularDrag = 0f;        // 회전저항 없음

        startTime = Time.time;

        // 수명 제한 (무한 비행 방지)
        Destroy(gameObject, lifetime);
    }

    private void Start()
    {
        // 초기화가 안된 경우 기본값으로 설정
        if (!isInitialized)
        {
            explodeTime = 2f;
        }

        // 직진 이동 시작
        StartMoving();
    }

    private void Update()
    {
        // 시간 기반 폭발 체크
        float elapsedTime = Time.time - startTime;

        if (!hasExploded && elapsedTime >= explodeTime)
        {
            ExplodeIntoBullets();
        }
    }

    #endregion

    #region 초기화 및 이동

    /// <summary>
    /// Enemy_Ranged에서 호출하는 초기화 메서드
    /// </summary>
    public void Initialize(float damage, float explodeTime, GameObject subBullet, int bulletCount,
                          float angle, ElementType elementType, DamageType damageType)
    {
        subBulletDamage = damage;
        this.explodeTime = explodeTime;
        subBulletPrefab = subBullet;
        subBulletCount = bulletCount;
        spreadAngle = angle;
        bulletElementType = elementType;
        bulletDamageType = damageType;
        isInitialized = true;

        // explodeTime이 너무 작으면 기본값으로 설정
        if (this.explodeTime <= 0.1f)
        {
            this.explodeTime = 2f;
        }

        // 이미 Start가 호출된 경우 즉시 이동 시작
        if (bulletRigidbody != null)
        {
            StartMoving();
        }
    }

    /// <summary>
    /// 탄환 이동 시작 (직선 이동)
    /// </summary>
    private void StartMoving()
    {
        if (bulletRigidbody == null) return;

        // 이미 움직이고 있다면 중복 적용 방지
        if (bulletRigidbody.velocity.magnitude > 0.1f) return;

        // 직선 이동 적용
        Vector3 moveDirection = transform.forward;
        bulletRigidbody.velocity = moveDirection * bulletSpeed;
    }

    #endregion

    #region 분열 시스템

    /// <summary>
    /// 탄환을 여러 방향으로 분열시킴
    /// </summary>
    private void ExplodeIntoBullets()
    {
        if (hasExploded) return;

        hasExploded = true;

        // 분열 이펙트 재생
        PlayExplosionEffects();

        // 서브 탄환 프리팹이 있을 때만 분열
        if (subBulletPrefab != null && subBulletCount > 0)
        {
            // 현재 진행 방향을 기준으로 분열
            Vector3 forwardDirection = transform.forward;

            // 분열 탄환 생성
            for (int i = 0; i < subBulletCount; i++)
            {
                CreateSubBullet(i, forwardDirection);
            }
        }

        // 원본 탄환 제거
        Destroy(gameObject);
    }

    /// <summary>
    /// 개별 분열 탄환 생성
    /// </summary>
    private void CreateSubBullet(int index, Vector3 baseDirection)
    {
        // 분열 각도 계산 (부채꼴 형태)
        float angleStep = subBulletCount > 1 ? spreadAngle / (subBulletCount - 1) : 0f;
        float currentAngle = -spreadAngle / 2f + angleStep * index;

        // Y축 기준 회전 (수평 퍼짐)
        Vector3 spreadDirection = Quaternion.AngleAxis(currentAngle, Vector3.up) * baseDirection;

        // 분열 탄환 생성
        GameObject subBullet = Instantiate(subBulletPrefab, transform.position, Quaternion.LookRotation(spreadDirection));

        if (subBullet != null)
        {
            // 분열 탄환 컴포넌트 설정
            var subBulletComponent = subBullet.GetComponent<Enemy_Ranged_SubBullet>();
            if (subBulletComponent != null)
            {
                subBulletComponent.Initialize(subBulletDamage, bulletElementType, bulletDamageType);
            }

            // 물리 이동 적용
            var subRigidbody = subBullet.GetComponent<Rigidbody>();
            if (subRigidbody != null)
            {
                subRigidbody.velocity = spreadDirection * bulletSpeed * 0.8f; // 분열 후 약간 느려짐
            }
        }
    }

    /// <summary>
    /// 분열 시 이펙트 재생
    /// </summary>
    private void PlayExplosionEffects()
    {
        // 분열 이펙트
        if (explosionEffect != null)
        {
            GameObject effect = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // 분열 사운드
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }
    }

    #endregion

    #region 기즈모

    private void OnDrawGizmosSelected()
    {
        // 분열 탄환 궤도 미리보기
        if (subBulletCount > 0 && !hasExploded)
        {
            Vector3 currentPos = transform.position;

            // 현재 위치에서 분열 예상 지점 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(currentPos, 0.3f);

            // 분열 탄환들의 예상 궤도
            Gizmos.color = Color.yellow;
            for (int i = 0; i < subBulletCount; i++)
            {
                float angleStep = subBulletCount > 1 ? spreadAngle / (subBulletCount - 1) : 0f;
                float currentAngle = -spreadAngle / 2f + angleStep * i;
                Vector3 spreadDirection = Quaternion.AngleAxis(currentAngle, Vector3.up) * transform.forward;
                Gizmos.DrawRay(currentPos, spreadDirection * 2f);
            }
        }

        // 현재 이동 방향 표시
        if (bulletRigidbody != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, bulletRigidbody.velocity.normalized * 2f);
        }

        // 남은 시간 시각화
        if (Application.isPlaying)
        {
            float elapsedTime = Time.time - startTime;
            float remainingTime = explodeTime - elapsedTime;

            if (remainingTime > 0)
            {
                // 남은 시간에 따라 색깔 변화
                float timeRatio = remainingTime / explodeTime;
                Gizmos.color = Color.Lerp(Color.red, Color.green, timeRatio);
                Gizmos.DrawWireSphere(transform.position, 0.5f);
            }
        }
    }

    #endregion
}