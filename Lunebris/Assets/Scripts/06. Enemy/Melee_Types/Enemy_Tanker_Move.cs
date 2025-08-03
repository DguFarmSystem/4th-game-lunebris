using UnityEngine;

/// <summary>
/// 단순화된 근접 탱커 이동 처리 스크립트
/// 기본적인 이동만 처리
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Tanker_Move : MonoBehaviour
{
    private Transform target;
    private Rigidbody rigid;
    private Enemy_Tanker simpleTanker;

    private void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (target == null)
        {
            target = GameObject.Find("Player")?.transform;
        }

        rigid = GetComponent<Rigidbody>();
        simpleTanker = GetComponent<Enemy_Tanker>();
    }

    private void FixedUpdate()
    {
        // 기본 조건 체크
        if (simpleTanker == null || target == null || simpleTanker.IsDead())
        {
            StopMovement();
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
        }
    }

    /// <summary>
    /// 이동 가능 여부 판단
    /// </summary>
    private bool ShouldMove()
    {
        // 공격 중이면 이동 불가
        if (simpleTanker.IsAttacking)
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

        if (dirVector.magnitude > 0.1f) // 너무 가까우면 이동하지 않음
        {
            // 탱커의 기본 이동 속도 사용 (일반적으로 느림)
            float moveSpeed = simpleTanker.GetEnemyStats().Get(Enemy.EnemyStatType.MoveSpeed);
            Vector3 moveVector = dirVector.normalized * moveSpeed * Time.fixedDeltaTime;

            rigid.MovePosition(rigid.position + moveVector);

            // 플레이어 방향으로 회전
            LookAtTarget(dirVector);
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
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 3f);
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
}