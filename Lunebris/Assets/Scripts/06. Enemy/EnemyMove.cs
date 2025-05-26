// Unity
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyMove : MonoBehaviour
{
    [SerializeField] private float speed;

    private Transform target;
    private Rigidbody rigid;

    private void Start()
    {
        target = GameObject.Find("Player").transform;
        rigid = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void Move()
    {
        Vector3 dirVector = target.position - transform.position;
        Vector3 moveVector = dirVector.normalized * speed * Time.fixedDeltaTime;
        rigid.MovePosition(rigid.position + moveVector);
    }

}
