using UnityEngine;

[ExecuteAlways]
public class ColorTint : MonoBehaviour
{
    public Color color = Color.white;

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int LegacyColor = Shader.PropertyToID("_Color");

    void OnEnable() => Apply();
    void OnValidate() => Apply();

    void Apply()
    {
        var r = GetComponent<Renderer>();
        if (r == null)
        {
            return;
        }
        var mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColor, color);
        mpb.SetColor(LegacyColor, color);
        r.SetPropertyBlock(mpb);
    }
}
