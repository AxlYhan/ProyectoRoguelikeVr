using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorTrigger : MonoBehaviour
{
    public RoomBehaviour room;     // arrastra aquí el RoomBehaviour del cuarto
    [Tooltip("0=Up, 1=Down, 2=Right, 3=Left")]
    public int sideIndex = 0;
    public bool closeOnExit = false; // si quieres que se cierre al salir

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true; // este sensor debe ser trigger
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (room) room.SetSideOpen(sideIndex, true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!closeOnExit) return;
        if (!other.CompareTag("Player")) return;
        if (room) room.SetSideOpen(sideIndex, false);
    }
}
