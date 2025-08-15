// Unity
using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    public class PlayerMove : MonoBehaviour
    {
        [SerializeField] private float speed;

        private Player player;
        private Rigidbody rigid;
        private Vector3 inputVector;
        private Animator animator;

        private void Start()
        {
            player = GetComponent<Player>();
            speed = player.GetMoveSpeed();
            rigid = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
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
            Vector3 moveVector = inputVector.normalized * player.GetMoveSpeed() * Time.fixedDeltaTime;

            if (moveVector != Vector3.zero) animator.SetBool("IsWalk", true);
            else animator.SetBool("IsWalk", false);

            rigid.MovePosition(rigid.position + moveVector);
        }
    }
}

