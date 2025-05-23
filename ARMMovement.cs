using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARMMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float turnSpeed = 100f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float move = Input.GetAxis("Vertical");   // W/S 또는 ↑/↓
        float turn = Input.GetAxis("Horizontal"); // A/D 또는 ←/→

        // 전진/후진 이동
        Vector3 movement = transform.forward * move * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);

        // 제자리 회전
        Quaternion turnRotation = Quaternion.Euler(0f, turn * turnSpeed * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);
    }
}
