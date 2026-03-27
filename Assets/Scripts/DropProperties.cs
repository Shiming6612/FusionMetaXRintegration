using UnityEngine;

[DisallowMultipleComponent]
public class DropProperties : MonoBehaviour
{
    [Header("Random Ranges (Simulation Units)")]
    [Tooltip("Rigidbody mass in kg (Unity). Keep in a reasonable range for stable physics.")]
    public float minMassKg = 0.005f;
    public float maxMassKg = 0.02f;

    [Tooltip("Charge in picoCoulombs (pC). 1 pC = 1e-12 C.")]
    public float minChargePC = -5f;
    public float maxChargePC = 5f;

    [Header("Options")]
    [Tooltip("If true, randomize in OnEnable(). Recommended: keep this OFF and randomize explicitly at spawn time (e.g., in SpraySpawner) to avoid double-randomization.")]
    public bool randomizeOnSpawn = false;

    public bool applyMassToRigidbody = true;

    public float MassKg { get; private set; }
    public float ChargeC { get; private set; }   // Coulombs

    private Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        if (randomizeOnSpawn)
            RandomizeAndApply();
    }

    public void RandomizeAndApply()
    {
        MassKg = Random.Range(minMassKg, maxMassKg);

        float chargePC = Random.Range(minChargePC, maxChargePC);
        ChargeC = chargePC * 1e-12f;

        if (applyMassToRigidbody && _rb != null)
            _rb.mass = MassKg;
    }
}
