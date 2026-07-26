using TMPro;
using UnityEngine;

public class NpcNameTag : MonoBehaviour
{
    public float margin = 0.35f;
    public float size = 48f;
    public float worldScale = 0.01f;
    public float fallbackHeight = 1.3f;

    TMP_Text text;
    Transform tagTf;
    Camera cam;
    float topOffset;
    string baseLabel = "";

    public void Init(string label)
    {
        if (tagTf == null)
        {
            Build();
        }
        baseLabel = label;
        text.text = label;
    }

    public void SetBoss(bool boss)
    {
        if (text == null)
        {
            return;
        }
        text.text = boss ? "† " + baseLabel : baseLabel;
        text.color = boss ? new Color(0.92f, 0.26f, 0.2f) : UIFactory.Parchment;
    }

    void Build()
    {
        topOffset = (TopY() - transform.position.y) + margin;

        var go = new GameObject("NameTag");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var crt = (RectTransform)go.transform;
        crt.sizeDelta = new Vector2(340f, 90f);
        crt.localScale = Vector3.one * worldScale;

        text = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(go.transform, false);
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = UIFactory.Parchment;
        text.rectTransform.sizeDelta = new Vector2(340f, 90f);

        tagTf = go.transform;
        cam = Camera.main;
    }

    float TopY()
    {
        var rends = GetComponentsInChildren<Renderer>();
        if (rends.Length == 0)
        {
            return transform.position.y + fallbackHeight;
        }
        var b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
        {
            b.Encapsulate(rends[i].bounds);
        }
        return b.max.y;
    }

    void LateUpdate()
    {
        if (tagTf == null)
        {
            return;
        }
        bool show = GameFlow.Instance == null || GameFlow.Instance.State == GameState.Explore;
        if (tagTf.gameObject.activeSelf != show)
        {
            tagTf.gameObject.SetActive(show);
        }
        if (!show)
        {
            return;
        }
        tagTf.position = transform.position + Vector3.up * topOffset;
        if (cam == null)
        {
            cam = Camera.main;
        }
        if (cam != null)
        {
            Vector3 dir = tagTf.position - cam.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                tagTf.forward = dir.normalized;
            }
        }
    }

    void OnEnable()
    {
        if (tagTf != null)
        {
            tagTf.gameObject.SetActive(true);
        }
    }

    void OnDisable()
    {
        if (tagTf != null)
        {
            tagTf.gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (tagTf != null)
        {
            Destroy(tagTf.gameObject);
        }
    }
}
