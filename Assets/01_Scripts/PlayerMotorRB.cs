using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMotorRB : MonoBehaviour
{
    [Header("Movimiento")]
    public float walkSpeed = 4.5f;
    public float sprintSpeed = 7.5f;

    [Header("Salto")]
    public float jumpForce = 5f;
    public LayerMask groundMask;
    public Transform groundCheck;           // opcional; si es null usa el centro
    public float groundCheckDistance = 0.15f;
    public float groundProbeOffsetY = 0.1f; // para iniciar el raycast apenas sobre el suelo

    [Header("Agarre al suelo")]
    public float groundedDrag = 2f;

    private Rigidbody rb;
    private bool grounded;
    private bool jumpRequested;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // y en Inspector: Freeze X y Z
    }

    void Update()
    {
        // Capturar el salto en Update (no perder el input)
        if (Input.GetButtonDown("Jump"))
            jumpRequested = true;
    }

    void FixedUpdate()
    {
        // Comprobar suelo
        Vector3 origin = (groundCheck ? groundCheck.position : transform.position) + Vector3.up * groundProbeOffsetY;
        grounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance, groundMask);

        // Dirección relativa al yaw del player
        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

        float ix = Input.GetAxisRaw("Horizontal");
        float iz = Input.GetAxisRaw("Vertical");
        Vector3 dir = (fwd * iz + right * ix).normalized;

        bool sprint = Input.GetKey(KeyCode.LeftShift) && iz > 0.1f;
        float targetSpeed = sprint ? sprintSpeed : walkSpeed;

        // Ajustar velocidad horizontal directamente (tipo "quebrar" fricción)
        Vector3 vel = rb.velocity;
        Vector3 targetVel = dir * targetSpeed;
        Vector3 velChange = (targetVel - new Vector3(vel.x, 0f, vel.z));
        rb.AddForce(velChange, ForceMode.VelocityChange);

        // Salto
        if (grounded && jumpRequested)
        {
            jumpRequested = false;
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z); // reset vertical
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
        else
        {
            jumpRequested = false;
        }

        // Arrastre en suelo para frenar más rápido
        rb.drag = grounded ? groundedDrag : 0f;
    }

    void OnDrawGizmosSelected()
    {
        // Visual del ground check
        Vector3 origin = (groundCheck ? groundCheck.position : transform.position) + Vector3.up * groundProbeOffsetY;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + Vector3.down * groundCheckDistance);
    }
}
