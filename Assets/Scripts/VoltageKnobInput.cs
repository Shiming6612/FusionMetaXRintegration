using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VoltageKnobInput : MonoBehaviour
{
    [Header("UI")]
    public Slider voltageSlider;
    public TMP_Text voltageText; // optional

    [Header("Hands / Controllers")]
    public Transform leftControllerTransform;
    public Transform rightControllerTransform;

    [Header("Voltage Range (kV)")]
    public float minKV = 0f;
    public float maxKV = 10f;
    public float startKV = 0f;

    [Header("Mapping")]
    public Vector3 knobLocalAxis = new Vector3(0, 0, 1);          // knob axis (local)
    public Vector3 controllerLocalRefAxis = new Vector3(0, 1, 0); // TRY (0,1,0) or (0,0,1)
    public float degreesForFullRange = 180f;                      // bigger = less sensitive
    public bool invertDirection = false;

    [Header("Stability")]
    public float deadzoneDegrees = 2f;     // ignore tiny jitter
    public float smoothing = 12f;          // higher = snappier, lower = smoother

    [Header("Grab Gating")]
    public float maxGrabDistance = 0.06f;  // smaller = must be closer

    public float CurrentKV { get; private set; }

    private bool grabbed;
    private Transform activeController;
    private float grabStartKV;

    private Vector3 axisWorld;

    // --- Anti-wrap state (prevents jumping to the other extreme) ---
    private Vector3 _startRefWorld;
    private float _prevRawDeg;
    private float _accumDeg;

    void Start()
    {
        SetKV(startKV, true);
        grabbed = false;
        activeController = null;

        _startRefWorld = Vector3.forward;
        _prevRawDeg = 0f;
        _accumDeg = 0f;
    }

    void Update()
    {
        if (!grabbed || activeController == null) return;

        axisWorld = transform.TransformDirection(knobLocalAxis.normalized);

        // Current controller reference direction in world space
        Vector3 refWorld = activeController.TransformDirection(controllerLocalRefAxis.normalized);

        // Project onto knob plane (stable twist measurement)
        Vector3 a = ProjectOnPlaneSafe(_startRefWorld, axisWorld);
        Vector3 b = ProjectOnPlaneSafe(refWorld, axisWorld);

        // Raw angle is [-180, 180] and will wrap; integrate using DeltaAngle
        float rawDeg = Vector3.SignedAngle(a, b, axisWorld);

        // deadzone on the per-frame delta (less jitter)
        float delta = Mathf.DeltaAngle(_prevRawDeg, rawDeg);
        _prevRawDeg = rawDeg;

        if (Mathf.Abs(delta) < deadzoneDegrees)
            delta = 0f;

        _accumDeg += delta;

        // Apply invert
        float usedDeg = invertDirection ? -_accumDeg : _accumDeg;

        // Map degrees -> kV
        float range = (maxKV - minKV);
        float denom = Mathf.Max(1e-3f, degreesForFullRange);
        float targetKV = grabStartKV + (usedDeg / denom) * range;

        // Clamp AND clamp accumulated degrees accordingly (prevents "bounce" / wrap-to-other-end)
        float clampedKV = Mathf.Clamp(targetKV, minKV, maxKV);
        if (!Mathf.Approximately(clampedKV, targetKV))
        {
            targetKV = clampedKV;

            // Back-calc usedDeg that matches the clamped voltage
            float t = (targetKV - grabStartKV) / Mathf.Max(1e-6f, range); // can be negative
            usedDeg = t * denom;

            // Convert back to _accumDeg considering invertDirection
            _accumDeg = invertDirection ? -usedDeg : usedDeg;
        }

        // smoothing (exponential)
        float alpha = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        float smoothKV = Mathf.Lerp(CurrentKV, targetKV, alpha);

        SetKV(smoothKV);
    }

    // Hook from Interactable Unity Event Wrapper -> When Select()
    public void BeginGrab()
    {
        Transform chosen = ChooseNearestHand();
        if (chosen == null) return;

        activeController = chosen;
        axisWorld = transform.TransformDirection(knobLocalAxis.normalized);

        // Cache controller ref at grab start (world)
        _startRefWorld = activeController.TransformDirection(controllerLocalRefAxis.normalized);

        // Reset integration state
        _prevRawDeg = 0f;
        _accumDeg = 0f;

        grabStartKV = CurrentKV;
        grabbed = true;
    }

    // Hook from Interactable Unity Event Wrapper -> When Unselect()
    public void EndGrab()
    {
        grabbed = false;
        activeController = null;
    }

    private Transform ChooseNearestHand()
    {
        Transform best = null;
        float bestDist = float.MaxValue;

        if (leftControllerTransform != null)
        {
            float d = Vector3.Distance(leftControllerTransform.position, transform.position);
            if (d < bestDist) { bestDist = d; best = leftControllerTransform; }
        }

        if (rightControllerTransform != null)
        {
            float d = Vector3.Distance(rightControllerTransform.position, transform.position);
            if (d < bestDist) { bestDist = d; best = rightControllerTransform; }
        }

        if (best == null || bestDist > maxGrabDistance) return null;
        return best;
    }

    private void SetKV(float kv, bool forceUI = false)
    {
        float clamped = Mathf.Clamp(kv, minKV, maxKV);

        if (!forceUI && Mathf.Approximately(clamped, CurrentKV))
            return;

        CurrentKV = clamped;

        if (voltageSlider != null)
        {
            voltageSlider.minValue = minKV;
            voltageSlider.maxValue = maxKV;
            voltageSlider.value = CurrentKV;
        }

        if (voltageText != null)
        {
            voltageText.text = $"{CurrentKV:0.00} kV";
        }
    }

    private static Vector3 ProjectOnPlaneSafe(Vector3 v, Vector3 planeNormal)
    {
        Vector3 p = Vector3.ProjectOnPlane(v, planeNormal);
        if (p.sqrMagnitude < 1e-6f)
        {
            Vector3 any = Vector3.Cross(planeNormal, Vector3.up);
            if (any.sqrMagnitude < 1e-6f) any = Vector3.Cross(planeNormal, Vector3.right);
            return any.normalized;
        }
        return p.normalized;
    }
}
