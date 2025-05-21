// Unity
using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    public class PlayerMove : MonoBehaviour
    {
        [SerializeField] private Player player;
        [SerializeField] private float speed;

        private Rigidbody rigid;
        private Vector3 inputVector;

        private void Start()
        {
            player = GetComponent<Player>();
            speed = player.GetPlayerStat().Get(StatType.MoveSpeed);
            rigid = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            MoveHandler();
        }

        private void FixedUpdate()
        {
            Move();
        }

        private void MoveHandler()
        {
            inputVector.x = Input.GetAxis("Horizontal");
            inputVector.z = Input.GetAxis("Vertical");
        }

        private void Move()
        {
            Vector3 moveVector = inputVector.normalized * speed * Time.fixedDeltaTime;
            rigid.MovePosition(rigid.position + moveVector);
        }
    }
}

