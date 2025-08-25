using UnityEngine;
using Enemy;

/// <summary>
/// 마법사 AI 클래스 - Enemy_Base에 상속 (AP형) - 메테오 스킬 추가
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Wizard : Enemy_Base
{
    [Header("마법사 공격 설정")]
    public GameObject magicOrbPrefab;     // 마법 구체 프리팹
    public GameObject meteorPrefab;       // 메테오 프리팹
    public Transform castPoint;           // 마법 시전 위치
    public float attackRange = 10f;       // 공격 사거리
    public float attackCooldown = 2.5f;   // 공격 쿨다운
    public float castDuration = 0.5f;     // 마법 시전 지속 시간 (멈춰있는 시간)
    public float orbSpeed = 6f;           // 마법 구체 속도

    [Header("메테오 스킬 설정")]
    public float meteorCooldown = 8f;     // 메테오 스킬 쿨다운
    public float meteorCastTime = 1.5f;   // 메테오 시전 시간
    public float meteorHeight = 15f;      // 메테오 생성 높이
    public float meteorFallSpeed = 8f;    // 메테오 낙하 속도
    public float meteorDamageRadius = 3f; // 메테오 피해 반경
    public int meteorUseCondition = 3;    // 몇 번째 공격마다 메테오 사용
    public GameObject warningIndicator;   // 바닥 경고 표시기 프리팹

    private float lastAttackTime;
    private float lastMeteorTime;
    private bool isCasting = false;       // 마법 시전 중인지 여부
    private bool isCastingMeteor = false; // 메테오 시전 중인지 여부
    private int attackCount = 0;          // 공격 횟수 카운터
    private GameObject currentWarning;    // 현재 생성된 경고 표시기

    // 마법사 전용 애니메이션 파라미터 이름들
    private readonly string ANIM_CAST_METEOR = "castMeteor";
    private readonly string ANIM_IS_CASTING_METEOR = "isCastingMeteor";

    // Move 스크립트에서 참조할 수 있는 프로퍼티
    public bool IsCasting => isCasting || isCastingMeteor;

    #region Enemy_Base 오버라이드

    protected override void InitializeEnemy()
    {
        // 스탯 시스템 설정
        enemyType = EnemyType.RangedAP;
        primaryDamageType = DamageType.Magical; // AP 드라이버로 마법 데미지

        // 기본 초기화 로직
        if (castPoint == null)
        {
            Transform childCastPoint = transform.Find("CastPoint");
            if (childCastPoint != null)
            {
                castPoint = childCastPoint;
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

        // 메테오 시전 상태 애니메이션 업데이트
        UpdateMeteorCastingAnimation();
    }

    protected override void PerformAttack()
    {
        attackCount++;

        // 메테오 스킬 사용 조건 체크
        if (ShouldUseMeteor())
        {
            CastMeteor();
        }
        else
        {
            // 기본 마법 구체 공격
            CastMagicOrb();
        }

        lastAttackTime = Time.time;
    }

    protected override void UpdateMovement()
    {
        // 이동은 Enemy_Wizard_Move에서 처리하므로 비어둠
    }

    #endregion

    #region 마법사 전용 애니메이션

    /// <summary>
    /// 메테오 시전 애니메이션 재생
    /// </summary>
    private void PlayCastMeteorAnimation()
    {
        if (characterAnimator == null) return;

        characterAnimator.SetTrigger(ANIM_CAST_METEOR);
        Debug.Log($"{enemyName} 메테오 시전 애니메이션 재생");
    }

    /// <summary>
    /// 메테오 시전 상태 애니메이션 파라미터 업데이트
    /// </summary>
    private void UpdateMeteorCastingAnimation()
    {
        if (characterAnimator == null) return;

        // 메테오 시전 상태 Bool 파라미터 업데이트
        characterAnimator.SetBool(ANIM_IS_CASTING_METEOR, isCastingMeteor);
    }

    #endregion

    #region 마법사 전용 로직

    private void HandleCombat(float distanceToPlayer)
    {
        LookAtPlayer();

        // 공격 범위 내에 있고 쿨다운이 끝났으며 시전 중이 아닐 때
        if (distanceToPlayer <= attackRange && Time.time > lastAttackTime + attackCooldown && !IsCasting)
        {
            PerformAttack();
        }
    }

    private bool ShouldUseMeteor()
    {
        return attackCount % meteorUseCondition == 0 &&
               Time.time > lastMeteorTime + meteorCooldown &&
               playerTransform != null;
    }

    private void CastMagicOrb()
    {
        if (magicOrbPrefab != null && castPoint != null && playerTransform != null)
        {
            // 시전 시작
            isCasting = true;

            // 기본 공격 애니메이션 재생 (Enemy_Base의 attack 파라미터 사용)
            PlayAttackAnimation();

            // 마법 구체 복제 생성
            GameObject orb = Instantiate(magicOrbPrefab, castPoint.position, castPoint.rotation);

            // 플레이어 방향으로 마법 구체 회전
            Vector3 targetPos = playerTransform.position;
            targetPos.y = castPoint.position.y;
            orb.transform.LookAt(targetPos);

            // 마법 구체에 스탯 적용
            var orbComponent = orb.GetComponent<Enemy_Wizard_MagicOrb>();

            // 일정 시간 후 시전 상태 해제
            Invoke(nameof(EndCasting), castDuration);

            Debug.Log($"{enemyName} 마법 구체 공격! 마법 데미지: {GetMainDamage()}");
        }
    }

    private void CastMeteor()
    {
        if (meteorPrefab != null && playerTransform != null)
        {
            // 메테오 시전 시작
            isCastingMeteor = true;
            lastMeteorTime = Time.time;

            // 메테오 시전 애니메이션 재생
            PlayCastMeteorAnimation();

            // 플레이어의 현재 위치를 예측 (시전 시간 + 낙하 시간 고려)
            Vector3 playerVelocity = Vector3.zero;
            if (playerTransform.GetComponent<Rigidbody>() != null)
            {
                playerVelocity = playerTransform.GetComponent<Rigidbody>().velocity;
            }

            float totalTime = meteorCastTime + (meteorHeight / meteorFallSpeed);
            Vector3 predictedPosition = playerTransform.position + playerVelocity * totalTime * 0.5f;
            predictedPosition.y = playerTransform.position.y; // Y축은 플레이어 높이로 고정

            // 즉시 경고 표시기 생성 (시전 시작과 동시에)
            CreateWarningAtPosition(predictedPosition);

            Debug.Log($"{enemyName} 메테오 시전 시작! 예상 위치: {predictedPosition}");

            // 메테오 시전 후 일정 시간 뒤에 실제 메테오 생성
            Invoke(nameof(SpawnMeteor), meteorCastTime);
            Invoke(nameof(EndMeteorCasting), meteorCastTime);

            // 메테오가 땅에 떨어질 때까지의 총 시간 계산하여 경고 표시기 파괴 예약
            float fallTime = meteorHeight / meteorFallSpeed;
            float totalMeteorTime = meteorCastTime + fallTime;
            Invoke(nameof(DestroyWarningIndicator), totalMeteorTime);
        }
    }

    private void CreateWarningAtPosition(Vector3 position)
    {
        if (warningIndicator != null)
        {
            // 기존 경고가 있다면 제거
            if (currentWarning != null)
            {
                Destroy(currentWarning);
            }

            // 바닥에 경고 표시기 생성
            Vector3 warningPos = new Vector3(position.x, position.y + 0.1f, position.z);
            currentWarning = Instantiate(warningIndicator, warningPos, Quaternion.identity);

            Debug.Log($"경고 표시기 생성: {warningPos}");
        }
    }

    private void SpawnMeteor()
    {
        if (meteorPrefab != null && currentWarning != null)
        {
            // 경고 표시기 위치를 목표 지점으로 사용
            Vector3 targetPosition = currentWarning.transform.position;
            targetPosition.y = currentWarning.transform.position.y - 0.1f; // 바닥 높이로 조정

            // 메테오 생성 위치 (목표 지점 위 높은 곳)
            Vector3 meteorSpawnPos = new Vector3(
                targetPosition.x,
                targetPosition.y + meteorHeight,
                targetPosition.z
            );

            // 메테오 생성
            GameObject meteor = Instantiate(meteorPrefab, meteorSpawnPos, Quaternion.identity);

            // 메테오 컴포넌트 설정
            var meteorComponent = meteor.GetComponent<Enemy_Wizard_Meteor>();
            if (meteorComponent != null)
            {
                meteorComponent.Initialize(
                    targetPosition,
                    meteorFallSpeed,
                    GetMainDamage() * 1.5f, // 메테오는 1.5배 데미지
                    meteorDamageRadius,
                    this,
                    currentWarning, // 경고 표시기 전달
                    this.elementType // 마법사의 속성을 메테오에 전달
                );
            }

            Debug.Log($"{enemyName} 메테오 소환! 목표 위치: {targetPosition}, 속성: {elementType}");
        }
    }

    /// <summary>
    /// 경고 표시기를 파괴하는 메서드
    /// </summary>
    private void DestroyWarningIndicator()
    {
        if (currentWarning != null)
        {
            Debug.Log($"{enemyName} 메테오 착지! 경고 표시기 파괴");
            Destroy(currentWarning);
            currentWarning = null;
        }
    }

    private void LookAtPlayer()
    {
        if (playerTransform == null) return;

        Vector3 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 2f);
        }
    }

    private void EndCasting()
    {
        isCasting = false;
        Debug.Log($"{enemyName} 마법 시전 완료, 이동 재개");
    }

    private void EndMeteorCasting()
    {
        isCastingMeteor = false;
        Debug.Log($"{enemyName} 메테오 시전 완료");
    }

    #endregion

    #region 정리 및 해제

    private void OnDestroy()
    {
        // 마법사가 파괴될 때 경고 표시기도 정리
        if (currentWarning != null)
        {
            Destroy(currentWarning);
        }

        // 예약된 Invoke 취소
        CancelInvoke();
    }

    private void OnDisable()
    {
        // 마법사가 비활성화될 때 경고 표시기도 정리
        if (currentWarning != null)
        {
            Destroy(currentWarning);
        }

        // 예약된 Invoke 취소
        CancelInvoke();
    }

    #endregion

    #region 기즈모 (유지)

    private void OnDrawGizmosSelected()
    {
        // 기본 공격 범위
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 스탯 시스템: 감지 범위 표시
        if (enemyStats != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, enemyStats.Get(EnemyStatType.DetectionRange));
        }

        // 메테오 피해 반경 (플레이어 위치 기준)
        if (playerTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerTransform.position, meteorDamageRadius);
        }

        // 현재 경고 표시기 위치 (메테오 시전 중일 때)
        if (currentWarning != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(currentWarning.transform.position, meteorDamageRadius);
            Gizmos.DrawWireCube(currentWarning.transform.position, Vector3.one * 0.5f);
        }
    }

    #endregion
}