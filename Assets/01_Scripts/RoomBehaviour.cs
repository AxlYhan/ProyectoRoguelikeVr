using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomBehaviour : MonoBehaviour
{
    [Header("Paredes y Puertas (0=Up, 1=Down, 2=Right, 3=Left)")]
    public GameObject[] walls;
    public GameObject[] doors;

    [Header("Spawn del jugador")]
    public Transform spawnPoint;

    [Header("Room Meta")]
    public int roomId;
    public bool isFinal;

    [Header("Portal (opcional)")]
    public GameObject portal; // si usas portal hijo; si no, dejalo vacío

    /// <summary>
    /// status[i] = true => hay conexión: puerta abierta (no bloquea) y pared oculta.
    /// </summary>
    public void UpdateRoom(bool[] status)
    {
        if (status == null) return;

        for (int i = 0; i < 4; i++)
        {
            bool open = i < status.Length && status[i];

            // Puerta visible si está abierta
            if (doors != null && i < doors.Length && doors[i] != null)
            {
                doors[i].SetActive(open);

                // 🔧 Puerta ABIERTA: collider en modo TRIGGER (no bloquea)
                // 🔧 Puerta CERRADA: la puerta está oculta, así que desactivamos sus colliders por si acaso
                var cols = doors[i].GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < cols.Length; c++)
                {
                    if (open)
                    {
                        cols[c].enabled = true;
                        cols[c].isTrigger = true;   // no bloquea
                    }
                    else
                    {
                        cols[c].enabled = false;    // puerta oculta => sin collider
                        cols[c].isTrigger = false;
                    }
                }
            }

            // Pared visible si NO hay conexión
            if (walls != null && i < walls.Length && walls[i] != null)
            {
                walls[i].SetActive(!open);
                // La pared (cerrada) es la que bloquea. Si tiene colliders, se activan con SetActive(true).
            }
        }
    }

    /// <summary>
    /// Abre/cierra un lado puntual (0=Up,1=Down,2=Right,3=Left) en runtime.
    /// </summary>
    public void SetSideOpen(int sideIndex, bool open)
    {
        if (sideIndex < 0 || sideIndex > 3) return;

        // Puerta
        if (doors != null && sideIndex < doors.Length && doors[sideIndex] != null)
        {
            doors[sideIndex].SetActive(open);

            var cols = doors[sideIndex].GetComponentsInChildren<Collider>(true);
            for (int c = 0; c < cols.Length; c++)
            {
                if (open)
                {
                    cols[c].enabled = true;
                    cols[c].isTrigger = true;  // no bloquea
                }
                else
                {
                    cols[c].enabled = false;
                    cols[c].isTrigger = false;
                }
            }
        }

        // Pared
        if (walls != null && sideIndex < walls.Length && walls[sideIndex] != null)
        {
            walls[sideIndex].SetActive(!open);
        }
    }

    /// <summary>
    /// Marca este cuarto como final y activa/desactiva su portal hijo (si lo usas).
    /// </summary>
    public void SetFinal(bool value)
    {
        isFinal = value;
        if (portal != null) portal.SetActive(value);
    }
}