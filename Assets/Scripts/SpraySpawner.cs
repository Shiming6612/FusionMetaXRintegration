using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpraySpawner : MonoBehaviour
{
    [Header("Refs")]
    public Transform spawnOrigin;
    public Transform aimTarget;
    public OilDrop dropPrefab;

    [Header("Limits")]
    public int maxTotalDrops = 15;
    public int minDropsPerSpray = 3;
    public int maxDropsPerSpray = 6;
    public float burstDuration = 0.12f;
    public float minTimeBetweenSprays = 0.05f;

    [Header("Spawn + Launch")]
    public float spawnRadius = 0.01f;
    public bool useAimTarget = true;
    [Range(0f, 60f)] public float coneAngle = 18f;
    public float baseLaunchSpeed = 2.0f;
    public float speedRandomPercent = 0.25f;
    public float lateralJitterSpeed = 0.35f;
    public float upwardBias = 0.15f;

    [Header("Nozzle Feedback (per squeeze)")]
    public AudioSource nozzleSfxSource;
    public AudioClip nozzleSfx;
    public ParticleSystem nozzleVfxPrefab;
    public Transform nozzlePoint;

    int _spawnedCount = 0;
    float _lastSprayTime = -999f;
    Coroutine _burstRoutine;
    readonly List<OilDrop> _spawned = new List<OilDrop>();

    public void SprayOnce()
    {
        if (!spawnOrigin || !dropPrefab) return;
        if (_spawnedCount >= maxTotalDrops) return;
        if (Time.time - _lastSprayTime < minTimeBetweenSprays) return;

        _lastSprayTime = Time.time;

        int want = Random.Range(minDropsPerSpray, maxDropsPerSpray + 1);
        want = Mathf.Min(want, maxTotalDrops - _spawnedCount);
        if (want <= 0) return;

        PlayNozzleFeedback(); // <- once per squeeze

        if (_burstRoutine != null) StopCoroutine(_burstRoutine);
        _burstRoutine = StartCoroutine(SpawnBurst(want));
    }

    public void ResetAllDrops()
    {
        if (_burstRoutine != null) StopCoroutine(_burstRoutine);
        _burstRoutine = null;

        for (int i = 0; i < _spawned.Count; i++)
            if (_spawned[i] != null) Destroy(_spawned[i].gameObject);

        _spawned.Clear();
        _spawnedCount = 0;
    }

    IEnumerator SpawnBurst(int count)
    {
        float dt = (burstDuration <= 0f || count <= 1) ? 0f : burstDuration / (count - 1);

        for (int i = 0; i < count; i++)
        {
            SpawnOne();
            if (dt > 0f) yield return new WaitForSeconds(dt);
        }

        _burstRoutine = null;
    }

    void SpawnOne()
    {
        var drop = Instantiate(dropPrefab);
        _spawned.Add(drop);
        _spawnedCount++;

        var props = drop.GetComponent<DropProperties>();
        if (props != null) props.RandomizeAndApply();

        Vector2 p = Random.insideUnitCircle * spawnRadius;
        Vector3 pos = spawnOrigin.position + spawnOrigin.right * p.x + spawnOrigin.up * p.y;

        Vector3 baseDir = (useAimTarget && aimTarget != null)
            ? (aimTarget.position - pos).normalized
            : spawnOrigin.forward;

        baseDir = (baseDir + Vector3.up * upwardBias).normalized;
        Vector3 dir = RandomDirectionInCone(baseDir, coneAngle);

        float speed = baseLaunchSpeed * (1f + Random.Range(-speedRandomPercent, speedRandomPercent));
        Vector3 lateral = Vector3.ProjectOnPlane(Random.onUnitSphere, dir).normalized * lateralJitterSpeed;

        drop.Launch(pos, dir * speed + lateral);
    }

    void PlayNozzleFeedback()
    {
        if (nozzleSfxSource != null && nozzleSfx != null)
            nozzleSfxSource.PlayOneShot(nozzleSfx);

        if (nozzleVfxPrefab == null) return;

        Transform t = nozzlePoint != null ? nozzlePoint : (spawnOrigin != null ? spawnOrigin : transform);
        var vfx = Instantiate(nozzleVfxPrefab, t.position, t.rotation);
        vfx.Play();

        float destroyAfter = 2f;
        var main = vfx.main;
        if (!main.loop)
        {
            float lifeMax = main.startLifetime.constantMax;
            destroyAfter = Mathf.Max(0.1f, main.duration + lifeMax + 0.2f);
        }
        Destroy(vfx.gameObject, destroyAfter);
    }

    static Vector3 RandomDirectionInCone(Vector3 forward, float coneHalfAngleDeg)
    {
        if (coneHalfAngleDeg <= 0.001f) return forward.normalized;

        float coneRad = coneHalfAngleDeg * Mathf.Deg2Rad;
        float cosMin = Mathf.Cos(coneRad);

        float z = Random.Range(cosMin, 1f);
        float theta = Random.Range(0f, Mathf.PI * 2f);
        float r = Mathf.Sqrt(1f - z * z);

        Vector3 local = new Vector3(r * Mathf.Cos(theta), r * Mathf.Sin(theta), z);
        return Quaternion.FromToRotation(Vector3.forward, forward.normalized) * local;
    }
}
