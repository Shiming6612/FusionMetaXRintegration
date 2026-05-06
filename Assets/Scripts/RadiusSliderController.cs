using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RadiusSliderController : MonoBehaviour
{
    [Header("UI")]
    public GameObject panelRoot;
    public Slider radiusSlider;
    public TMP_Text radiusText;

    [Header("Target")]
    public SpraySpawner spraySpawner;

    [Header("Radius Settings")]
    public float minRadiusMicrometer = 0.3f;
    public float maxRadiusMicrometer = 2.0f;
    public float defaultRadiusMicrometer = 0.5f;

    [Header("Behaviour")]
    public bool clearDropsWhenRadiusChanges = true;
    public bool enableTutorialRadiusModeOnStart = false;
    public bool hidePanelOnStart = true;

    private bool isActiveForTask;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        SetupSlider();
    }

    private void Start()
    {
        if (hidePanelOnStart && panelRoot != null)
            panelRoot.SetActive(false);

        if (enableTutorialRadiusModeOnStart)
            StartRadiusTask();
        else
            isActiveForTask = false;
    }

    private void SetupSlider()
    {
        if (radiusSlider == null)
            return;

        radiusSlider.minValue = minRadiusMicrometer;
        radiusSlider.maxValue = maxRadiusMicrometer;
        radiusSlider.wholeNumbers = false;
        radiusSlider.value = defaultRadiusMicrometer;

        radiusSlider.onValueChanged.RemoveAllListeners();
        radiusSlider.onValueChanged.AddListener(OnSliderValueChanged);

        UpdateRadiusText(radiusSlider.value);
    }

    public void StartRadiusTask()
    {
        isActiveForTask = true;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (radiusSlider != null)
        {
            radiusSlider.interactable = true;
            radiusSlider.value = defaultRadiusMicrometer;
        }

        if (spraySpawner != null)
        {
            spraySpawner.EnableTutorialRadiusMode();
            spraySpawner.SetTutorialRadiusMicrometer(defaultRadiusMicrometer, true);
        }

        UpdateRadiusText(defaultRadiusMicrometer);
    }

    public void EndRadiusTask()
    {
        isActiveForTask = false;

        if (radiusSlider != null)
            radiusSlider.interactable = false;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (spraySpawner != null)
            spraySpawner.ReturnToRandomModeAndClearDrops();
    }

    public void LockRadiusButKeepPanelVisible()
    {
        isActiveForTask = false;

        if (radiusSlider != null)
            radiusSlider.interactable = false;
    }

    public void ShowPanelOnly()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void HidePanelOnly()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnSliderValueChanged(float value)
    {
        UpdateRadiusText(value);

        if (!isActiveForTask)
            return;

        if (spraySpawner != null)
            spraySpawner.SetTutorialRadiusMicrometer(value, clearDropsWhenRadiusChanges);
    }

    private void UpdateRadiusText(float radius)
    {
        if (radiusText != null)
            radiusText.text = "Radius: " + radius.ToString("0.00") + " µm";
    }
}