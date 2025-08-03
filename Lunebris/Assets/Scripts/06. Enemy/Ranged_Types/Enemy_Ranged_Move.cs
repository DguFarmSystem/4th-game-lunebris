using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_Ranged_Move : MonoBehaviour
{
    [SerializeField] private float speed = 2f;

    private Transform target;
    private Rigidbody rigid;
    private Enemy_Ranged enemyRanged; // Enemy_Ranged 참조 추가

    private void Start()
    {
        target = GameObject.Find("Player").transform;
        rigid = GetComponent<Rigidbody>();
        enemyRanged = GetComponent<Enemy_Ranged>(); // 참조 가져오기
    }

    private void FixedUpdate()
    {
        // 공격 중이 아닐 때만 이동
        if (enemyRanged == null || !enemyRanged.IsAttacking)
            Move();
        else
        {
            // 공격 중일 때는 멈춤
            if (rigid != null)
            {
                rigid.velocity = Vector3.zero;
            }
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
}