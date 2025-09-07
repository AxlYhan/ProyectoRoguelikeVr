using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RoomUIManager;

public class RoomArea : MonoBehaviour
{
    public RoomBehaviour room;
    public bool logEntry = true;   // para ver en consola cuando entra el player

    void Reset()
    {
        var box = GetComponent<BoxCollider>();
        box.isTrigger = true;                    // DEBE ser trigger
        if (room == null) room = GetComponentInParent<RoomBehaviour>();
    }

    void Awake()
    {
        var box = GetComponent<BoxCollider>();
        box.isTrigger = true;                    // DEBE ser trigger
        if (room == null) room = GetComponentInParent<RoomBehaviour>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (room == null) return;

        // ⬅️ NUEVO: ignorar el primer trigger del primer cuarto del nivel
        if (PlayerRoomState.suppressNextRoomBanner)
        {
            PlayerRoomState.suppressNextRoomBanner = false;
            return;
        }

        if (PlayerRoomState.LastRoomId != room.roomId)
        {
            PlayerRoomState.LastRoomId = room.roomId;
            RoomUIManager.Show($"Cuarto {room.roomId}");
            if (logEntry) Debug.Log($"[RoomArea] Entraste al Cuarto {room.roomId} ({room.name})");
        }
    }
    // Ayuda visual en Scene View para ver el volumen del trigger
    void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider>();
        if (!col) return;

        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        if (col is BoxCollider b)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(b.center, b.size);
        }
        else
        {
            Gizmos.DrawWireSphere(col.bounds.center, Mathf.Max(col.bounds.extents.x, col.bounds.extents.z));
        }
    }
}
