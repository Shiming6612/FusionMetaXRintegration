using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class VoltageHumAudio : MonoBehaviour
{
    public VoltageKnobInput voltageSource;

    [Header("Volume Mapping")]
    public float maxKV = 10f;                 // 10 kV => maxVolume
    [Range(0f, 1f)] public float maxVolume = 0.8f;

    private AudioSource _audio;

    void Awake()
    {
        _audio = GetComponent<AudioSource>();
        _audio.loop = true;
        _audio.playOnAwake = false;
        _audio.volume = 0f;

        if (_audio.clip != null)
            _audio.Play(); // start muted
    }

    void Update()
    {
        float kv = (voltageSource != null) ? voltageSource.CurrentKV : 0f;

        // 0 kV => 0 volume, 10 kV => maxVolume
        _audio.volume = Mathf.Clamp01(kv / maxKV) * maxVolume;
    }
}
