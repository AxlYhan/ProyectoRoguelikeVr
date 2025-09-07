using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomUIManager : MonoBehaviour
{
    public static RoomUIManager Instance;

    public Text label;
    public CanvasGroup group;
    public float fadeTime = 0.25f;
    public float holdTime = 1.2f;

    Coroutine current;

    void Awake()
    {
        Instance = this;
        if (group) group.alpha = 0f; // empieza oculto
    }

    public static void Show(string text)
    {
        if (Instance != null) Instance.ShowInternal(text);
        else Debug.Log($"[RoomUI] {text} (no hay UIManager en escena)");
    }

    void ShowInternal(string text)
    {
        if (!label || !group) { Debug.Log($"[RoomUI] {text} (UI no asignada)"); return; }
        label.text = text;
        if (current != null) StopCoroutine(current);
        current = StartCoroutine(FadeRoutine());
    }

    IEnumerator FadeRoutine()
    {
        yield return FadeTo(1f, fadeTime);
        yield return new WaitForSeconds(holdTime);
        yield return FadeTo(0f, fadeTime);
    }

    IEnumerator FadeTo(float target, float t)
    {
        float start = group.alpha, time = 0f;
        while (time < t)
        {
            time += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, target, time / t);
            yield return null;
        }
        group.alpha = target;
    }
    public static class PlayerRoomState
    {
        public static int LastRoomId = -1;

        // ⬇️ NUEVO: si es true, RoomArea ignorará el próximo anuncio de "Cuarto ..."
        public static bool suppressNextRoomBanner = false;
    }
}
