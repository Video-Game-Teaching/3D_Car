using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class McLarenCluster : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Rigidbody targetRb;

    public enum Unit { MPH, KPH }
    [SerializeField] Unit unit = Unit.MPH;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI speedText;
    [SerializeField] TextMeshProUGUI unitText;
    [SerializeField] TextMeshProUGUI gearText;
    [SerializeField] Image rpmBar;       // 横向转速条（Filled Horizontal）
    [SerializeField] Image[] shiftLeds;  // 顶部换挡灯（从左到右）

    [Header("Dynamics")]
    [Range(0f,1f)] public float speedSmooth = 0.25f;
    [Range(0f,1f)] public float rpmSmooth   = 0.25f;
    public float zeroThresholdMps = 0.3f;

    [Header("Powertrain (估算/可替换)")]
    // 估算用：由速度推算转速（如果你有真实引擎转速，直接在 UpdateRPMFromGame 把 rpm 赋值即可）
    public float wheelRadius = 0.34f;         // 车轮半径（米），超跑 0.32~0.36
    public float finalDrive  = 3.54f;
    public float[] gearRatios = { 0f, 3.80f, 2.32f, 1.80f, 1.46f, 1.23f, 1.03f, 0.84f }; // 1~7档
    public float upshiftRpm = 7800f;
    public float downshiftRpm = 3000f;
    public float redline = 8200f;

    [Header("Colors")]
    public Color barLow  = Color.white;
    public Color barMid  = new Color(1f, 0.8f, 0.2f);     // 黄
    public Color barHigh = new Color(1f, 0.25f, 0.15f);   // 红
    public Color ledOff  = new Color(1f,1f,1f,0.15f);
    public Color ledOn   = new Color(1f,0.8f,0.2f);
    public Color ledRed  = new Color(1f,0.25f,0.15f);

    float displaySpeed; // 平滑后的速度（当前单位）
    float rpm;          // 估算/输入的引擎转速
    int   gear = 1;

    const float MpsToMph = 2.23693629f;
    const float MpsToKph = 3.6f;

    void Reset()
    {
        if (!targetRb) targetRb = FindAnyObjectByType<Rigidbody>();
    }

    void Update()
    {
        if (!targetRb) return;

        // --- 1) 速度（单位换算 + 平滑） ---
        float mps = targetRb.velocity.magnitude;
        float conv = (unit == Unit.MPH) ? MpsToMph : MpsToKph;
        float rawSpeed = (mps < zeroThresholdMps) ? 0f : mps * conv;
        float sLerp = 1f - Mathf.Exp(-speedSmooth * Time.unscaledDeltaTime * 60f);
        displaySpeed = Mathf.Lerp(displaySpeed, rawSpeed, sLerp);

        int shownSpeed = Mathf.RoundToInt(displaySpeed);
        if (speedText) speedText.text = shownSpeed.ToString("D3");
        if (unitText)  unitText.text  = (unit == Unit.MPH) ? "mph" : "km/h";

        // --- 2) 估算/更新转速 + 自动换挡 ---
        UpdateRPMFromPhysics(mps); // 如果你有真实 rpm，可改成 UpdateRPMFromGame(realRpm)
        AutoShift();

        // 平滑到表盘
        float rLerp = 1f - Mathf.Exp(-rpmSmooth * Time.unscaledDeltaTime * 60f);
        float shownRpm = Mathf.Lerp(GetShownRPM(), rpm, rLerp);
        SetShownRPM(shownRpm);

        // --- 3) 驱动转速条与换挡灯 ---
        float frac = Mathf.Clamp01(shownRpm / redline);              // 0~1
        if (rpmBar)
        {
            rpmBar.fillAmount = frac;
            rpmBar.color = Color.Lerp(
                Color.Lerp(barLow, barMid, Mathf.SmoothStep(0f, 0.65f, frac)),
                barHigh,
                Mathf.SmoothStep(0.65f, 1f, frac)
            );
        }

        if (shiftLeds != null && shiftLeds.Length > 0)
        {
            // 前 60% 逐个点亮为黄，最后 20% 变红并在>97% 闪烁
            int n = shiftLeds.Length;
            for (int i = 0; i < n; i++)
            {
                float ledPos = (i + 1f) / n;         // 0..1
                bool on = frac >= ledPos * 0.85f;    // 灯点亮阈值（稍晚一点更有“冲刺感”）
                Color c = on ? ledOn : ledOff;
                if (on && ledPos > 0.80f) c = ledRed;

                // 临界闪烁
                if (frac > 0.97f && on)
                {
                    float f = (Mathf.Sin(Time.unscaledTime * 20f) * 0.5f + 0.5f); // 20Hz 闪
                    c.a = Mathf.Lerp(0.25f, 1f, f);
                }

                if (shiftLeds[i]) shiftLeds[i].color = c;
            }
        }

        // --- 4) 档位显示 ---
        if (gearText) gearText.text = GearLabel();
    }

    // —— 物理估算转速（无引擎系统时可用） ——
    void UpdateRPMFromPhysics(float mps)
    {
        // 车轮转速（转/分）
        float wheelRpm = (mps / (2f * Mathf.PI * Mathf.Max(0.01f, wheelRadius))) * 60f;
        float ratio = finalDrive * Mathf.Max(1f, (gear >= 1 && gear < gearRatios.Length) ? gearRatios[gear] : 1f);
        rpm = Mathf.Clamp(wheelRpm * ratio, 600f, redline * 1.05f);
    }

    // —— 如果你以后有真实引擎转速，直接用这个接口赋值 ——
    public void UpdateRPMFromGame(float engineRpm)
    {
        rpm = Mathf.Clamp(engineRpm, 600f, redline * 1.05f);
    }

    void AutoShift()
    {
        // 简单自动变速逻辑（保持在合理转速）
        if (rpm > upshiftRpm && gear < gearRatios.Length - 1) gear++;
        else if (rpm < downshiftRpm && gear > 1) gear--;
    }

    string GearLabel()
    {
        // 你也可以自定义 P/R/N/D，这里简单用 1..7
        return gear.ToString();
    }

    // —— 用于平滑缓存（不必外显） ——
    float _shownRpm;
    float GetShownRPM() => _shownRpm;
    void SetShownRPM(float v) => _shownRpm = v;

    // 切单位
    public void UseKph(bool useKph) => unit = useKph ? Unit.KPH : Unit.MPH;
}
