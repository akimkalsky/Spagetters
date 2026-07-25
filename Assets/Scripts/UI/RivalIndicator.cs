using TMPro;
using UnityEngine;

public class RivalIndicator : MonoBehaviour
{
    Canvas canvas;
    RectTransform canvasRT;
    TMP_Text marker;
    Transform player;
    Goon nearest;
    Camera cam;
    float refreshTimer;

    const float Margin = 70f;

    void Start() => Build();

    void Build()
    {
        canvas = UIFactory.CreateOverlayCanvas("RivalIndicatorCanvas", 48, transform);
        canvasRT = (RectTransform)canvas.transform;
        marker = UIFactory.Label(canvas.transform, "»", 60, UIFactory.Rust, Vector2.zero);
        var rt = marker.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        marker.gameObject.SetActive(false);
    }

    void Update()
    {
        bool explore = GameFlow.Instance != null && GameFlow.Instance.State == GameState.Explore;
        if (cam == null)
        {
            cam = Camera.main;
        }
        if (!explore || cam == null)
        {
            Hide();
            return;
        }

        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f || nearest == null || !nearest.isActiveAndEnabled)
        {
            refreshTimer = 0.25f;
            nearest = FindNearest(cam);
        }
        if (nearest == null)
        {
            Hide();
            return;
        }

        if (!marker.gameObject.activeSelf)
        {
            marker.gameObject.SetActive(true);
        }
        Place(cam, nearest.transform.position + Vector3.up * 1.2f);
    }

    void Hide()
    {
        if (marker != null && marker.gameObject.activeSelf)
        {
            marker.gameObject.SetActive(false);
        }
    }

    Goon FindNearest(Camera cam)
    {
        if (player == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                player = pc.transform;
            }
        }
        Vector3 from = player != null ? player.position : cam.transform.position;

        Goon best = null;
        float bestSq = float.MaxValue;
        foreach (var g in FindObjectsByType<Goon>(FindObjectsSortMode.None))
        {
            float d = (g.transform.position - from).sqrMagnitude;
            if (d < bestSq)
            {
                bestSq = d;
                best = g;
            }
        }
        return best;
    }

    void Place(Camera cam, Vector3 world)
    {
        Vector3 sp = cam.WorldToScreenPoint(world);
        bool behind = sp.z < 0f;
        if (behind)
        {
            sp.x = Screen.width - sp.x;
            sp.y = Screen.height - sp.y;
        }
        bool onScreen = !behind && sp.x > Margin && sp.x < Screen.width - Margin && sp.y > Margin && sp.y < Screen.height - Margin;

        Vector2 screenPoint;
        float angle = 0f;
        if (onScreen)
        {
            angle = -90f;
            screenPoint = new Vector2(sp.x, sp.y + 44f);
        }
        else
        {
            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 dir = new Vector2(sp.x, sp.y) - center;
            if (dir.sqrMagnitude < 0.001f)
            {
                dir = Vector2.up;
            }
            dir.Normalize();
            screenPoint = new Vector2(Mathf.Clamp(sp.x, Margin, Screen.width - Margin), Mathf.Clamp(sp.y, Margin, Screen.height - Margin));
            angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenPoint, null, out Vector2 local);
        marker.rectTransform.anchoredPosition = local;
        marker.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
