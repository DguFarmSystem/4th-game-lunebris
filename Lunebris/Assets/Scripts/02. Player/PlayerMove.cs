using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    public class PlayerMove : MonoBehaviour
    {
        [SerializeField] private float speed;

        private Rigidbody rigid;
        private Vector3 inputVector;

        private void Start()
        {
            rigid = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            inputVector.x = Input.GetAxis("Horizontal");
            inputVector.z = Input.GetAxis("Vertical");
        }

        private void FixedUpdate()
        {
            Vector3 moveVector = inputVector.normalized * speed * Time.fixedDeltaTime;
            rigid.MovePosition(rigid.position + moveVector);
        }
    }
}

