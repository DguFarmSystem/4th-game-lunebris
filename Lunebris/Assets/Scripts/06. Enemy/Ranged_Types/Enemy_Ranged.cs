using UnityEngine;
using Enemy;

/// <summary>
/// 기존 원거리 코드를 Enemy_Base에 통합 (AD형) - 분열형 폭발 탄환 스킬 추가
/// 일반공격과 폭발탄환 모두 시전시간 적용 - 사운드 시스템 포함
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Ranged : Enemy_Base
{
    [Header("원거리 공격 설정")]
    public GameObject bulletPrefab;      // 일반 총알 프리팹
    public Transform firePoint;          // 총알 발사 위치
    public float attackRange = 18f;       // 공격 사거리
    public float attackCooldown = 2f;    // 공격 쿨다운
    public float attackDuration = 0.5f;  // 공격 지속 시간 (멈춰있는 시간)
    public float attackCastTime = 0.5f;    // 일반 공격 시전 시간

    [Header("폭발 탄환 스킬 설정")]
    public GameObject explosiveBulletPrefab;  // 폭발 탄환 프리팹 (분열되는 메인 탄환)
    public GameObject subBulletPrefab;        // 분열된 작은 탄환 프리팹
    public float explosiveCooldown = 6f;      // 폭발 탄환 쿨다운
    public float explosiveRange = 12f;        // 폭발 탄환 사거리 (일반보다 길게)
    public float explosiveDamageMultiplier = 1.2f; // 각 분열 탄환 데미지 배율
    public float explodeTime = 1f;            // 탄환이 몇 초 후에 분열하는지
    public int subBulletCount = 5;           // 분열되는 탄환 개수
    public float spreadAngle = 45f;          // 분열 각도 (도)
    public int explosiveUseCondition = 4;     // 몇 번째 공격마다 폭발 탄환 사용
    public float explosiveCastTime = 0.8f;    // 폭발 탄환 시전 시간 (일반보다 길게)

    [Header("사운드 효과")]
    [SerializeField] private AudioSource audioSource; // 오디오 소스
    [SerializeField] private AudioClip normalCastSound; // 일반 공격 시전 사운드
    [SerializeField] private AudioClip normalFireSound; // 일반 총알 발사 사운드
    [SerializeField] private AudioClip explosiveCastSound; // 폭발 탄환 시전 사운드
    [SerializeField] private AudioClip explosiveFireSound; // 폭발 탄환 발사 사운드
    [SerializeField] private AudioClip hitSound; // 피격 사운드
    [SerializeField][Range(0f, 1f)] private float soundVolume = 0.8f; // 사운드 볼륨
    [SerializeField] private bool useRandomPitch = true; // 랜덤 피치 사용 여부
    [SerializeField][Range(0.8f, 1.2f)] private float minPitch = 0.9f; // 최소 피치
    [SerializeField][Range(0.8f, 1.2f)] private float maxPitch = 1.1f; // 최대 피치

    private float lastAttackTime;
    private float lastExplosiveTime;
    private bool isAttacking = false;         // 일반 공격 중인지 여부
    private bool isCastingNormal = false;     // 일반 공격 시전 중인지 여부
    private bool isCastingExplosive = false;  // 폭발 탄환 시전 중인지 여부
    private int attackCount = 0;              // 공격 횟수 카운터

    // 일반 공격 전용 애니메이션 파라미터 이름들
    private readonly string ANIM_CAST_NORMAL = "castNormal";
    private readonly string ANIM_IS_CASTING_NORMAL = "isCastingNormal";

    // 폭발 탄환 전용 애니메이션 파라미터 이름들
    private readonly string ANIM_CAST_EXPLOSIVE = "castExplosive";
    private readonly string ANIM_IS_CASTING_EXPLOSIVE = "isCastingExplosive";

    // Move 스크립트에서 참조할 수 있는 프로퍼티
    public bool IsAttacking => isAttacking || isCastingNormal || isCastingExplosive;
    public bool IsCastingAny => isCastingNormal || isCastingExplosive;

    #region Enemy_Base 오버라이드

    protected override void InitializeEnemy()
    {
        // 스탯 시스템 설정
        enemyType = EnemyType.RangedAD;
        primaryDamageType = DamageType.Physical; // AD 딜러이므로 물리 데미지

        // AudioSource 자동 설정
        SetupAudioSource();

        // 기본 초기화 로직
        if (firePoint == null)
        {
            Transform childFirePoint = transform.Find("FirePoint");
            if (childFirePoint != null)
            {
                firePoint = childFirePoint;
            }
        }

        // 스탯 시스템의 값으로 기본 설정 업데이트
        attackRange = enemyStats.Get(EnemyStatType.AttackRange);
        attackCooldown = 1f / enemyStats.Get(EnemyStatType.AttackSpeed);

        base.InitializeEnemy();
    }

    protected override void UpdateBehavior()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        HandleCombat(distanceToPlayer);

        // 시전 상태 애니메이션 업데이트
        UpdateCastingAnimations();
    }

    protected override void PerformAttack()
    {
        attackCount++;

        // 폭발 탄환 스킬 사용 조건 체크
        if (ShouldUseExplosiveBullet())
        {
            CastExplosiveBullet();
        }
        else
        {
            // 일반 총알 공격 (시전시간 포함)
            CastNormalBullet();
        }

        lastAttackTime = Time.time;
    }

    protected override void UpdateMovement()
    {
        // 이동은 Enemy_Ranged_Move에서 처리하므로 비워둠
        // Enemy_Ranged_Move가 IsAttacking 프로퍼티를 참조해서 움직임 제어
    }

    protected override void OnDamaged()
    {
        // 피격 사운드 재생
        PlayHitSound();

        // 부모 클래스의 기본 피격 처리
        base.OnDamaged();
    }

    #endregion

    #region 사운드 시스템

    /// <summary>
    /// AudioSource 자동 설정
    /// </summary>
    private void SetupAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log($"{enemyName}: AudioSource 컴포넌트를 자동으로 추가했습니다.");
            }
        }

        // AudioSource 기본 설정 (원거리는 중간 소리)
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.volume = soundVolume;
            audioSource.spatialBlend = 1f; // 3D 사운드
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.maxDistance = 22f; // 원거리는 중간 거리
            audioSource.minDistance = 2f;
        }
    }

    /// <summary>
    /// 일반 공격 시전 사운드 재생
    /// </summary>
    private void PlayNormalCastSound()
    {
        if (normalCastSound != null)
        {
            PlaySound(normalCastSound);
            Debug.Log($"{enemyName}: 일반 공격 시전 사운드 재생");
        }
    }

    /// <summary>
    /// 일반 총알 발사 사운드 재생
    /// </summary>
    private void PlayNormalFireSound()
    {
        if (normalFireSound != null)
        {
            PlaySound(normalFireSound);
            Debug.Log($"{enemyName}: 일반 총알 발사 사운드 재생");
        }
    }

    /// <summary>
    /// 폭발 탄환 시전 사운드 재생
    /// </summary>
    private void PlayExplosiveCastSound()
    {
        if (explosiveCastSound != null)
        {
            PlaySound(explosiveCastSound);
            Debug.Log($"{enemyName}: 폭발 탄환 시전 사운드 재생");
        }
    }

    /// <summary>
    /// 폭발 탄환 발사 사운드 재생
    /// </summary>
    private void PlayExplosiveFireSound()
    {
        if (explosiveFireSound != null)
        {
            PlaySound(explosiveFireSound);
            Debug.Log($"{enemyName}: 폭발 탄환 발사 사운드 재생");
        }
    }

    /// <summary>
    /// 피격 사운드 재생
    /// </summary>
    private void PlayHitSound()
    {
        if (hitSound != null)
        {
            PlaySound(hitSound);
            Debug.Log($"{enemyName}: 피격 사운드 재생");
        }
    }

    /// <summary>
    /// 사운드 재생 (공통 메서드)
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;

        // 볼륨 설정
        audioSource.volume = soundVolume;

        // 랜덤 피치 적용
        if (useRandomPitch)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
        }
        else
        {
            audioSource.pitch = 1f;
        }

        // 사운드 재생
        audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// 사운드 즉시 정지
    /// </summary>
    private void StopSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    #endregion

    #region 시전 애니메이션 관리

    /// <summary>
    /// 일반 공격 시전 애니메이션 재생
    /// </summary>
    private void PlayCastNormalAnimation()
    {
        if (characterAnimator == null) return;

        characterAnimator.SetTrigger(ANIM_CAST_NORMAL);
        Debug.Log($"{enemyName} 일반 공격 시전 애니메이션 재생");
    }

    /// <summary>
    /// 폭발 탄환 시전 애니메이션 재생
    /// </summary>
    private void PlayCastExplosiveAnimation()
    {
        if (characterAnimator == null) return;

        characterAnimator.SetTrigger(ANIM_CAST_EXPLOSIVE);
        Debug.Log($"{enemyName} 폭발 탄환 시전 애니메이션 재생");
    }

    /// <summary>
    /// 모든 시전 상태 애니메이션 파라미터 업데이트
    /// </summary>
    private void UpdateCastingAnimations()
    {
        if (characterAnimator == null) return;

        // 일반 공격 시전 상태 Bool 파라미터 업데이트
        characterAnimator.SetBool(ANIM_IS_CASTING_NORMAL, isCastingNormal);

        // 폭발 탄환 시전 상태 Bool 파라미터 업데이트
        characterAnimator.SetBool(ANIM_IS_CASTING_EXPLOSIVE, isCastingExplosive);
    }

    #endregion

    #region 공격 시스템

    private void HandleCombat(float distanceToPlayer)
    {
        LookAtPlayer();

        // 폭발 탄환의 경우 더 긴 사거리 체크
        float currentRange = ShouldUseExplosiveBullet() ? explosiveRange : attackRange;

        if (distanceToPlayer <= currentRange && Time.time > lastAttackTime + attackCooldown && !IsAttacking)
        {
            PerformAttack();
        }
    }

    private bool ShouldUseExplosiveBullet()
    {
        return attackCount % explosiveUseCondition == 0 &&
               Time.time > lastExplosiveTime + explosiveCooldown &&
               playerTransform != null;
    }

    #region 일반 공격 시스템

    private void CastNormalBullet()
    {
        if (bulletPrefab != null && firePoint != null && playerTransform != null)
        {
            // 일반 공격 시전 시작
            isCastingNormal = true;

            // 일반 공격 시전 애니메이션 재생
            PlayCastNormalAnimation();

            // 일반 공격 시전 사운드 재생
            PlayNormalCastSound();

            Debug.Log($"{enemyName} 일반 공격 시전 시작! 시전시간: {attackCastTime}초");

            // 시전 시간 후 발사 애니메이션 재생
            Invoke(nameof(StartNormalAttackAnimation), attackCastTime);
            // 발사 애니메이션 후 실제 탄환 발사 (애니메이션 재생 시간 고려)
            Invoke(nameof(FireNormalBullet), attackCastTime + 0.2f); // 0.2초는 발사 애니메이션 시간
            Invoke(nameof(EndNormalCasting), attackCastTime);
        }
    }

    /// <summary>
    /// 일반 공격 발사 애니메이션 시작
    /// </summary>
    private void StartNormalAttackAnimation()
    {
        // 실제 공격 상태로 전환
        isAttacking = true;

        // 실제 발사 애니메이션 재생
        PlayAttackAnimation();

        Debug.Log($"{enemyName} 일반 공격 발사 애니메이션 재생!");
    }

    private void FireNormalBullet()
    {
        if (bulletPrefab != null && firePoint != null && playerTransform != null)
        {
            // 일반 총알 발사 사운드 재생
            PlayNormalFireSound();

            // 총알 복제 생성 (애니메이션 이후에)
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

            // 플레이어 방향으로 총알 회전
            Vector3 targetPos = playerTransform.position;
            targetPos.y = firePoint.position.y;
            bullet.transform.LookAt(targetPos);

            // 총알 컴포넌트에 스탯 적용
            // TODO: Enemy_Ranged_Bullet 컴포넌트 구현 필요
            // var bulletComponent = bullet.GetComponent<Enemy_Ranged_Bullet>();

            // 일정 시간 후 공격 상태 해제
            Invoke(nameof(EndNormalAttack), attackDuration);

            Debug.Log($"{enemyName} 일반 총알 발사! 물리 데미지: {GetMainDamage()}");
        }
    }

    private void EndNormalCasting()
    {
        isCastingNormal = false;
        Debug.Log($"{enemyName} 일반 공격 시전 완료");
    }

    private void EndNormalAttack()
    {
        isAttacking = false;
        Debug.Log($"{enemyName} 일반 공격 완료");
    }

    #endregion

    #region 폭발 탄환 시스템

    private void CastExplosiveBullet()
    {
        if (explosiveBulletPrefab != null && firePoint != null && playerTransform != null)
        {
            // 폭발 탄환 시전 시작
            isCastingExplosive = true;
            lastExplosiveTime = Time.time;

            // 폭발 탄환 시전 애니메이션 재생
            PlayCastExplosiveAnimation();

            // 폭발 탄환 시전 사운드 재생
            PlayExplosiveCastSound();

            Debug.Log($"{enemyName} 폭발 탄환 시전 시작! 시전시간: {explosiveCastTime}초");

            // 시전 시간 후 발사 애니메이션 재생
            Invoke(nameof(StartExplosiveAttackAnimation), explosiveCastTime);
            // 발사 애니메이션 후 실제 탄환 발사 (애니메이션 재생 시간 고려)
            Invoke(nameof(FireExplosiveBullet), explosiveCastTime + 0.2f); // 0.2초는 발사 애니메이션 시간
            Invoke(nameof(EndExplosiveCasting), explosiveCastTime);
        }
    }

    /// <summary>
    /// 폭발 탄환 발사 애니메이션 시작
    /// </summary>
    private void StartExplosiveAttackAnimation()
    {
        // 실제 발사 애니메이션 재생
        PlayAttackAnimation();

        Debug.Log($"{enemyName} 폭발 탄환 발사 애니메이션 재생!");
    }

    private void FireExplosiveBullet()
    {
        if (explosiveBulletPrefab != null && firePoint != null && playerTransform != null)
        {
            // 폭발 탄환 발사 사운드 재생
            PlayExplosiveFireSound();

            // 폭발 탄환 생성 (애니메이션 이후에)
            GameObject explosiveBullet = Instantiate(explosiveBulletPrefab, firePoint.position, firePoint.rotation);

            // 플레이어 방향으로 폭발 탄환 회전
            Vector3 targetPos = playerTransform.position;
            targetPos.y = firePoint.position.y;
            explosiveBullet.transform.LookAt(targetPos);

            // 폭발 탄환 컴포넌트 설정 (분열 시스템)
            var explosiveComponent = explosiveBullet.GetComponent<Enemy_Ranged_ExplosiveBullet>();
            if (explosiveComponent != null)
            {
                explosiveComponent.Initialize(
                    GetMainDamage() * explosiveDamageMultiplier, // 각 분열 탄환 데미지
                    explodeTime, // 폭발 시간 (몇 초 후에 터질지)
                    subBulletPrefab, // 분열될 작은 탄환 프리팹
                    subBulletCount, // 분열 탄환 개수
                    spreadAngle, // 퍼짐 각도
                    this.elementType, // 원거리 적의 속성
                    primaryDamageType // 물리 데미지
                );

                Debug.Log($"{enemyName} 폭발 탄환 초기화 완료! 폭발시간: {explodeTime}초, 분열개수: {subBulletCount}개");
            }
            else
            {
                Debug.LogError($"{enemyName} 폭발 탄환에 Enemy_Ranged_ExplosiveBullet 컴포넌트가 없습니다!");
            }

            Debug.Log($"{enemyName} 폭발 탄환 발사! 폭발시간: {explodeTime}초, 분열 개수: {subBulletCount}개, 각 탄환 데미지: {GetMainDamage() * explosiveDamageMultiplier}");
        }
    }

    private void EndExplosiveCasting()
    {
        isCastingExplosive = false;
        Debug.Log($"{enemyName} 폭발 탄환 시전 완료");
    }

    #endregion

    private void LookAtPlayer()
    {
        if (playerTransform == null) return;

        Vector3 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    #endregion

    #region 기즈모

    private void OnDrawGizmosSelected()
    {
        // 일반 공격 범위
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 폭발 탄환 공격 범위
        Gizmos.color = new Color(1f, 0.5f, 0f); // 주황색
        Gizmos.DrawWireSphere(transform.position, explosiveRange);

        // 스탯 시스템: 감지 범위 표시
        if (enemyStats != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, enemyStats.Get(EnemyStatType.DetectionRange));
        }

        // 폭발 탄환 예상 궤도 (플레이어 방향으로)
        if (playerTransform != null)
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            Vector3 endPoint = transform.position + direction * (explodeTime * 15f); // 예상 비행거리

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, endPoint);
            Gizmos.DrawWireSphere(endPoint, 0.5f); // 분열 예상 지점

            // 분열 탄환들의 예상 궤도
            Gizmos.color = Color.cyan;
            for (int i = 0; i < subBulletCount; i++)
            {
                float angle = -spreadAngle / 2f + (spreadAngle / (subBulletCount - 1)) * i;
                Vector3 spreadDirection = Quaternion.AngleAxis(angle, Vector3.up) * direction;
                Gizmos.DrawRay(endPoint, spreadDirection * 3f);
            }
        }
    }

    #endregion

    #region 실시간 스탯 확인

    /// <summary>
    /// 인스펙터에서 실시간으로 스탯 확인 가능
    /// </summary>
    [System.Serializable]
    public class RuntimeStats
    {
        [SerializeField] private float currentHP;
        [SerializeField] private float physicalDamage;
        [SerializeField] private float attackSpeed;
        [SerializeField] private float moveSpeed;
        [SerializeField] private int attackCount;
        [SerializeField] private bool isAttacking;
        [SerializeField] private bool isCastingNormal;
        [SerializeField] private bool isCastingExplosive;
        [SerializeField] private float normalCastTime;
        [SerializeField] private float explosiveCastTime;
        [SerializeField] private float explodeTime;
        [SerializeField] private int subBulletCount;

        public void UpdateStats(Enemy_Ranged enemy)
        {
            if (enemy.enemyStats != null)
            {
                currentHP = enemy.currentHp;
                physicalDamage = enemy.enemyStats.Get(EnemyStatType.PhysicalDamage);
                attackSpeed = enemy.enemyStats.Get(EnemyStatType.AttackSpeed);
                moveSpeed = enemy.enemyStats.Get(EnemyStatType.MoveSpeed);
                attackCount = enemy.attackCount;
                isAttacking = enemy.isAttacking;
                isCastingNormal = enemy.isCastingNormal;
                isCastingExplosive = enemy.isCastingExplosive;
                normalCastTime = enemy.attackCastTime;
                explosiveCastTime = enemy.explosiveCastTime;
                explodeTime = enemy.explodeTime;
                subBulletCount = enemy.subBulletCount;
            }
        }
    }

    [Header("실시간 스탯 확인 (읽기전용)")]
    [SerializeField] private RuntimeStats runtimeStats = new RuntimeStats();

    protected override void Update()
    {
        base.Update();

        // 디버그용: 실시간 스탯 업데이트
        if (Application.isEditor)
        {
            runtimeStats.UpdateStats(this);
        }
    }

    #endregion
}