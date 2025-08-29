using UnityEngine;
using System.Collections;
using Enemy;

/// <summary>
/// 대쉬 스킬을 가진 근접 암살자
/// 빠른 속도로 플레이어에게 접근해서 기습 공격
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Assassin : Enemy_Base
{
    [Header("기본 공격 설정")]
    [SerializeField] private float attackDuration = 0.3f; // 공격 지속시간

    [Header("대쉬 스킬 설정")]
    [SerializeField] private float dashDistance = 12f; // 대쉬 거리 (8 → 12로 증가)
    [SerializeField] private float dashSpeed = 25f; // 대쉬 속도 (20 → 25로 증가)
    [SerializeField] private float dashCooldown = 4f; // 대쉬 쿨다운
    [SerializeField] private float dashRange = 15f; // 대쉬 사용 가능 범위 (12 → 15로 증가)
    [SerializeField] private float dashStopDistance = 2f; // 플레이어로부터 이 거리에서 대쉬 중지
    [SerializeField] private float dashPrepareDelay = 0.5f; // 대쉬 전 준비 딜레이
    [SerializeField] private float postDashAttackDelay = 0.2f; // 대쉬 후 공격 딜레이

    [Header("시각적 효과")]
    [SerializeField] private GameObject dashEffect; // 대쉬 이펙트
    [SerializeField] private TrailRenderer dashTrail; // 대쉬 궤적

    [Header("사운드 효과")]
    [SerializeField] private AudioSource audioSource; // 오디오 소스
    [SerializeField] private AudioClip attackSound; // 공격 사운드
    [SerializeField] private AudioClip dashPrepareSound; // 대쉬 준비 사운드
    [SerializeField] private AudioClip dashExecuteSound; // 대쉬 실행 사운드
    [SerializeField] private AudioClip dashEndSound; // 대쉬 종료 사운드
    [SerializeField][Range(0f, 1f)] private float soundVolume = 0.8f; // 사운드 볼륨
    [SerializeField] private bool useRandomPitch = true; // 랜덤 피치 사용 여부
    [SerializeField][Range(0.8f, 1.2f)] private float minPitch = 0.9f; // 최소 피치
    [SerializeField][Range(0.8f, 1.2f)] private float maxPitch = 1.1f; // 최대 피치

    // 상태 관리
    private bool isAttacking = false;
    private bool isDashing = false;
    private bool isPreparingDash = false; // 대쉬 준비 중인지
    private bool canDash = true;
    private float lastAttackTime;
    private float lastDashTime;

    // 대쉬 관련
    private Vector3 dashDirection;
    private Vector3 dashStartPosition;
    private float dashTimer;
    private Rigidbody rb;

    // 대쉬 애니메이션 파라미터 이름들
    private readonly string ANIM_DASH_PREPARE = "prepareDash"; // 대쉬 준비 트리거
    private readonly string ANIM_IS_PREPARING_DASH = "isPreparingDash"; // 대쉬 준비 중인지
    private readonly string ANIM_DASH_TRIGGER = "startDash";
    private readonly string ANIM_IS_DASHING = "isDashing";
    private readonly string ANIM_DASH_END = "endDash";

    // Move 스크립트에서 참조할 수 있는 프로퍼티들
    public bool IsAttacking => isAttacking;
    public bool IsDashing => isDashing;
    public bool IsPreparingDash => isPreparingDash;

    protected override void Awake()
    {
        // 암살자 기본 설정
        enemyType = EnemyType.MeleeAssassin;
        elementType = ElementType.Tenebris;
        primaryDamageType = DamageType.Physical;
        enemyName = "Dash Assassin";

        rb = GetComponent<Rigidbody>();

        // AudioSource 자동 설정
        SetupAudioSource();

        base.Awake();
    }

    protected override void InitializeEnemy()
    {
        // 공격 지속시간을 공격속도에 따라 조정
        attackDuration = 1f / enemyStats.Get(EnemyStatType.AttackSpeed) * 0.3f;

        // Trail Renderer 설정
        if (dashTrail != null)
        {
            dashTrail.enabled = false;
        }

        base.InitializeEnemy();
    }

    protected override void UpdateBehavior()
    {
        // 대쉬 애니메이션 상태 업데이트
        UpdateDashAnimation();

        // 대쉬 중일 때는 다른 행동 하지 않음
        if (isDashing)
        {
            UpdateDash();
            return;
        }

        if (isPreparingDash)
        {
            // 준비 중에는 계속 멈춤 상태 유지
            StopMovement();
            return; // 대쉬 준비 중이면 다른 행동 하지 않음
        }

        // 공격 중인지 체크
        if (isAttacking)
        {
            if (Time.time - lastAttackTime >= attackDuration)
            {
                isAttacking = false;
                Debug.Log($"{enemyName}: 공격 완료");
            }
            return; // 공격 중이면 다른 행동 하지 않음
        }

        if (playerTransform == null) return;

        float distanceToPlayer = GetDistanceToPlayer();

        // 1. 대쉬 범위 내면 대쉬 시도
        if (distanceToPlayer <= dashRange && CanUseDash())
        {
            StartDashPrepare();
        }
        // 2. 공격 범위 내면 공격
        else if (IsPlayerInAttackRange())
        {
            TryAttack();
        }
    }

    protected override void UpdateMovement()
    {
        // 대쉬 준비 중이거나 대쉬 중일 때는 움직임 멈춤
        if (isPreparingDash || isDashing)
        {
            StopMovement();
            return;
        }

        // 이동은 Enemy_Assassin_Move에서 처리하거나 대쉬로 대체
    }

    protected override void PerformAttack()
    {
        if (playerScript == null) return;

        // 공격 사운드 재생
        PlayAttackSound();

        // 빠르고 강력한 물리 공격 (암살자 특성)
        DealDamageToPlayer(DamageType.Physical);
        Debug.Log($"{enemyName}: 기습 공격!");
    }

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

        // AudioSource 기본 설정
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.volume = soundVolume;
            audioSource.spatialBlend = 1f; // 3D 사운드
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.maxDistance = 20f;
        }
    }

    /// <summary>
    /// 공격 사운드 재생
    /// </summary>
    private void PlayAttackSound()
    {
        if (attackSound != null)
        {
            PlaySound(attackSound);
            Debug.Log($"{enemyName}: 공격 사운드 재생");
        }
    }

    /// <summary>
    /// 대쉬 준비 사운드 재생
    /// </summary>
    private void PlayDashPrepareSound()
    {
        if (dashPrepareSound != null)
        {
            PlaySound(dashPrepareSound);
            Debug.Log($"{enemyName}: 대쉬 준비 사운드 재생");
        }
    }

    /// <summary>
    /// 대쉬 실행 사운드 재생
    /// </summary>
    private void PlayDashExecuteSound()
    {
        if (dashExecuteSound != null)
        {
            PlaySound(dashExecuteSound);
            Debug.Log($"{enemyName}: 대쉬 실행 사운드 재생");
        }
    }

    /// <summary>
    /// 대쉬 종료 사운드 재생
    /// </summary>
    private void PlayDashEndSound()
    {
        if (dashEndSound != null)
        {
            PlaySound(dashEndSound);
            Debug.Log($"{enemyName}: 대쉬 종료 사운드 재생");
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

    #region 대쉬 애니메이션

    /// <summary>
    /// 대쉬 준비 애니메이션 재생
    /// </summary>
    private void PlayDashPrepareAnimation()
    {
        if (characterAnimator == null) return;

        characterAnimator.SetTrigger(ANIM_DASH_PREPARE);
        characterAnimator.SetBool(ANIM_IS_PREPARING_DASH, true);
        Debug.Log($"{enemyName}: 대쉬 준비 애니메이션 재생");
    }

    /// <summary>
    /// 대쉬 시작 애니메이션 재생
    /// </summary>
    private void PlayDashStartAnimation()
    {
        if (characterAnimator == null) return;

        characterAnimator.SetBool(ANIM_IS_PREPARING_DASH, false);
        characterAnimator.SetTrigger(ANIM_DASH_TRIGGER);
        characterAnimator.SetBool(ANIM_IS_DASHING, true);
        Debug.Log($"{enemyName}: 대쉬 시작 애니메이션 재생");
    }

    /// <summary>
    /// 대쉬 종료 애니메이션 재생
    /// </summary>
    private void PlayDashEndAnimation()
    {
        if (characterAnimator == null) return;

        characterAnimator.SetTrigger(ANIM_DASH_END);
        characterAnimator.SetBool(ANIM_IS_DASHING, false);
        Debug.Log($"{enemyName}: 대쉬 종료 애니메이션 재생");
    }

    /// <summary>
    /// 대쉬 상태 애니메이션 업데이트
    /// </summary>
    private void UpdateDashAnimation()
    {
        if (characterAnimator == null) return;

        characterAnimator.SetBool(ANIM_IS_PREPARING_DASH, isPreparingDash);
        characterAnimator.SetBool(ANIM_IS_DASHING, isDashing);
    }

    #endregion

    #region 대쉬 시스템

    /// <summary>
    /// 일반 공격 시도
    /// </summary>
    private void TryAttack()
    {
        float attackSpeed = enemyStats.Get(EnemyStatType.AttackSpeed);
        float attackCooldown = 1f / attackSpeed;

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            isAttacking = true;
            PerformAttack();
            lastAttackTime = Time.time;

            // 일정 시간 후 공격 상태 해제
            Invoke(nameof(EndAttack), attackDuration);
        }
    }

    /// <summary>
    /// 대쉬 준비 시작
    /// </summary>
    private void StartDashPrepare()
    {
        if (!CanUseDash()) return;

        isPreparingDash = true;
        canDash = false;

        // 준비 중에는 움직임 멈춤
        StopMovement();

        // 플레이어 방향 미리 계산
        Vector3 dir = (playerTransform.position - transform.position);
        dir.y = 0f;

        // 방향이 너무 작으면(거의 같은 위치) 대쉬 취소
        if (dir.magnitude < 0.1f)
        {
            Debug.Log($"{enemyName}: 플레이어가 너무 가까워 대쉬 취소");
            isPreparingDash = false;
            canDash = true;
            return;
        }

        dashDirection = dir.normalized;

        // 플레이어 방향으로 회전 (준비 동작)
        transform.LookAt(new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z));

        // 대쉬 준비 애니메이션 재생
        PlayDashPrepareAnimation();

        // 대쉬 준비 사운드 재생
        PlayDashPrepareSound();

        Debug.Log($"{enemyName}: 대쉬 준비 시작! 방향 {dashDirection}");

        // 준비 딜레이 후 실제 대쉬 실행
        DelayAction(dashPrepareDelay, () => {
            if (isPreparingDash) // 준비 중인 상태에서만 대쉬 실행
            {
                ExecuteDash();
            }
        });
    }

    /// <summary>
    /// 실제 대쉬 실행
    /// </summary>
    private void ExecuteDash()
    {
        if (!isPreparingDash) return;

        isPreparingDash = false;
        isDashing = true;
        dashTimer = 0f;

        dashStartPosition = transform.position;
        lastDashTime = Time.time;

        // 시각적 효과
        StartDashEffects();

        // 대쉬 시작 애니메이션 재생
        PlayDashStartAnimation();

        // 대쉬 실행 사운드 재생
        PlayDashExecuteSound();

        Debug.Log($"{enemyName}: 대쉬 실행! 방향 {dashDirection}");

        // 대쉬 쿨다운 시작
        Invoke(nameof(ResetDashCooldown), dashCooldown);
    }



    /// <summary>
    /// 대쉬 업데이트 (매 프레임)
    /// </summary>
    private void UpdateDash()
    {
        dashTimer += Time.deltaTime;

        // 플레이어와의 거리 체크 - 너무 가까워지면 대쉬 중지
        float distanceToPlayer = GetDistanceToPlayer();
        if (distanceToPlayer <= dashStopDistance)
        {
            Debug.Log($"{enemyName}: 플레이어 근처에 도착하여 대쉬 중지 (거리: {distanceToPlayer:F1})");
            EndDash();
            return;
        }

        // 대쉬 거리 계산
        float dashDuration = dashDistance / dashSpeed;

        if (dashTimer >= dashDuration)
        {
            // 대쉬 완료
            EndDash();
            return;
        }

        // 대쉬 이동
        Vector3 dashVelocity = dashDirection * dashSpeed;

        bool moved = false;

        if (rb != null && !rb.isKinematic && !rb.constraints.HasFlag(RigidbodyConstraints.FreezePositionX) && !rb.constraints.HasFlag(RigidbodyConstraints.FreezePositionZ))
        {
            rb.velocity = new Vector3(dashVelocity.x, rb.velocity.y, dashVelocity.z);
            moved = true;
        }

        // Rigidbody로 안 움직였다면 Transform 직접 이동
        if (!moved)
        {
            transform.position += dashVelocity * Time.deltaTime;
        }

        // 장애물 충돌 체크 (옵션) - 디버그용으로 비활성화
        // CheckDashCollision();
    }

    /// <summary>
    /// 대쉬 중 충돌 체크
    /// </summary>
    private void CheckDashCollision()
    {
        // 앞쪽에 장애물이 있으면 대쉬 중단
        float checkDistance = 1f;
        RaycastHit hit;

        if (Physics.Raycast(transform.position, dashDirection, out hit, checkDistance))
        {
            if (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("Obstacle"))
            {
                EndDash();
            }
        }
    }

    /// <summary>
    /// 대쉬 종료
    /// </summary>
    private void EndDash()
    {
        isDashing = false;

        // 속도 초기화
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
        }

        // 시각적 효과 종료
        EndDashEffects();

        // 대쉬 종료 애니메이션 재생
        PlayDashEndAnimation();

        // 대쉬 종료 사운드 재생
        PlayDashEndSound();

        Debug.Log($"{enemyName}: 대쉬 완료!");

        // 대쉬 후 잠깐 딜레이 후 공격 가능
        DelayAction(postDashAttackDelay, () => {
            if (IsPlayerInAttackRange())
            {
                TryAttack();
            }
        });
    }

    /// <summary>
    /// 대쉬 후 딜레이 (기존 코루틴 대신 DelayAction 사용)
    /// </summary>
    private IEnumerator PostDashDelay()
    {
        yield return new WaitForSeconds(postDashAttackDelay);

        // 대쉬 후 플레이어가 공격 범위에 있으면 즉시 공격
        if (IsPlayerInAttackRange())
        {
            TryAttack();
        }
    }

    /// <summary>
    /// 대쉬 사용 가능 여부
    /// </summary>
    private bool CanUseDash()
    {
        return canDash && !isDashing && !isAttacking && !isPreparingDash;
    }

    /// <summary>
    /// 대쉬 쿨다운 리셋
    /// </summary>
    private void ResetDashCooldown()
    {
        canDash = true;
        Debug.Log($"{enemyName}: 대쉬 쿨다운 완료!");
    }

    /// <summary>
    /// 움직임 완전히 멈춤
    /// </summary>
    private void StopMovement()
    {
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// 딜레이 액션 (간단한 딜레이 유틸리티)
    /// </summary>
    private void DelayAction(float delay, System.Action action)
    {
        StartCoroutine(DelayCoroutine(delay, action));
    }

    private IEnumerator DelayCoroutine(float delay, System.Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }

    #endregion

    #region 시각적 효과

    /// <summary>
    /// 대쉬 시각적 효과 시작
    /// </summary>
    private void StartDashEffects()
    {
        // 대쉬 이펙트 생성
        if (dashEffect != null)
        {
            GameObject effect = Instantiate(dashEffect, transform.position, Quaternion.LookRotation(dashDirection));
            effect.transform.SetParent(transform);
            Destroy(effect, 1f);
        }

        // Trail Renderer 활성화
        if (dashTrail != null)
        {
            dashTrail.enabled = true;
            dashTrail.Clear();
        }
    }

    /// <summary>
    /// 대쉬 시각적 효과 종료
    /// </summary>
    private void EndDashEffects()
    {
        // Trail Renderer 비활성화
        if (dashTrail != null)
        {
            dashTrail.enabled = false;
        }
    }

    #endregion

    private void EndAttack()
    {
        isAttacking = false;
    }

    protected override int GetExperienceReward()
    {
        return 22; // 대쉬 스킬로 인한 추가 경험치
    }

}