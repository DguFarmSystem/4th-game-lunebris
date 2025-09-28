using UnityEngine;
using Enemy;

/// <summary>
/// 완성된 근접 탱커 - 애니메이션 연동 및 투사체 공격 포함
/// 높은 체력과 방어력을 가진 내구형 적
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Tanker : Enemy_Base
{
    [Header("기본 공격 설정")]
    [SerializeField] private float attackDuration = 0.5f;
    [SerializeField] private float meleeAttackCooldown = 2f; // 근접 공격 쿨다운

    [Header("투사체 공격 설정")]
    [SerializeField] private GameObject slowProjectilePrefab;
    [SerializeField] private GameObject slowAreaPrefab; // SlowArea 프리팹 추가
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float projectileAttackRange = 10f;
    [SerializeField] private float projectileAttackCooldown = 8f;
    [SerializeField] private float projectileDelay = 0.5f; // 애니메이션 후 실제 발사까지의 딜레이

    [Header("사운드 효과")]
    [SerializeField] private AudioSource audioSource; // 오디오 소스
    [SerializeField] private AudioClip meleeAttackSound; // 근접 공격 사운드
    [SerializeField] private AudioClip projectileAttackSound; // 투사체 공격 준비 사운드
    [SerializeField] private AudioClip hitSound; // 피격 사운드
    [SerializeField][Range(0f, 1f)] private float soundVolume = 0.8f; // 사운드 볼륨
    [SerializeField] private bool useRandomPitch = true; // 랜덤 피치 사용 여부
    [SerializeField][Range(0.7f, 1.3f)] private float minPitch = 0.8f; // 최소 피치 (탱커는 낮은 음성)
    [SerializeField][Range(0.7f, 1.3f)] private float maxPitch = 1.0f; // 최대 피치

    [Header("디버깅")]
    [SerializeField] private bool enableDebugLogs = true;

    // 상태 관리
    private bool isAttacking = false;
    private bool isProjectileAttacking = false;
    private float lastMeleeAttackTime;
    private float lastProjectileTime;

    // 컴포넌트 참조
    private Enemy_Tanker_Move moveScript;

    // 공격 관련 상태
    private bool waitingToFireProjectile = false;

    // Move 스크립트에서 참조할 수 있는 프로퍼티들
    public bool IsAttacking => isAttacking;
    public bool IsProjectileAttacking => isProjectileAttacking;

    #region Unity Lifecycle

    protected override void Awake()
    {
        // 탱커 기본 설정
        enemyType = EnemyType.MeleeTanker;
        elementType = ElementType.Neutral;
        primaryDamageType = DamageType.Physical;
        enemyName = "Dwarf Tanker";

        // AudioSource 자동 설정
        SetupAudioSource();

        base.Awake();

        // Move 스크립트 참조
        moveScript = GetComponent<Enemy_Tanker_Move>();
        if (moveScript == null)
        {
            Debug.LogWarning($"{name}: Enemy_Tanker_Move 컴포넌트를 찾을 수 없습니다!");
        }
    }

    protected override void InitializeEnemy()
    {
        // 공격 지속시간을 공격속도에 따라 조정
        if (enemyStats != null)
        {
            attackDuration = 1f / enemyStats.Get(EnemyStatType.AttackSpeed) * 0.5f;
        }

        // 투사체 발사 위치가 없으면 자동으로 생성
        if (projectileSpawnPoint == null)
        {
            GameObject spawnPoint = new GameObject("ProjectileSpawnPoint");
            spawnPoint.transform.SetParent(transform);
            spawnPoint.transform.localPosition = Vector3.up * 1.5f;
            projectileSpawnPoint = spawnPoint.transform;
            DebugLog("투사체 발사 위치 자동 생성됨");
        }

        base.InitializeEnemy();
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
                DebugLog("AudioSource 컴포넌트를 자동으로 추가했습니다.");
            }
        }

        // AudioSource 기본 설정 (탱커는 더 큰 소리)
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.volume = soundVolume;
            audioSource.spatialBlend = 1f; // 3D 사운드
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.maxDistance = 25f; // 탱커는 더 멀리 들림
            audioSource.minDistance = 3f;
        }
    }

    /// <summary>
    /// 근접 공격 사운드 재생
    /// </summary>
    private void PlayMeleeAttackSound()
    {
        if (meleeAttackSound != null)
        {
            PlaySound(meleeAttackSound);
            DebugLog("근접 공격 사운드 재생");
        }
    }

    /// <summary>
    /// 투사체 공격 준비 사운드 재생
    /// </summary>
    private void PlayProjectileAttackSound()
    {
        if (projectileAttackSound != null)
        {
            PlaySound(projectileAttackSound);
            DebugLog("투사체 공격 준비 사운드 재생");
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
            DebugLog("피격 사운드 재생");
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

        // 랜덤 피치 적용 (탱커는 낮은 목소리)
        if (useRandomPitch)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
        }
        else
        {
            audioSource.pitch = 0.9f; // 탱커 기본 피치 (약간 낮게)
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

    #region 행동 패턴

    protected override void UpdateBehavior()
    {
        // 공격 중인지 체크
        if (isAttacking)
        {
            if (Time.time - lastMeleeAttackTime >= attackDuration)
            {
                EndMeleeAttack();
            }
            return; // 공격 중이면 다른 행동 하지 않음
        }

        if (playerTransform == null) return;

        float distanceToPlayer = GetDistanceToPlayer();
        

        // 1. 근접 공격 범위 내면 근접 공격
        if (IsPlayerInAttackRange())
        {
            TryMeleeAttack();
        }
        // 2. 투사체 공격 범위 내면 투사체 공격 시도
        else if (distanceToPlayer <= projectileAttackRange && CanUseProjectileAttack())
        {
            TryProjectileAttack();
        }
        // 3. 감지 범위 내면 이동
        else if (IsPlayerInDetectionRange())
        {
            // 이동은 Enemy_Tanker_Move에서 처리
        }
    }

    protected override void UpdateMovement()
    {
        // Enemy_Tanker_Move 스크립트가 이동을 처리
        // 여기서는 공격 상태만 체크
        return;
    }

    protected override void PerformAttack()
    {
        if (playerScript == null) return;

        // 강력한 물리 공격
        DealDamageToPlayer(DamageType.Physical);
        DebugLog($"근접 공격 실행! 데미지 타입: {DamageType.Physical}");
    }

    #endregion

    #region 근접 공격 시스템

    /// <summary>
    /// 근접 공격 시도
    /// </summary>
    private void TryMeleeAttack()
    {
        if (Time.time - lastMeleeAttackTime >= meleeAttackCooldown)
        {
            StartMeleeAttack();
        }
    }

    private void StartMeleeAttack()
    {
        isAttacking = true;
        lastMeleeAttackTime = Time.time;

        // 근접 공격 애니메이션 실행
        if (moveScript != null)
        {
            moveScript.PlayAttackAnimation();
        }

        // 근접 공격 사운드 재생
        PlayMeleeAttackSound();

        DebugLog("근접 공격 시작!");

        // 애니메이션 약간 후에 실제 데미지 적용
        Invoke(nameof(ExecuteMeleeAttack), 0.3f);
    }

    private void ExecuteMeleeAttack()
    {
        if (IsPlayerInAttackRange()) // 여전히 범위 내에 있는지 확인
        {
            PerformAttack();
        }
    }

    private void EndMeleeAttack()
    {
        isAttacking = false;
        DebugLog("근접 공격 완료");
    }

    #endregion

    #region 투사체 공격 시스템

    /// <summary>
    /// 투사체 공격 시도
    /// </summary>
    private void TryProjectileAttack()
    {
        if (slowProjectilePrefab == null)
        {
            DebugLog("투사체 프리팹이 없어서 즉시 원거리 공격 실행", true);
            ExecuteInstantRangedAttack();
            return;
        }

        StartProjectileAttack();
    }

    private void StartProjectileAttack()
    {
        isProjectileAttacking = true;
        lastProjectileTime = Time.time;

        // 투사체 공격 애니메이션 실행
        if (moveScript != null)
        {
            moveScript.PlayProjectileAttackAnimation();
        }

        // 투사체 공격 준비 사운드 재생
        PlayProjectileAttackSound();

        DebugLog("투사체 공격 애니메이션 시작!");

        // 애니메이션 후에 실제 투사체 발사
        waitingToFireProjectile = true;
        Invoke(nameof(FireProjectile), projectileDelay);
        Invoke(nameof(EndProjectileAttack), 1.5f);
    }

    private void FireProjectile()
    {
        if (!waitingToFireProjectile) return;

        waitingToFireProjectile = false;

        // 플레이어 방향 계산
        Vector3 direction = (playerTransform.position - projectileSpawnPoint.position).normalized;
        direction.y = 0f; // 수평으로만 발사

        // 투사체 생성 및 발사
        GameObject projectile = Instantiate(slowProjectilePrefab, projectileSpawnPoint.position, Quaternion.LookRotation(direction));

        // 투사체 스크립트가 있다면 발사
        var projectileScript = projectile.GetComponent<Enemy_Tanker_SlowProjectile>();
        if (projectileScript != null)
        {
            projectileScript.Launch(direction);
        }
        else
        {
            // 기본 Rigidbody 발사
            var rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = direction * 5f; // 기본 속도
            }
        }

        // 모든 투사체에 폭발 처리 컴포넌트 추가
        var exploder = projectile.GetComponent<ProjectileExploder>();
        if (exploder == null)
        {
            exploder = projectile.AddComponent<ProjectileExploder>();
        }
        exploder.SetTanker(this);

        DebugLog("투사체 발사 완료!");
    }

    /// <summary>
    /// 투사체 프리팹이 없을 때 즉시 원거리 공격
    /// </summary>
    private void ExecuteInstantRangedAttack()
    {
        isProjectileAttacking = true;
        lastProjectileTime = Time.time;

        // 애니메이션 실행
        if (moveScript != null)
        {
            moveScript.PlayProjectileAttackAnimation();
        }

        // 레이캐스트로 즉시 공격
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        RaycastHit hit;

        if (Physics.Raycast(transform.position, direction, out hit, projectileAttackRange))
        {
            if (hit.collider.CompareTag("Player"))
            {
                Player.Player player = hit.collider.GetComponent<Player.Player>();
                if (player != null)
                {
                    float rangedDamage = enemyStats.Get(EnemyStatType.MagicalDamage); // 원거리는 마법 데미지
                    player.DecreaseHP(rangedDamage);
                    DebugLog($"즉시 원거리 공격 적중! {rangedDamage} 마법 데미지");
                }
            }
        }

        Invoke(nameof(EndProjectileAttack), 1f);
    }

    private void EndProjectileAttack()
    {
        isProjectileAttacking = false;
        waitingToFireProjectile = false;
        DebugLog("투사체 공격 완료");
    }

    /// <summary>
    /// 투사체 공격이 가능한지 확인
    /// </summary>
    private bool CanUseProjectileAttack()
    {
        return !isProjectileAttacking && (Time.time - lastProjectileTime >= projectileAttackCooldown);
    }

    /// <summary>
    /// 투사체 폭발시 SlowArea 생성
    /// </summary>
    public void CreateSlowAreaOnExplode(Vector3 position)
    {
        // SlowArea 생성
        CreateSlowAreaAt(position);

        DebugLog($"투사체 폭발! 위치: {position}에 SlowArea 생성");
    }

    /// <summary>
    /// 특정 위치에 SlowArea 생성
    /// </summary>
    private void CreateSlowAreaAt(Vector3 position)
    {
        if (slowAreaPrefab != null)
        {
            GameObject slowArea = Instantiate(slowAreaPrefab, position, Quaternion.identity);
            DebugLog($"SlowArea 생성됨! 위치: {position}");

            // SlowArea 초기화 (필요한 경우)
            var slowAreaScript = slowArea.GetComponent<Enemy_Tanker_SlowArea>();
            if (slowAreaScript != null)
            {
                // 필요하다면 여기서 Initialize 메서드 호출
                DebugLog("SlowArea 스크립트 참조 완료");
            }
        }
        else
        {
            DebugLog("SlowArea 프리팹이 설정되지 않음", true);
        }
    }

    #endregion

    #region 애니메이션 연동

    /// <summary>
    /// 피격시 처리 오버라이드
    /// </summary>
    protected override void OnDamaged()
    {
        // 피격 사운드 재생
        PlayHitSound();

        // Move 스크립트를 통한 피격 애니메이션
        if (moveScript != null)
        {
            moveScript.PlayHitAnimation();
        }

        // 부모 클래스의 기본 피격 처리 (색상 효과, 이펙트 등)
        base.OnDamaged();
    }

    /// <summary>
    /// 죽음 처리 오버라이드
    /// </summary>
    protected override void Die()
    {
        DebugLog($"Enemy_Tanker.Die() 호출됨");

        if (IsDead())
        {
            DebugLog("이미 죽은 상태");
            return;
        }

        // Move 스크립트를 통한 죽음 애니메이션
        if (moveScript != null)
        {
            moveScript.PlayDeathAnimation();
        }

        // 진행 중인 공격들 취소
        CancelInvoke();
        isAttacking = false;
        isProjectileAttacking = false;
        waitingToFireProjectile = false;

        // 부모 클래스의 죽음 처리
        base.Die();
    }

    #endregion

    #region 유틸리티 메서드

    /// <summary>
    /// 경험치 보상 설정
    /// </summary>
    protected override int GetExperienceReward()
    {
        return 25; // 탱커는 더 많은 경험치 (투사체 공격 추가로 인한 증가)
    }

    /// <summary>
    /// 디버그 로그 출력
    /// </summary>
    private void DebugLog(string message, bool forceLog = false)
    {
        if (enableDebugLogs || forceLog)
        {
            Debug.Log($"[{enemyName}] {message}");
        }
    }

    /// <summary>
    /// 충돌 처리 오버라이드
    /// </summary>
    protected override void OnTriggerEnter(Collider other)
    {
        DebugLog($"충돌 감지: {other.name} (태그: {other.tag})");

        // Enemy 태그인 경우 충돌을 무시 (통과)
        if (other.CompareTag("Enemy") && ignoreEnemyCollisions)
        {
            return;
        }

        if (other.CompareTag("Attack"))
        {
            // BaseAttack 컴포넌트 가져오기
            Player.BaseAttack baseAttack = other.GetComponent<Player.BaseAttack>();

            if (baseAttack != null)
            {
                // 직접 데미지 적용 (BaseAttack에서 가져온 실제 데미지)
                float directDamage = baseAttack.GetBaseDamage();
                TakeDamage(directDamage, DamageType.Physical, ElementType.Neutral);

                //  스플래쉬 효과가 있다면 실행 (직접 맞은 적 제외)
                if (baseAttack.HasSplashEffect())
                {
                    baseAttack.ExecuteSplashEffect(transform.position, this);
                }

                DebugLog($"플레이어 공격에 피격됨! 데미지: {directDamage}");
            }
            else
            {
                // 기존 로직 (BaseAttack이 없을 경우 대비)
                TakeDamage(10f, DamageType.Physical, ElementType.Neutral);
                DebugLog("플레이어 공격에 피격됨! (기본 데미지)");
            }

            // 이알 비활성화
            other.gameObject.SetActive(false);
        }
    }

    #endregion

    #region 디버그용 기즈모

    private void OnDrawGizmosSelected()
    {
        // 투사체 공격 범위 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, projectileAttackRange);

        // 근접 공격 범위 표시
        if (enemyStats != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, enemyStats.Get(EnemyStatType.AttackRange));
        }

        // 감지 범위 표시
        if (enemyStats != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, enemyStats.Get(EnemyStatType.DetectionRange));
        }

        // 투사체 발사 지점 표시
        if (projectileSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(projectileSpawnPoint.position, 0.2f);
        }
    }

    #endregion

    #region 공개 메서드 (외부 호출용)

    /// <summary>
    /// 강제로 근접 공격 실행 (디버그/테스트용)
    /// </summary>
    public void ForceAttack()
    {
        if (!isAttacking)
        {
            StartMeleeAttack();
        }
    }

    /// <summary>
    /// 강제로 투사체 공격 실행 (디버그/테스트용)
    /// </summary>
    public void ForceProjectileAttack()
    {
        if (!isProjectileAttacking)
        {
            TryProjectileAttack();
        }
    }

    /// <summary>
    /// 현재 상태 정보 반환 (디버그용)
    /// </summary>
    public string GetStatusInfo()
    {
        return $"공격중: {isAttacking}, 투사체공격중: {isProjectileAttacking}, " +
               $"근접쿨다운: {meleeAttackCooldown - (Time.time - lastMeleeAttackTime):F1}s, " +
               $"투사체쿨다운: {projectileAttackCooldown - (Time.time - lastProjectileTime):F1}s";
    }

    #endregion
}

/// <summary>
/// 투사체 폭발 처리를 위한 헬퍼 컴포넌트
/// </summary>
public class ProjectileExploder : MonoBehaviour
{
    private Enemy_Tanker tanker;
    private bool hasExploded = false;

    public void SetTanker(Enemy_Tanker tankerRef)
    {
        tanker = tankerRef;
        Debug.Log($"ProjectileExploder: 탱커 참조 설정됨 - {tankerRef.name}");
    }

    private void Start()
    {
        Debug.Log($"ProjectileExploder: 시작됨 - {gameObject.name}");

        // 5초 후 자동 폭발 (더 짧게 설정)
        Invoke(nameof(AutoExplode), 5f);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"ProjectileExploder: Trigger 충돌 감지 - {other.name} (태그: {other.tag})");

        if (hasExploded) return;

        /*
        // 플레이어나 벽에 충돌하면 폭발
        if (other.CompareTag("Player") || other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            Debug.Log($"ProjectileExploder: {other.tag}와 충돌하여 폭발!");
            Explode();
        }
        */
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"ProjectileExploder: Collision 충돌 감지 - {collision.gameObject.name}");

        if (hasExploded) return;

        // 어떤 것과든 충돌하면 폭발
        Debug.Log("ProjectileExploder: 충돌하여 폭발!");
        Explode();
    }

    private void AutoExplode()
    {
        if (hasExploded) return;

        Debug.Log("ProjectileExploder: 시간 초과로 자동 폭발!");
        Explode();
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        Debug.Log($"ProjectileExploder: 폭발 실행! 위치: {transform.position}");

        // 탱커에게 SlowArea 생성 요청
        if (tanker != null)
        {
            tanker.CreateSlowAreaOnExplode(transform.position);
            Debug.Log("ProjectileExploder: 탱커에게 SlowArea 생성 요청 완료");
        }
        else
        {
            Debug.LogWarning("ProjectileExploder: 탱커 참조가 없어서 SlowArea 생성 불가");
        }

        // 투사체 제거
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        Debug.Log($"ProjectileExploder: 오브젝트 제거됨 - {gameObject.name}");
    }
}