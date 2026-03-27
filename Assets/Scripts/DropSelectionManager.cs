using System;
using UnityEngine;

public class DropSelectionManager : MonoBehaviour
{
    [Header("Ray")]
    public Transform rayOrigin;
    public float rayLength = 10f;
    public LayerMask oilDropLayerMask = ~0;
    public float sphereCastRadius = 0.03f;
    public bool enableHoverHighlight = true;

    [Header("Visual (optional)")]
    public LineRenderer line;
    public Color rayNormalColor = Color.white;
    public Color rayHitColor = Color.yellow;

    [Header("Input")]
    public bool useQuestInput = true;
    public bool useMouseInEditor = true;
    public bool allowAButtonFallback = false;

    [Tooltip("Force which controller reads trigger input.")]
    public OVRInput.Controller triggerController = OVRInput.Controller.RTouch;

    [Header("Debug")]
    public bool logHits = false;
    public bool logSelection = true;

    public SelectableDrop CurrentSelected => _selected;
    public event Action<SelectableDrop> OnSelectionChanged;

    SelectableDrop _selected;
    SelectableDrop _hovered;

    void Reset()
    {
        rayOrigin = Camera.main ? Camera.main.transform : null;
    }

    void Update()
    {
        if (rayOrigin == null) return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        bool hitSomething = Physics.SphereCast(
            ray,
            sphereCastRadius,
            out RaycastHit hit,
            rayLength,
            oilDropLayerMask,
            QueryTriggerInteraction.Ignore
        );

        SelectableDrop hitDrop = null;
        if (hitSomething && hit.collider != null)
            hitDrop = hit.collider.GetComponentInParent<SelectableDrop>();

        if (logHits)
            Debug.Log(hitDrop != null ? $"[DropSelection] Hit {hitDrop.name}" : "[DropSelection] No hit");

        UpdateHover(hitDrop);
        UpdateLine(ray, hitSomething, hit);

        if (GetSelectDown())
        {
            SetSelected(hitDrop);
        }
    }

    void UpdateHover(SelectableDrop hitDrop)
    {
        if (!enableHoverHighlight)
        {
            if (_hovered != null) _hovered.SetHovered(false);
            _hovered = null;
            return;
        }

        if (_hovered == hitDrop) return;

        if (_hovered != null) _hovered.SetHovered(false);
        _hovered = hitDrop;
        if (_hovered != null) _hovered.SetHovered(true);
    }

    void UpdateLine(Ray ray, bool hitSomething, RaycastHit hit)
    {
        if (line == null) return;

        line.positionCount = 2;

        Vector3 end = ray.origin + ray.direction * rayLength;
        if (hitSomething) end = hit.point;

        line.SetPosition(0, ray.origin);
        line.SetPosition(1, end);

        line.startColor = hitSomething ? rayHitColor : rayNormalColor;
        line.endColor = hitSomething ? rayHitColor : rayNormalColor;

        if (!line.enabled) line.enabled = true;
    }

    public void SetSelected(SelectableDrop newSelected)
    {
        if (_selected == newSelected) return;

        if (_selected != null) _selected.SetSelected(false);

        _selected = newSelected;

        if (_selected != null) _selected.SetSelected(true);

        if (logSelection)
            Debug.Log(_selected != null ? $"[DropSelection] Selected: {_selected.name}" : "[DropSelection] Selected: None");

        OnSelectionChanged?.Invoke(_selected);
    }

    bool GetSelectDown()
    {
        if (useMouseInEditor && Application.isEditor)
        {
            if (Input.GetMouseButtonDown(0))
                return true;
        }

        if (useQuestInput)
        {
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, triggerController))
                return true;

            if (allowAButtonFallback && OVRInput.GetDown(OVRInput.Button.One, triggerController))
                return true;
        }

        return false;
    }
}
