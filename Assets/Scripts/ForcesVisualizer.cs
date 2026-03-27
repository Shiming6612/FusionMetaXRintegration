using UnityEngine;

public class ForcesVisualizer : MonoBehaviour
{
    [Header("Refs (drag in Inspector)")]
    public DropSelectionManager selectionManager;
    public VoltageKnobInput voltageSource;          // 读取当前kV（可选）
    public ElectricFieldVolume fieldVolume;         // 如果你有invertVoltage等逻辑（可选）

    [Header("Prefab")]
    public GameObject forcesRootPrefab;

    [Header("Child names (must match prefab)")]
    public string arrowFgName = "Arrow_Fg";
    public string arrowFbName = "Arrow_Fb";
    public string arrowFelName = "Arrow_Fel";

    [Header("Attach to droplet")]
    public Vector3 localOffset = Vector3.zero;      // 你可以之后慢慢调位置
    public Vector3 localEuler = Vector3.zero;       // 需要时再调
    public bool billboardToCamera = true;           // 平面箭头建议打开
    public float billboardSmooth = 0f;              // 0=不平滑；>0可做插值（可选）

    [Header("Force -> Length Mapping (visual only)")]
    public float lengthScale = 0.02f;               // 力映射到箭头长度的比例（你需要调）
    public float minLen = 0.02f;
    public float maxLen = 0.35f;

    [Header("Buoyancy (approx)")]
    public bool useSimpleBuoyancyRatio = true;
    [Range(0f, 1f)] public float buoyancyRatio = 0.2f; // Fb ≈ 0.2 * Fg（先跑通再精确）

    [Header("Electrical (visual)")]
    public bool showFelOnlyWhenVoltageNonZero = true;
    public float voltageEpsilon = 0.0001f;
    public bool assumeFieldUp = true;               // 你装置里电场方向若固定向上，先用这个

    GameObject _inst;
    Transform _fg, _fb, _fel;

    SelectableDrop _lastSelected;
    Transform _targetDrop;      // 当前选中油滴 transform
    DropProperties _dp;
    Rigidbody _rb;

    void Start()
    {
        if (forcesRootPrefab == null)
        {
            Debug.LogError("[ForcesVisualizer] forcesRootPrefab is NULL.");
            return;
        }

        _inst = Instantiate(forcesRootPrefab);
        _inst.name = "ForcesRoot(Clone)";
        _inst.SetActive(false);

        _fg = FindChild(_inst.transform, arrowFgName);
        _fb = FindChild(_inst.transform, arrowFbName);
        _fel = FindChild(_inst.transform, arrowFelName);

        if (_fg == null || _fb == null || _fel == null)
            Debug.LogError("[ForcesVisualizer] Arrow child not found. Check prefab child names.");
    }

    void Update()
    {
        if (_inst == null || selectionManager == null) return;

        // 1) 读取当前选中油滴（你现有系统已经在维护这个）
        var sel = selectionManager.CurrentSelected;

        // 2) 选中对象变化时，重新挂载/隐藏
        if (sel != _lastSelected)
        {
            _lastSelected = sel;
            OnSelectionChanged(sel);
        }

        // 3) 若未选中：不做任何更新
        if (_targetDrop == null || !_inst.activeSelf) return;

        // 4) 跟随油滴（因为 SetParent 了，一般不需要每帧位置；但你可能想保持billboard）
        if (billboardToCamera)
            FaceCamera();

        // 5) 更新箭头长度（先跑通“会变”，力学精确后面再换公式）
        UpdateForcesAndArrows();
    }

    void OnSelectionChanged(SelectableDrop sel)
    {
        if (sel == null)
        {
            _targetDrop = null;
            _dp = null;
            _rb = null;
            _inst.SetActive(false);
            return;
        }

        _targetDrop = sel.transform;
        _dp = sel.GetComponent<DropProperties>();
        _rb = sel.GetComponent<Rigidbody>();

        // D：SetParent（不跑偏）
        _inst.transform.SetParent(_targetDrop, worldPositionStays: false);
        _inst.transform.localPosition = localOffset;
        _inst.transform.localRotation = Quaternion.Euler(localEuler);
        _inst.transform.localScale = Vector3.one;

        // B：强制可见
        _inst.SetActive(true);
    }

    void UpdateForcesAndArrows()
    {
        if (_fg == null || _fb == null || _fel == null) return;

        float mass = (_rb != null) ? Mathf.Max(1e-6f, _rb.mass) : 1e-6f;

        // 用你项目的重力（如果OilDrop里有customGravity，这里先用Physics.gravity）
        float gMag = Mathf.Abs(Physics.gravity.y);
        float Fg = mass * gMag;

        float Fb = useSimpleBuoyancyRatio ? (buoyancyRatio * Fg) : (0.0f);

        // 电压（kV）
        float kv = (voltageSource != null) ? voltageSource.CurrentKV : 0f;
        if (fieldVolume != null && fieldVolume.invertVoltage) kv = -kv;

        bool hasVoltage = Mathf.Abs(kv) > voltageEpsilon;
        if (showFelOnlyWhenVoltageNonZero)
            _fel.gameObject.SetActive(hasVoltage);
        else
            _fel.gameObject.SetActive(true);

        // electrical force（可视化：用 |q| 与 |kV| 做比例，先保证“电压越大箭头越长”）
        float qC = (_dp != null) ? _dp.ChargeC : 0f;
        float Fel = Mathf.Abs(qC) * Mathf.Abs(kv);   // 先做“趋势正确”的视觉量（后面可替换成 q*E）

        // 方向：Fg向下，Fb向上；Fel根据电荷符号决定（假设电场向上）
        SetArrow(_fg, Fg, isUp: false);
        SetArrow(_fb, Fb, isUp: true);

        if (hasVoltage && _fel.gameObject.activeSelf)
        {
            bool felUp = true;

            if (assumeFieldUp)
                felUp = (qC >= 0f);  // q>0 受力向上；q<0 向下（电场向上假设）
            else
                felUp = (qC < 0f);

            SetArrow(_fel, Fel, isUp: felUp);
        }
    }

    void SetArrow(Transform t, float forceLikeValue, bool isUp)
    {
        if (t == null) return;

        float len = Mathf.Clamp(forceLikeValue * lengthScale, minLen, maxLen);

        // 以 localScale.y 作为长度
        var s = t.localScale;
        s.y = len;
        t.localScale = s;

        // 方向：上=identity，下=绕Z翻转180（如果你箭头翻转轴不对，改成X=180）
        t.localRotation = isUp ? Quaternion.identity : Quaternion.Euler(0f, 0f, 180f);
    }

    void FaceCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;

        var targetRot = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
        if (billboardSmooth <= 0f)
            _inst.transform.rotation = targetRot;
        else
            _inst.transform.rotation = Quaternion.Slerp(_inst.transform.rotation, targetRot, Time.deltaTime * billboardSmooth);
    }

    Transform FindChild(Transform root, string name)
    {
        var all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i].name == name) return all[i];
        return null;
    }
}
