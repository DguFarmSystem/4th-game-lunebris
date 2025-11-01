using UnityEngine;
using Enemy;
using System.Collections;

/// <summary>
/// 최종보스 - 빛 모드 (레일건, 검기, 빛 기둥)
/// Idle 상태일 때는 모든 동작 중지
/// </summary>
public class Enemy_Final_Boss_Light : Enemy_Base
{
    [Header("레일건 빔")]
    [SerializeField] private GameObject railgunBeamPrefab;
    [SerializeField] private float railgunDamage = 120f;
    [SerializeField] private float railgunChargeTime = 2f;
    [SerializeField] private float railgunCooldown = 4f;

    [Header("360빔 공격")]
    [SerializeField] private GameObject beam360Prefab;
    [SerializeField] private float beam360Damage = 80f;

    [Header("빛 기둥")]
    [SerializeField] private GameObject lightPillarPrefab;
    [SerializeField] private float lightPillarDamage = 100f;
    [SerializeField] private float lightPillarInterval = 6f;
    [SerializeField] private GameObject warningEffectPrefab;

    [Header("근거리 공격 설정")]
    [SerializeField] private float meleeAttackRange = 3f;
    [SerializeField] private float meleeAttackDamage = 150f;
    [SerializeField] private GameObject meleeAttackEffectPrefab;

    [Header("슬로우 효과 설정")]
    [SerializeField] private float meleeSlowDuration = 2f;
    [SerializeField] private float meleeSlowIntensity = 0.5f; // 50% 속도 감소
    [SerializeField] private float railgunSlowDuration = 3f;
    [SerializeField] private float railgunSlowIntensity = 0.7f; // 70% 속도 감소
    [SerializeField] private float beam360SlowDuration = 2.5f;
    [SerializeField] private float beam360SlowIntensity = 0.6f; // 60% 속도 감소
    [SerializeField] private float pillarSlowDuration = 4f;
    [SerializeField] private float pillarSlowIntensity = 0.8f; // 80% 속도 감소

    [Header("Idle 상태 설정")]
    [SerializeField] private string[] idleStateNames = { "Idle", "idle", "Boss_Idle" };
    [SerializeField] private bool useIdleParameter = true;
    [SerializeField] private string idleParameterName = "isIdle";

    private bool isPerformingAttack = false;
    private float lastRailgunTime = 0f;
    private float lastLightPillarTime = 0f;

    // 보스 상태 프로퍼티
    public bool IsPerformingSpecialAttack => isPerformingAttack;
    public bool IsInIdleState => CheckIdleState();

    protected override void Awake()
    {
        enemyType = EnemyType.MiddleBoss;
        elementType = ElementType.Lux;
        primaryDamageType = DamageType.Magical;
        enemyName = "Light Sovereign";

        // 기본 레이어 설정
        gameObject.layer = LayerMask.NameToLayer("Enemy");

        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        UpdateBossStateAnimations();
    }

    protected override void Update()
    {
        base.Update();
        UpdateBossStateAnimations();
    }

    protected override void InitializeEnemy()
    {
        base.InitializeEnemy();
        StartCoroutine(LightModeAttackRoutine());
    }

    public void SetHealth(float health)
    {
        currentHp = health;
    }

    /// <summary>
    /// Idle 상태인지 확인
    /// </summary>
    private bool CheckIdleState()
    {
        if (characterAnimator == null) return false;

        if (useIdleParameter && !string.IsNullOrEmpty(idleParameterName))
        {
            try
            {
                return characterAnimator.GetBool(idleParameterName);
            }
            catch (System.Exception)
            {
                // 파라미터가 없으면 상태 이름으로 확인
            }
        }

        AnimatorStateInfo currentState = characterAnimator.GetCurrentAnimatorStateInfo(0);
        foreach (string idleName in idleStateNames)
        {
            if (currentState.IsName(idleName))
            {
                return true;
            }
        }

        return false;
    }

    protected override void UpdateBehavior()
    {
        if (IsInIdleState || isPerformingAttack) return;

        float distanceToPlayer = GetDistanceToPlayer();

        // 근거리 공격
        if (distanceToPlayer <= meleeAttackRange)
        {
            StartCoroutine(PerformMeleeAttack());
            return;
        }

        // 레일건 빔 공격
        if (distanceToPlayer > meleeAttackRange && Time.time - lastRailgunTime >= railgunCooldown)
        {
            StartCoroutine(PerformRailgunBeamAttack());
            lastRailgunTime = Time.time;
        }

        // 빛 기둥 공격
        if (Time.time - lastLightPillarTime >= lightPillarInterval)
        {
            StartCoroutine(PerformLightPillarAttack());
            lastLightPillarTime = Time.time;
        }
    }

    private IEnumerator LightModeAttackRoutine()
    {
        yield return new WaitForSeconds(3f);

        while (currentHp > 0)
        {
            if (!IsInIdleState && !isPerformingAttack)
            {
                yield return StartCoroutine(Perform360BeamAttack());
            }
            yield return new WaitForSeconds(5f);
        }
    }

    private IEnumerator PerformMeleeAttack()
    {
        if (IsInIdleState) yield break;

        isPerformingAttack = true;
        PlayMeleeAttackAnimation();
        yield return new WaitForSeconds(0.2f);

        if (IsInIdleState)
        {
            isPerformingAttack = false;
            yield break;
        }

        if (meleeAttackEffectPrefab != null)
        {
            Vector3 attackPosition = transform.position + transform.forward * 2f;
            Instantiate(meleeAttackEffectPrefab, attackPosition, transform.rotation);
        }

        if (IsPlayerInMeleeRange())
        {
            DealDamageToPlayerWithDamage(meleeAttackDamage, DamageType.Magical);
            ApplySlowEffectToPlayer(meleeSlowDuration, meleeSlowIntensity);
        }

        yield return new WaitForSeconds(0.3f);
        isPerformingAttack = false;
    }

    private IEnumerator PerformRailgunBeamAttack()
    {
        if (IsInIdleState) yield break;

        isPerformingAttack = true;
        PlayRailgunAnimation();
        yield return new WaitForSeconds(railgunChargeTime);

        if (IsInIdleState)
        {
            isPerformingAttack = false;
            yield break;
        }

        if (playerTransform != null && railgunBeamPrefab != null)
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            GameObject beam = Instantiate(railgunBeamPrefab, transform.position, Quaternion.LookRotation(direction));
            Enemy_Final_Boss_RailgunBeam beamScript = beam.GetComponent<Enemy_Final_Boss_RailgunBeam>();
            if (beamScript != null)
            {
                beamScript.Initialize(railgunDamage, direction, 30f);
                // 레일건에 슬로우 효과 정보 전달
                beamScript.SetSlowEffect(railgunSlowDuration, railgunSlowIntensity);
            }
        }

        isPerformingAttack = false;
    }

    private IEnumerator Perform360BeamAttack()
    {
        if (IsInIdleState) yield break;

        isPerformingAttack = true;
        PlayBeam360Animation();

        for (int i = 0; i < 20; i++)
        {
            if (IsInIdleState)
            {
                isPerformingAttack = false;
                yield break;
            }

            if (playerTransform != null && beam360Prefab != null)
            {
                Vector3 direction = playerTransform.position - transform.position;
                direction.y = 0f;
                direction.Normalize();

                float angleOffset = (i - 1) * 60f;
                Quaternion yRotation = Quaternion.AngleAxis(angleOffset, Vector3.up);
                Vector3 finalDirection = yRotation * direction;
                Quaternion rotation = Quaternion.LookRotation(finalDirection, Vector3.up);

                GameObject beam360 = Instantiate(beam360Prefab, transform.position, rotation);
                Enemy_Final_Boss_360Beam beamScript = beam360.GetComponent<Enemy_Final_Boss_360Beam>();
                if (beamScript != null)
                {
                    beamScript.Initialize(beam360Damage, finalDirection * 15f);
                    // 360빔에 슬로우 효과 정보 전달
                    beamScript.SetSlowEffect(beam360SlowDuration, beam360SlowIntensity);
                }
            }

            yield return new WaitForSeconds(0.3f);
        }

        isPerformingAttack = false;
    }

    private IEnumerator PerformLightPillarAttack()
    {
        if (IsInIdleState) yield break;

        PlayLightPillarAnimation();

        if (playerTransform != null)
        {
            int pillarCount = Random.Range(4, 7);
            Vector3[] pillarPositions = CalculatePillarPositions(pillarCount);
            float warningTime = 1f;

            // 경고 이펙트 생성
            GameObject[] warnings = new GameObject[pillarCount];
            for (int i = 0; i < pillarCount; i++)
            {
                if (warningEffectPrefab != null)
                {
                    warnings[i] = Instantiate(warningEffectPrefab, pillarPositions[i] + Vector3.down * 0.1f, Quaternion.identity);
                }
            }

            yield return new WaitForSeconds(warningTime);

            // 경고 이펙트 제거
            for (int i = 0; i < warnings.Length; i++)
            {
                if (warnings[i] != null) Destroy(warnings[i]);
            }

            if (IsInIdleState) yield break;

            // 빛 기둥 생성
            for (int i = 0; i < pillarCount; i++)
            {
                if (lightPillarPrefab != null)
                {
                    GameObject pillar = Instantiate(lightPillarPrefab, pillarPositions[i], Quaternion.identity);
                    Enemy_Final_Boss_LightPillar pillarScript = pillar.GetComponent<Enemy_Final_Boss_LightPillar>();
                    if (pillarScript != null)
                    {
                        pillarScript.StartLightPillar(lightPillarDamage, 2.0f);
                        // 빛 기둥에 슬로우 효과 정보 전달
                        pillarScript.SetSlowEffect(pillarSlowDuration, pillarSlowIntensity);
                    }
                }

                if (i < pillarCount - 1) yield return new WaitForSeconds(0.2f);
            }
        }
    }

    /// <summary>
    /// 빛 기둥 위치 계산 (보스와의 충돌 방지)
    /// </summary>
    private Vector3[] CalculatePillarPositions(int count)
    {
        Vector3[] positions = new Vector3[count];
        Vector3 playerPos = playerTransform.position;
        Vector3 bossPos = transform.position;

        // 이 값을 늘리면 플레이어를 중심으로 원형 배치되는 기둥들이 더 멀리 떨어집니다.
        float radius = 8f; // 기존 5f에서 8f로 증가

        // 이 값을 늘리면 보스에게서 최소한 이 거리만큼 떨어지게 됩니다.
        float minDistanceFromBoss = 5f; // 기존 3f에서 5f로 증가

        for (int i = 0; i < count; i++)
        {
            Vector3 candidatePosition;

            if (i == 0)
            {
                // 플레이어 예측 위치
                Vector3 playerVelocity = Vector3.zero;
                if (playerTransform.GetComponent<Rigidbody>() != null)
                {
                    playerVelocity = playerTransform.GetComponent<Rigidbody>().velocity;
                }
                candidatePosition = playerPos + playerVelocity * 1.5f;
                candidatePosition.y = 0f;
            }
            else
            {
                // 원형 배치
                float angle = ((float)(i - 1) / (count - 1)) * 360f * Mathf.Deg2Rad;
                float randomRadius = radius + Random.Range(-2f, 2f);
                float randomAngle = angle + Random.Range(-60f, 60f) * Mathf.Deg2Rad;

                Vector3 offset = new Vector3(
                    Mathf.Cos(randomAngle) * randomRadius,
                    0f,
                    Mathf.Sin(randomAngle) * randomRadius
                );

                candidatePosition = playerPos + offset;
                candidatePosition.y = 0f;
            }

            // 보스와 너무 가까우면 재배치
            if (Vector3.Distance(candidatePosition, bossPos) < minDistanceFromBoss)
            {
                Vector3 awayFromBoss = (candidatePosition - bossPos).normalized;
                candidatePosition = bossPos + awayFromBoss * minDistanceFromBoss;
                candidatePosition.y = 0f;
            }

            positions[i] = candidatePosition;
        }

        return positions;
    }
    protected override void UpdateMovement() { }

    private bool IsPlayerInMeleeRange()
    {
        return IsPlayerInRange(meleeAttackRange);
    }

    private void PlayMeleeAttackAnimation()
    {
        if (characterAnimator != null && !IsInIdleState)
        {
            characterAnimator.SetTrigger("meleeAttack");
        }
    }

    private void PlayRailgunAnimation()
    {
        if (characterAnimator != null && !IsInIdleState)
        {
            characterAnimator.SetTrigger("railgun");
        }
    }

    private void PlayBeam360Animation()
    {
        if (characterAnimator != null && !IsInIdleState)
        {
            characterAnimator.SetTrigger("beam360");
        }
    }

    private void PlayLightPillarAnimation()
    {
        if (characterAnimator != null && !IsInIdleState)
        {
            characterAnimator.SetTrigger("lightPillar");
        }
    }

    private void UpdateBossStateAnimations()
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetBool("isPerformingSpecialAttack", isPerformingAttack);
            float healthPercentage = currentHp / enemyStats.Get(EnemyStatType.MaxHp);
            characterAnimator.SetFloat("healthPercentage", healthPercentage);
        }
    }

    public void SetIdleState(bool isIdle)
    {
        if (characterAnimator != null && useIdleParameter && !string.IsNullOrEmpty(idleParameterName))
        {
            characterAnimator.SetBool(idleParameterName, isIdle);
        }
    }

    protected override void PerformAttack() { }

    private void DealDamageToPlayerWithDamage(float damage, DamageType damageType = DamageType.Magical)
    {
        if (playerScript == null) return;
        playerScript.DecreaseHP(damage);
    }

    /// <summary>
    /// 플레이어에게 슬로우 효과 적용
    /// </summary>
    private void ApplySlowEffectToPlayer(float duration, float intensity)
    {
        if (playerScript == null) return;

        // 플레이어 스크립트에 슬로우 효과 적용 시도 (여러 방법)
        // 방법 1: Player 스크립트에 ApplySlowEffect 메서드가 있는 경우
        var playerObj = playerScript.gameObject;

        // Player 스크립트에서 슬로우 효과 메서드 찾기
        var slowMethod = playerScript.GetType().GetMethod("ApplySlowEffect");
        if (slowMethod != null)
        {
            slowMethod.Invoke(playerScript, new object[] { duration, intensity });
            Debug.Log($"플레이어에게 슬로우 효과 적용: {intensity * 100}% 감속, {duration}초 지속");
            return;
        }

        // 방법 2: Player Movement 컴포넌트가 있는 경우
        var movementComponent = playerObj.GetComponent<MonoBehaviour>();
        if (movementComponent != null)
        {
            var moveSlowMethod = movementComponent.GetType().GetMethod("ApplySlowEffect");
            if (moveSlowMethod != null)
            {
                moveSlowMethod.Invoke(movementComponent, new object[] { duration, intensity });
                Debug.Log($"플레이어 이동에 슬로우 효과 적용: {intensity * 100}% 감속, {duration}초 지속");
            }
        }

        // 방법 3: 직접 Rigidbody 제어 (임시 방법)
        var playerRb = playerObj.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            StartCoroutine(ApplyTemporarySlowEffect(playerRb, duration, intensity));
            Debug.Log($"플레이어에게 임시 슬로우 효과 적용: {intensity * 100}% 감속, {duration}초 지속");
        }
    }

    /// <summary>
    /// 임시 슬로우 효과 (Rigidbody 직접 제어)
    /// </summary>
    private IEnumerator ApplyTemporarySlowEffect(Rigidbody playerRb, float duration, float intensity)
    {
        float originalDrag = playerRb.drag;
        float slowDrag = originalDrag + (intensity * 10f); // 드래그 증가로 슬로우 효과

        playerRb.drag = slowDrag;
        yield return new WaitForSeconds(duration);
        playerRb.drag = originalDrag;
    }

    /// <summary>
    /// 공개 메서드: 외부에서 플레이어에게 슬로우 효과 적용 (빔 스크립트용)
    /// </summary>
    public void ApplySlowToPlayer(float duration, float intensity)
    {
        ApplySlowEffectToPlayer(duration, intensity);
    }

    protected override void Die()
    {
        base.Die();
    }

    protected override int GetExperienceReward()
    {
        return 1000;
    }
}