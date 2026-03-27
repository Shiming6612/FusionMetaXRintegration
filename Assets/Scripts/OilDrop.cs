using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class OilDrop : MonoBehaviour
{
    [Header("Gravity (simulation)")]
    [Tooltip("Bigger magnitude = faster fall (scene scale).")]
    public Vector3 customGravity = new Vector3(0f, -25f, 0f);

    [Header("Air Physics (for realism)")]
    public bool useBuoyancy = true;
    public bool useStokesDrag = true;

    [Tooltip("Oil density (kg/m^3). Typical ~ 800-900.")]
    public float oilDensity = 860f;

    [Tooltip("Air density (kg/m^3). Typical ~ 1.2.")]
    public float airDensity = 1.2f;

    [Tooltip("Air dynamic viscosity (Pa*s). Typical ~ 1.81e-5.")]
    public float airViscosity = 1.81e-5f;

    [Tooltip("Multiply drag for scene-tuning if needed (keep 1 for physical).")]
    public float dragScale = 1f;

    [Header("Other")]
    public bool destroyOnCollision = false;

    private Rigidbody _rb;
    private Vector3 _startPosition;
    private bool _active = false;

    private float _radiusM = 0.0005f; // computed from mass + oilDensity
    private float _lastMass = -1f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        _rb.useGravity = false;
        _rb.isKinematic = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        // IMPORTANT: do not use built-in damping if we do physical drag
        _rb.linearDamping = 0f;

        var col = GetComponent<Collider>();
        col.isTrigger = false;

        gameObject.SetActive(false);
    }

    public void Launch(Vector3 worldPos, Vector3 initialVelocity)
    {
        _startPosition = worldPos;
        transform.position = worldPos;

        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.linearVelocity = initialVelocity;

        _active = true;

        // ensure radius computed with current mass
        RecomputeRadiusIfNeeded(force: true);

        gameObject.SetActive(true);
    }

    public void ResetDrop()
    {
        _active = false;

        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        transform.position = _startPosition;
        gameObject.SetActive(false);
    }

    void FixedUpdate()
    {
        if (!_active) return;

        RecomputeRadiusIfNeeded(force: false);

        // gravity (+ buoyancy scaling)
        Vector3 gEff = customGravity;
        if (useBuoyancy && oilDensity > 1e-6f)
        {
            // buoyancy reduces effective gravity by rho_air/rho_oil
            float factor = 1f - Mathf.Clamp01(airDensity / oilDensity);
            gEff *= factor;
        }
        _rb.AddForce(gEff, ForceMode.Acceleration);

        // Stokes drag: Fd = 6*pi*eta*r*v  => a = (Fd/m) opposite to v
        if (useStokesDrag)
        {
            Vector3 v = _rb.linearVelocity;
            if (v.sqrMagnitude > 1e-12f)
            {
                float m = Mathf.Max(1e-6f, _rb.mass);
                float coeff = (6f * Mathf.PI * airViscosity * _radiusM) / m; // 1/s
                Vector3 aDrag = -coeff * v * Mathf.Max(0f, dragScale);
                _rb.AddForce(aDrag, ForceMode.Acceleration);
            }
        }
    }

    private void RecomputeRadiusIfNeeded(bool force)
    {
        float m = Mathf.Max(1e-9f, _rb.mass);
        if (!force && Mathf.Abs(m - _lastMass) < 1e-9f) return;

        _lastMass = m;

        // r from mass and density: r = (3m/(4*pi*rho))^(1/3)
        if (oilDensity > 1e-6f)
        {
            _radiusM = Mathf.Pow((3f * m) / (4f * Mathf.PI * oilDensity), 1f / 3f);
            _radiusM = Mathf.Clamp(_radiusM, 1e-6f, 1f);
        }
        else
        {
            _radiusM = 0.0005f;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!destroyOnCollision) return;
        ResetDrop();
    }
}
