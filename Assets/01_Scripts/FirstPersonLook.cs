using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonLook : MonoBehaviour
{
    public Transform playerBody;        // asignado por DungeonGenerator
    public float mouseSensitivity = 300f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    private float xRotation = 0f;       // pitch acumulado

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // Opcional: cursor centrado y oculto
        Cursor.visible = false;
    }

    void Update()
    {
        if (playerBody == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minPitch, maxPitch);

        // Pitch en la cámara (vertical)
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        // Yaw en el cuerpo del player (horizontal)
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
