using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true; // portal debe ser trigger
    }

    void Awake()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var gen = FindObjectOfType<DungeonGenerator>();
        if (gen != null) gen.OnPortalEntered();
        else Debug.LogError("[Portal] No se encontró DungeonGenerator en escena.");
    }
}
