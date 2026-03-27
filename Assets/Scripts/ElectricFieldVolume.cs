using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ElectricFieldVolume : MonoBehaviour
{
    [Header("Voltage Source")]
    public VoltageKnobInput voltageSource;
    public bool invertVoltage = false;

    [Header("Field Geometry (world)")]
    public Vector3 fieldDirection = new Vector3(0, 1, 0);

    [Header("Hover / Calibration")]
    [Tooltip("At this kV, a 'reference' droplet will roughly hover (net accel ~ 0 along field).")]
    public float hoverCenterKV = 5f;

    [Tooltip("Reference droplet charge in pC (simulation units). Use mid of your DropProperties range.")]
    public float referenceChargePC = 0.16f;

    [Tooltip("Reference droplet mass in kg (Unity). Use mid of your DropProperties range.")]
    public float referenceMassKg = 0.02f;

    [Header("Controls")]
    [Tooltip("Smooth input voltage so tiny knob jitter doesn't cause instant force jumps.")]
    public float voltageSmoothing = 12f;

    [Tooltip("Within this +-kV around hoverCenterKV, treat as exactly hoverCenterKV.")]
    public float deadbandKV = 0.05f;

    [Tooltip("Clamp effective voltage to hoverCenterKV +- this range for observation. Set 0 to disable.")]
    public float clampRangeAroundCenterKV = 0f;

    [Header("Safety (optional)")]
    [Tooltip("Acceleration clamp to avoid extreme spikes. Set high if you want more physical freedom.")]
    public float maxAccel = 200f;

    [Header("Maintenance")]
    [Tooltip("Every N FixedUpdate ticks, remove destroyed Rigidbody references from the internal set. 0 = disable.")]
    public int nullCleanupIntervalFrames = 30;

    [Header("Debug")]
    public bool logEnterExit = false;

    private readonly HashSet<Rigidbody> _bodies = new HashSet<Rigidbody>();
    private float _kvSmooth = 0f;
    private int _cleanupCounter = 0;

    void Awake()
    {
        var trigger = GetComponent<BoxCollider>();
        trigger.isTrigger = true;

        // Start from knob value (default should be 0 kV), not hoverCenterKV.
        float kv = (voltageSource != null) ? voltageSource.CurrentKV : 0f;
        _kvSmooth = invertVoltage ? -kv : kv;
    }

    void OnTriggerEnter(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null) return;

        _bodies.Add(rb);

        if (logEnterExit)
            Debug.Log($"[ElectricFieldVolume] Enter: {rb.name}", this);
    }

    void OnTriggerExit(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null) return;

        _bodies.Remove(rb);

        if (logEnterExit)
            Debug.Log($"[ElectricFieldVolume] Exit: {rb.name}", this);
    }

    void FixedUpdate()
    {
        // periodic cleanup for destroyed/disabled drops inside volume
        if (nullCleanupIntervalFrames > 0)
        {
            _cleanupCounter++;
            if (_cleanupCounter >= nullCleanupIntervalFrames)
            {
                _cleanupCounter = 0;
                _bodies.RemoveWhere(rb => rb == null);
            }
        }

        if (_bodies.Count == 0) return;

        float kvRaw = (voltageSource != null) ? voltageSource.CurrentKV : 0f;
        if (invertVoltage) kvRaw = -kvRaw;

        // smooth
        float alpha = 1f - Mathf.Exp(-Mathf.Max(0.01f, voltageSmoothing) * Time.fixedDeltaTime);
        _kvSmooth = Mathf.Lerp(_kvSmooth, kvRaw, alpha);

        float kv = _kvSmooth;

        // deadband near hover center (prevents twitch near hover)
        if (Mathf.Abs(kv - hoverCenterKV) < deadbandKV)
            kv = hoverCenterKV;

        // optional clamp around center
        if (clampRangeAroundCenterKV > 0f)
            kv = Mathf.Clamp(kv, hoverCenterKV - clampRangeAroundCenterKV, hoverCenterKV + clampRangeAroundCenterKV);

        Vector3 dir = (fieldDirection.sqrMagnitude > 1e-6f) ? fieldDirection.normalized : Vector3.up;

        float kv0 = Mathf.Max(0.001f, hoverCenterKV);
        float refQOverM = referenceChargePC / Mathf.Max(1e-6f, referenceMassKg); // pC/kg

        foreach (var rb in _bodies)
        {
            if (rb == null) continue;

            float m = Mathf.Max(1e-6f, rb.mass);

            // droplet charge in pC
            float qPC = 0f;
            var dp = rb.GetComponent<DropProperties>();
            if (dp != null) qPC = dp.ChargeC * 1e12f;
            if (Mathf.Abs(qPC) < 1e-6f) continue;

            float qOverM = qPC / m;

            // gravity along field (prefer OilDrop.customGravity)
            Vector3 g = Vector3.zero;
            var od = rb.GetComponent<OilDrop>();
            if (od != null) g = od.customGravity;
            else if (rb.useGravity) g = Physics.gravity;

            float gAlong = Vector3.Dot(g, dir);

            // calibration: at kv0, reference droplet cancels gravity along field
            float k = (-gAlong) / (kv0 * Mathf.Max(1e-6f, refQOverM));

            float a = Mathf.Clamp(k * kv * qOverM, -maxAccel, maxAccel);

            rb.AddForce(dir * a, ForceMode.Acceleration);
        }
    }
}
