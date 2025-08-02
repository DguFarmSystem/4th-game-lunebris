using UnityEngine;

/// <summary>
/// 단순화된 근접 적 이동 처리 스크립트 (Y축 고정 버전)
/// 기본적인 이동만 처리
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Assassin_Move : MonoBehaviour
{
    private Transform target;
    private Rigidbody rigid;
    private Enemy_Assassin simpleAssassin;

    private void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (target == null)
        {
            target = GameObject.Find("Player")?.transform;
        }

        rigid = GetComponent<Rigidbody>();
        simpleAssassin = GetComponent<Enemy_Assassin>();
    }

    private void FixedUpdate()
    {
        // 기본 조건 체크
        if (simpleAssassin == null || target == null || simpleAssassin.IsDead())
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
        if (simpleAssassin.IsAttacking)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 일반 이동 (플레이어 추격) - Y축 고정
    /// </summary>
    private void NormalMove()
    {
        Vector3 dirVector = target.position - transform.position;
        dirVector.y = 0; // Y축 이동 제거

        if (dirVector.magnitude > 0.1f) // 너무 가까우면 이동하지 않음
        {
            // 이동 속도 스탯 사용
            float moveSpeed = simpleAssassin.GetEnemyStats().Get(Enemy.EnemyStatType.MoveSpeed);
            Vector3 moveVector = dirVector.normalized * moveSpeed * Time.fixedDeltaTime;

            Vector3 newPosition = rigid.position + moveVector;
            newPosition.y = rigid.position.y; // Y축 위치 고정
            rigid.MovePosition(newPosition);

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
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 5f);
        }
    }

    /// <summary>
    /// 이동 정지
    /// </summary>
    private void StopMovement()
    {
        if (rigid != null)
        {
            // X, Z축 속도만 0으로 설정, Y축은 중력 유지
            rigid.velocity = new Vector3(0, rigid.velocity.y, 0);
        }
    }
}