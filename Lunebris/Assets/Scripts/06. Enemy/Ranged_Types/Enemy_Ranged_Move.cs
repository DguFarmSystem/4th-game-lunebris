using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_Ranged_Move : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private string idleStateName = "Idle"; // Idle 애니메이션 상태 이름
    [SerializeField] private string[] movingStateNames = { "Walk", "Run", "Move" }; // 움직이는 애니메이션 상태 이름들

    private Transform target;
    private Rigidbody rigid;
    private Enemy_Ranged enemyRanged; // Enemy_Ranged 참조 추가
    private Enemy_Base enemyBase; // Enemy_Base 참조 추가 (죽음 상태 확인용)
    private Animator characterAnimator; // 애니메이터 참조 추가

    private void Start()
    {
        target = GameObject.Find("Player").transform;
        rigid = GetComponent<Rigidbody>();
        enemyRanged = GetComponent<Enemy_Ranged>(); // 참조 가져오기
        enemyBase = GetComponent<Enemy_Base>(); // Enemy_Base 참조 가져오기

        // 애니메이터 찾기 (자식에서도 찾기)
        characterAnimator = GetComponent<Animator>();
        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<Animator>();
        }

        // Enemy_Base가 없으면 경고 메시지 출력
        if (enemyBase == null)
        {
            Debug.LogWarning($"{name}: Enemy_Base 컴포넌트를 찾을 수 없습니다. 죽음 상태 체크가 불가능합니다.");
        }
    }

    private void FixedUpdate()
    {
        // 죽은 상태면 모든 움직임 중단
        if (enemyBase != null && enemyBase.IsDead())
        {
            StopMovement();
            UpdateMovementAnimation(false);
            return;
        }

        // 현재 움직이는 애니메이션이 재생 중인지 확인
        bool isPlayingMovingAnimation = IsPlayingMovingAnimation();

        // 움직이는 애니메이션이 재생 중이면 무조건 움직임 (Idle 상태 무시)
        // 하지만 죽었으면 움직이지 않음 (위에서 이미 체크했으므로 여기서는 안전)
        if (isPlayingMovingAnimation)
        {
            Move();
            return;
        }

        // 움직이는 애니메이션이 아닐 때만 다른 조건들 확인

        // Idle 모션 중이면 움직이지 않음
        bool isInIdleState = IsPlayingIdleAnimation();
        if (isInIdleState)
        {
            StopMovement();
            UpdateMovementAnimation(false);
            return;
        }

        // 공격 중이 아닐 때만 이동
        if (enemyRanged == null || !enemyRanged.IsAttacking)
        {
            Move();
            UpdateMovementAnimation(true);
        }
        else
        {
            // 공격 중일 때는 멈춤
            StopMovement();
            UpdateMovementAnimation(false);
        }
    }

    /// <summary>
    /// 현재 움직이는 애니메이션이 재생 중인지 확인
    /// </summary>
    private bool IsPlayingMovingAnimation()
    {
        if (characterAnimator == null) return false;

        // 현재 애니메이션 상태 정보 가져오기
        AnimatorStateInfo stateInfo = characterAnimator.GetCurrentAnimatorStateInfo(0);

        // 움직이는 상태들 중 하나인지 확인
        foreach (string movingState in movingStateNames)
        {
            if (stateInfo.IsName(movingState))
            {
                return true;
            }
        }

        // 태그로도 확인 (Movement 태그가 있는 상태들)
        if (stateInfo.IsTag("Movement") || stateInfo.IsTag("Moving"))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 현재 Idle 애니메이션이 재생 중인지 확인
    /// </summary>
    private bool IsPlayingIdleAnimation()
    {
        if (characterAnimator == null) return false;

        // 현재 애니메이션 상태 정보 가져오기
        AnimatorStateInfo stateInfo = characterAnimator.GetCurrentAnimatorStateInfo(0);

        // Idle 상태인지 확인 (상태 이름 또는 태그로 확인)
        bool isIdleState = stateInfo.IsName(idleStateName) || stateInfo.IsTag("Idle");

        // 애니메이션이 진행 중인지 확인 (normalizedTime < 1이면 아직 진행 중)
        bool isAnimationPlaying = stateInfo.normalizedTime < 1.0f;

        return isIdleState && isAnimationPlaying;
    }

    /// <summary>
    /// 이동 정지
    /// </summary>
    private void StopMovement()
    {
        if (rigid != null)
        {
            rigid.velocity = new Vector3(0, rigid.velocity.y, 0);
        }
    }

    private void Move()
    {
        if (target == null) return;

        Vector3 dirVector = target.position - transform.position;
        dirVector.y = 0;
        Vector3 moveVector = dirVector.normalized * speed * Time.fixedDeltaTime;
        rigid.MovePosition(rigid.position + moveVector);
    }

    /// <summary>
    /// 이동 애니메이션 파라미터 업데이트
    /// </summary>
    private void UpdateMovementAnimation(bool isMoving)
    {
        if (characterAnimator == null) return;

        // 이동 애니메이션 제어
        characterAnimator.SetBool("isMoving", isMoving);
        characterAnimator.SetFloat("moveSpeed", isMoving ? speed : 0f);
    }
}