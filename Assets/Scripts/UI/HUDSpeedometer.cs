using UnityEngine;
using TMPro;

public class HUDSpeedometer : MonoBehaviour
{
    public enum Unit { Kmh, Mph }

    [Header("Refs")]
    [SerializeField] private Rigidbody targetRb;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private CanvasGroup canvasGroup;   // 可为空（则不做淡入淡出）

    [Header("Unit / Format")]
    [SerializeField] private Unit unit = Unit.Mph;
    [SerializeField] private bool padWithZeros = true;  // 显示 000/045/123 这种
    [SerializeField] private int padDigits = 3;         // 几位补零

    [Header("Smoothing")]
    [Range(0f, 1f)]
    [SerializeField] private float smooth = 0.25f;      // 0.15 更稳，0.35 更灵
    [SerializeField] private float zeroThresholdMps = 0.3f; // <阈值当作 0，抑制抖动

    [Header("Look & Feel")]
    [SerializeField] private bool autoHideWhenZero = true;
    [SerializeField] private float colorMin = 30f;      // 低于此接近慢速颜色
    [SerializeField] private float colorMax = 120f;     // 高于此接近高速颜色
    [SerializeField] private Color slowColor = Color.white;
    [SerializeField] private Color fastColor = new Color(1f, 0.35f, 0.15f);

    private float displaySpeed;                         // 平滑后的速度（同所选单位）
    private const float MS_TO_KMH = 3.6f;
    private const float MS_TO_MPH = 2.23693629f;

    void Reset()
    {
        speedText = GetComponent<TextMeshProUGUI>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (!targetRb) targetRb = FindAnyObjectByType<Rigidbody>();
    }

    void Update()
    {
        if (!targetRb || !speedText) return;

        // 1) 取物理速度（m/s）
        float mps = targetRb.velocity.magnitude;

        // 2) 抑制低速抖动 + 单位换算
        float conv = (unit == Unit.Kmh) ? MS_TO_KMH : MS_TO_MPH;
        float raw = (mps < zeroThresholdMps) ? 0f : mps * conv;

        // 3) 指数平滑（帧率无关）
        float lerpFactor = 1f - Mathf.Exp(-smooth * Time.unscaledDeltaTime * 60f);
        displaySpeed = Mathf.Lerp(displaySpeed, raw, lerpFactor);

        // 4) 显示文本
        int shown = Mathf.RoundToInt(displaySpeed);
        string suffix = (unit == Unit.Kmh) ? "km/h" : "mph";
        string number = padWithZeros ? shown.ToString("D" + padDigits) : shown.ToString();
        speedText.text = $"{number} {suffix}";

        // 5) 颜色渐变
        float t = Mathf.InverseLerp(colorMin, colorMax, shown);
        speedText.color = Color.Lerp(slowColor, fastColor, Mathf.Clamp01(t));

        // 6) 静止时淡入淡出
        if (canvasGroup && autoHideWhenZero)
        {
            float targetAlpha = (shown == 0) ? 0.45f : 1f;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, 10f * Time.unscaledDeltaTime);
        }
    }

    // 外部切换
    public void SetUnit(Unit u) => unit = u;
    public void SetTarget(Rigidbody rb) => targetRb = rb;
}
