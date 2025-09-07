using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallCollision : MonoBehaviour
{

    [Tooltip("Capa que usan los MUROS (no puertas)")]
    public LayerMask wallMask;                       // Asigna aquí la capa 'Wall'
    [Tooltip("Margen para el volumen de solape (m)")]
    public float checkEpsilon = 0.001f;

    void Start()
    {
        var myCol = GetComponent<Collider>();
        if (myCol == null) return;

        // ✅ Sólo muros: debe tener tag 'Wall' y estar en la capa wallMask
        if (!CompareTag("Wall")) return;
        if ((wallMask.value & (1 << gameObject.layer)) == 0) return;

        // Volumen AABB en mundo (ignora triggers para no pisar puertas abiertas)
        Vector3 center = myCol.bounds.center;
        Vector3 half = myCol.bounds.extents + Vector3.one * checkEpsilon;

        var hits = Physics.OverlapBox(
            center, half, Quaternion.identity,
            wallMask, QueryTriggerInteraction.Ignore   // <- ignora triggers
        );

        bool keepMine = true;
        int myId = myCol.GetInstanceID();

        foreach (var h in hits)
        {
            if (h == myCol) continue;
            // Garantiza que el otro sea MURO (tag y capa)
            if (!h.CompareTag("Wall")) continue;
            if ((wallMask.value & (1 << h.gameObject.layer)) == 0) continue;

            // Determinismo: el de menor InstanceID "gana"
            if (h.GetInstanceID() < myId)
            {
                keepMine = false;
                break;
            }
        }

        if (keepMine)
        {
            SetRenderers(true);
            myCol.enabled = true;
            myCol.isTrigger = false; // muro sólido
        }
        else
        {
            SetRenderers(false);
            myCol.enabled = false;   // apagado para evitar doble pared y z-fight
            myCol.isTrigger = false;
        }
    }

    void SetRenderers(bool on)
    {
        var rends = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rends.Length; i++) rends[i].enabled = on;
    }
}