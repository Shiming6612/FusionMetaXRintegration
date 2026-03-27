using TMPro;
using UnityEngine;

public class LegendUIController : MonoBehaviour
{
    [Header("Sources")]
    public DropSelectionManager selectionManager;
    public VoltageKnobInput voltageSource;
    public ElectricFieldVolume fieldVolume;

    [Header("UI")]
    public CanvasGroup panelGroup;
    public TMP_Text titleText, massText, chargeText, voltageText, hintText;

    [Header("Task Feedback")]
    public float toleranceKV = 0.05f;
    public Color correctColor = Color.green;
    public float correctFontSizeMultiplier = 1.25f;
    public AudioSource correctSfxSource;
    public AudioClip correctSfx;

    bool _wasCorrect;
    Color _baseColor;
    float _baseSize;
    FontStyles _baseStyle;
    bool _cached;

    void OnEnable()
    {
        if (selectionManager) selectionManager.OnSelectionChanged += _ => RefreshAll();
        CacheBaseStyle();
        RefreshAll();
    }

    void OnDisable()
    {
        if (selectionManager) selectionManager.OnSelectionChanged -= _ => RefreshAll();
    }

    void Update()
    {
        var sel = selectionManager ? selectionManager.CurrentSelected : null;
        if (!sel) return;
        RefreshVoltage(sel);
    }

    void RefreshAll()
    {
        var sel = selectionManager ? selectionManager.CurrentSelected : null;

        if (!sel)
        {
            SetPanel(false);
            if (titleText) titleText.text = "";
            if (massText) massText.text = "";
            if (chargeText) chargeText.text = "";
            if (voltageText) voltageText.text = "";
            if (hintText) hintText.text = "";
            _wasCorrect = false;
            RestoreStyle();
            return;
        }

        SetPanel(true);
        if (titleText) titleText.text = sel.dropId >= 0 ? $"Drop {sel.dropId}" : "Drop";

        var dp = sel.GetComponent<DropProperties>();
        if (dp)
        {
            if (massText) massText.text = $"Mass: {dp.MassKg * 1000f:0.00} g";
            if (chargeText) chargeText.text = $"Charge: {dp.ChargeC * 1e12f:0.000} pC";
        }
        else
        {
            if (massText) massText.text = "Mass: --";
            if (chargeText) chargeText.text = "Charge: --";
        }

        _wasCorrect = false;
        RestoreStyle();
        RefreshVoltage(sel);
    }

    void RefreshVoltage(SelectableDrop sel)
    {
        CacheBaseStyle();

        float kv = voltageSource ? voltageSource.CurrentKV : 0f;
        if (fieldVolume && fieldVolume.invertVoltage) kv = -kv;

        if (voltageText) voltageText.text = voltageSource ? $"kV: {kv:0.00}" : "kV: --";

        bool can = TryHoverKV(sel, out float hoverKV);
        bool correct = can && voltageSource && Mathf.Abs(Mathf.Abs(kv) - hoverKV) <= toleranceKV;

        if (correct) ApplyCorrectStyle(); else RestoreStyle();

        if (correct && !_wasCorrect && correctSfxSource && correctSfx)
            correctSfxSource.PlayOneShot(correctSfx);

        _wasCorrect = correct;

        if (hintText)
        {
            if (!can || !voltageSource) hintText.text = "";
            else if (Mathf.Abs(kv) > hoverKV + toleranceKV) hintText.text = "State: Rise";
            else if (Mathf.Abs(kv) < hoverKV - toleranceKV) hintText.text = "State: Fall";
            else hintText.text = "State: Hover";
        }
    }

    bool TryHoverKV(SelectableDrop sel, out float hoverKV)
    {
        hoverKV = 0f;
        if (!fieldVolume) return false;

        var dp = sel.GetComponent<DropProperties>();
        if (!dp) return false;

        float m = Mathf.Max(1e-6f, dp.MassKg);
        float qPC = dp.ChargeC * 1e12f;
        if (Mathf.Abs(qPC) < 1e-6f) return false;

        float qOverM = qPC / m;
        float refQOverM = fieldVolume.referenceChargePC / Mathf.Max(1e-6f, fieldVolume.referenceMassKg);
        if (Mathf.Abs(qOverM) < 1e-6f || refQOverM <= 1e-6f) return false;

        hoverKV = Mathf.Abs(fieldVolume.hoverCenterKV * (refQOverM / qOverM));
        return true;
    }

    void SetPanel(bool on)
    {
        if (!panelGroup) return;
        panelGroup.alpha = on ? 1f : 0f;
        panelGroup.interactable = on;
        panelGroup.blocksRaycasts = on;
    }

    void CacheBaseStyle()
    {
        if (_cached || !voltageText) return;
        _baseColor = voltageText.color;
        _baseSize = voltageText.fontSize;
        _baseStyle = voltageText.fontStyle;
        _cached = true;
    }

    void ApplyCorrectStyle()
    {
        if (!voltageText) return;
        voltageText.color = correctColor;
        voltageText.fontStyle = _baseStyle | FontStyles.Bold;
        voltageText.fontSize = _baseSize * Mathf.Max(1f, correctFontSizeMultiplier);
    }

    void RestoreStyle()
    {
        if (!voltageText || !_cached) return;
        voltageText.color = _baseColor;
        voltageText.fontStyle = _baseStyle;
        voltageText.fontSize = _baseSize;
    }
}
