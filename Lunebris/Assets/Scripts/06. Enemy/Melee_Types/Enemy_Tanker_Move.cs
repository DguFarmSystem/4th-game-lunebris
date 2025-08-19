using UnityEngine;

/// <summary>
/// 완성된 근접 탱커 이동 처리 스크립트
/// 기본적인 이동과 모든 애니메이션 연동 처리
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Tanker_Move : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float rotationSpeed = 3f; // 탱커는 회전이 느림
    [SerializeField] private bool enableDebugLogs = false;

    // 컴포넌트 참조
    private Transform target;
    private Rigidbody rigid;
    private Enemy_Tanker simpleTanker;
    private Animator characterAnimator;
    private Enemy_Base enemyBase;

    // 애니메이션 파라미터 이름들
    private readonly string ANIM_IS_MOVING = "isMoving";
    private readonly string ANIM_MOVE_SPEED = "moveSpeed";
    private readonly string ANIM_ATTACK_TRIGGER = "attack";
    private readonly string ANIM_PROJECTILE_ATTACK_TRIGGER = "projectileAttack";
    private readonly string ANIM_HIT_TRIGGER = "hit";
    private readonly string ANIM_DIE_TRIGGER = "die";
    private readonly string ANIM_IS_DEAD = "isDead";

    // 상태 추적
    private bool wasMoving = false;
    private bool isInitialized = false;

    #region Unity Lifecycle

    private void Start()
    {
        InitializeComponents();
        InitializeAnimator();
    }

    private void FixedUpdate()
    {
        if (!isInitialized) return;

        // 기본 조건 체크
        if (simpleTanker == null || target == null || simpleTanker.IsDead())
        {
            StopMovement();
            UpdateAnimation(false, 0f);
            return;
        }

        // 이동 가능한지 체크
        if (ShouldMove())
        {
            NormalMove();
        }
        else
        {
            StopMovement();
            UpdateAnimation(false, 0f);
        }
    }

    #endregion

    #region 초기화

    private void InitializeComponents()
    {
        // 플레이어 찾기
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (target == null)
        {
            target = GameObject.Find("Player")?.transform;
        }

        if (target == null)
        {
            Debug.LogError($"{name}: 플레이어를 찾을 수 없습니다!");
            return;
        }

        // 컴포넌트 참조
        rigid = GetComponent<Rigidbody>();
        simpleTanker = GetComponent<Enemy_Tanker>();
        enemyBase = GetComponent<Enemy_Base>();

        if (simpleTanker == null)
        {
            Debug.LogError($"{name}: Enemy_Tanker 컴포넌트를 찾을 수 없습니다!");
            return;
        }

        DebugLog("컴포넌트 초기화 완료");
        isInitialized = true;
    }

    private void InitializeAnimator()
    {
        // 자식에서 Animator 찾기
        characterAnimator = GetComponentInChildren<Animator>();

        if (characterAnimator == null)
        {
            Debug.LogWarning($"{name}: Animator를 찾을 수 없습니다. 애니메이션이 재생되지 않습니다.");
            return;
        }

        // 애니메이션 파라미터 초기화
        SafeSetBool(ANIM_IS_MOVING, false);
        SafeSetBool(ANIM_IS_DEAD, false);
        SafeSetFloat(ANIM_MOVE_SPEED, 0f);

        // 모든 트리거 초기화
        characterAnimator.ResetTrigger(ANIM_ATTACK_TRIGGER);
        characterAnimator.ResetTrigger(ANIM_PROJECTILE_ATTACK_TRIGGER);
        characterAnimator.ResetTrigger(ANIM_HIT_TRIGGER);
        characterAnimator.ResetTrigger(ANIM_DIE_TRIGGER);

        DebugLog("애니메이터 초기화 완료");
    }

    #endregion

    #region 이동 로직

    /// <summary>
    /// 이동 가능 여부 판단
    /// </summary>
    private bool ShouldMove()
    {
        // Enemy_Base가 죽었는지 확인
        if (enemyBase != null && enemyBase.IsDead())
        {
            return false;
        }

        // 공격 중이면 이동 불가
        if (simpleTanker.IsAttacking || simpleTanker.IsProjectileAttacking)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 일반 이동 (천천히 플레이어 추격)
    /// </summary>
    private void NormalMove()
    {
        Vector3 dirVector = target.position - transform.position;
        dirVector.y = 0; // Y축 이동 제거

        bool isMoving = dirVector.magnitude > 0.1f; // 너무 가까우면 이동하지 않음

        if (isMoving)
        {
            // 탱커의 기본 이동 속도 사용
            float moveSpeed = simpleTanker.GetEnemyStats().Get(Enemy.EnemyStatType.MoveSpeed);
            Vector3 moveVector = dirVector.normalized * moveSpeed * Time.fixedDeltaTime;

            // Rigidbody를 사용한 이동
            if (rigid != null)
            {
                rigid.MovePosition(rigid.position + moveVector);
            }
            else
            {
                // Rigidbody가 없으면 Transform 이동
                transform.position += moveVector;
            }

            // 플레이어 방향으로 회전
            LookAtTarget(dirVector);

            // 애니메이션 업데이트
            UpdateAnimation(true, moveSpeed);

            DebugLog($"이동 중 - 속도: {moveSpeed:F1}, 거리: {dirVector.magnitude:F1}");
        }
        else
        {
            // 정지 상태
            UpdateAnimation(false, 0f);
        }
    }

    /// <summary>
    /// 목표 방향으로 회전
    /// </summary>
    private void LookAtTarget(Vector3 direction)
    {
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            // 탱커는 회전도 느리게
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
        }
    }

    /// <summary>
    /// 이동 정지
    /// </summary>
    private void StopMovement()
    {
        if (rigid != null)
        {
            rigid.velocity = Vector3.zero;
        }
    }

    #endregion

    #region 애니메이션 관리

    /// <summary>
    /// 이동 애니메이션 업데이트
    /// </summary>
    private void UpdateAnimation(bool isMoving, float moveSpeed)
    {
        if (characterAnimator == null) return;

        // 이동 상태가 변경되었을 때만 애니메이션 파라미터 업데이트
        if (wasMoving != isMoving)
        {
            SafeSetBool(ANIM_IS_MOVING, isMoving);
            wasMoving = isMoving;
            DebugLog($"애니메이션 상태 변경: isMoving = {isMoving}");
        }

        // 이동 속도 업데이트 (moveSpeed 파라미터가 있을 때만)
        SafeSetFloat(ANIM_MOVE_SPEED, isMoving ? moveSpeed : 0f);
    }

    /// <summary>
    /// 근접 공격 애니메이션
    /// </summary>
    public void PlayAttackAnimation()
    {
        DebugLog("근접 공격 애니메이션 재생");
        if (characterAnimator == null)
        {
            Debug.LogWarning($"{name}: Animator가 없어서 공격 애니메이션 재생 불가");
            return;
        }

        SafeSetTrigger(ANIM_ATTACK_TRIGGER);
    }

    /// <summary>
    /// 투사체 공격 애니메이션
    /// </summary>
    public void PlayProjectileAttackAnimation()
    {
        DebugLog("투사체 공격 애니메이션 재생");
        if (characterAnimator == null)
        {
            Debug.LogWarning($"{name}: Animator가 없어서 투사체 공격 애니메이션 재생 불가");
            return;
        }

        // projectileAttack 트리거가 없으면 기본 attack 사용
        if (HasParameter(ANIM_PROJECTILE_ATTACK_TRIGGER))
        {
            SafeSetTrigger(ANIM_PROJECTILE_ATTACK_TRIGGER);
        }
        else
        {
            SafeSetTrigger(ANIM_ATTACK_TRIGGER);
            DebugLog("projectileAttack 트리거가 없어서 attack 트리거 사용");
        }
    }

    /// <summary>
    /// 피격 애니메이션
    /// </summary>
    public void PlayHitAnimation()
    {
        DebugLog("피격 애니메이션 재생");
        if (characterAnimator == null)
        {
            Debug.LogWarning($"{name}: Animator가 없어서 피격 애니메이션 재생 불가");
            return;
        }

        SafeSetTrigger(ANIM_HIT_TRIGGER);
    }

    /// <summary>
    /// 죽음 애니메이션
    /// </summary>
    public void PlayDeathAnimation()
    {
        DebugLog("죽음 애니메이션 재생");
        if (characterAnimator == null)
        {
            Debug.LogWarning($"{name}: Animator가 없어서 죽음 애니메이션 재생 불가");
            return;
        }

        SafeSetTrigger(ANIM_DIE_TRIGGER);
        SafeSetBool(ANIM_IS_DEAD, true);

        // 물리 효과 정지
        StopMovement();

        DebugLog("죽음 애니메이션 트리거 및 isDead = true 설정 완료");
    }

    #endregion

    #region 애니메이션 안전 메서드

    /// <summary>
    /// 안전한 Bool 파라미터 설정
    /// </summary>
    private void SafeSetBool(string paramName, bool value)
    {
        if (characterAnimator != null && HasParameter(paramName))
        {
            characterAnimator.SetBool(paramName, value);
        }
    }

    /// <summary>
    /// 안전한 Float 파라미터 설정
    /// </summary>
    private void SafeSetFloat(string paramName, float value)
    {
        if (characterAnimator != null && HasParameter(paramName))
        {
            characterAnimator.SetFloat(paramName, value);
        }
    }

    /// <summary>
    /// 안전한 Trigger 파라미터 설정
    /// </summary>
    private void SafeSetTrigger(string paramName)
    {
        if (characterAnimator != null && HasParameter(paramName))
        {
            characterAnimator.SetTrigger(paramName);
        }
        else if (characterAnimator != null)
        {
            Debug.LogWarning($"{name}: '{paramName}' 트리거가 Animator Controller에 없습니다!");
        }
    }

    /// <summary>
    /// Animator에 특정 파라미터가 있는지 확인
    /// </summary>
    private bool HasParameter(string parameterName)
    {
        if (characterAnimator == null) return false;

        foreach (AnimatorControllerParameter param in characterAnimator.parameters)
        {
            if (param.name == parameterName)
                return true;
        }
        return false;
    }

    #endregion

    #region 유틸리티 메서드

    /// <summary>
    /// 디버그 로그 출력
    /// </summary>
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[{name}] {message}");
        }
    }

    /// <summary>
    /// 현재 애니메이션 상태 정보 반환 (디버그용)
    /// </summary>
    public string GetAnimationStatus()
    {
        if (characterAnimator == null) return "Animator 없음";

        AnimatorStateInfo stateInfo = characterAnimator.GetCurrentAnimatorStateInfo(0);
        return $"현재 상태: {stateInfo.shortNameHash}, 진행도: {stateInfo.normalizedTime:F2}";
    }

    /// <summary>
    /// 애니메이터 강제 리셋 (디버그용)
    /// </summary>
    public void ResetAnimator()
    {
        if (characterAnimator == null) return;

        // 모든 트리거 리셋
        characterAnimator.ResetTrigger(ANIM_ATTACK_TRIGGER);
        characterAnimator.ResetTrigger(ANIM_PROJECTILE_ATTACK_TRIGGER);
        characterAnimator.ResetTrigger(ANIM_HIT_TRIGGER);
        characterAnimator.ResetTrigger(ANIM_DIE_TRIGGER);

        // Bool 파라미터 초기화
        SafeSetBool(ANIM_IS_MOVING, false);
        SafeSetBool(ANIM_IS_DEAD, false);
        SafeSetFloat(ANIM_MOVE_SPEED, 0f);

        wasMoving = false;
        DebugLog("애니메이터 리셋 완료");
    }

    #endregion

    #region 공개 속성

    /// <summary>
    /// 이동 중인지 여부
    /// </summary>
    public bool IsMoving => wasMoving;

    /// <summary>
    /// 애니메이터 참조
    /// </summary>
    public Animator CharacterAnimator => characterAnimator;

    /// <summary>
    /// 초기화 완료 여부
    /// </summary>
    public bool IsInitialized => isInitialized;

    #endregion
}