using UnityEngine;

public class AIDrivingInput : MonoBehaviour, ICarInputProvider
{
    [Header("AI Input Values")]
    public float steerInput = 0f;
    public bool throttleInput = false;
    public bool brakeInput = false;
    public bool handbrakeInput = false;
    public bool driftInput = false;
    public bool nitroInput = false;
    public bool pausePressed = false;

    // ICarInputProvider implementation
    public float Steer => steerInput;
    public bool Throttle => throttleInput;
    public bool Brake => brakeInput;
    public bool Handbrake => handbrakeInput;
    public bool Drift => driftInput;
    public bool Nitro => nitroInput;
    public bool PausePressed => pausePressed;

    // Public methods to control AI input
    public void SetSteer(float steer)
    {
        steerInput = Mathf.Clamp(steer, -1f, 1f);
    }

    public void SetThrottle(bool throttle)
    {
        throttleInput = throttle;
    }

    public void SetBrake(bool brake)
    {
        brakeInput = brake;
    }

    public void SetHandbrake(bool handbrake)
    {
        handbrakeInput = handbrake;
    }

    public void SetDrift(bool drift)
    {
        driftInput = drift;
    }

    public void SetNitro(bool nitro)
    {
        nitroInput = nitro;
    }

    // Reset all inputs
    public void ResetInputs()
    {
        steerInput = 0f;
        throttleInput = false;
        brakeInput = false;
        handbrakeInput = false;
        driftInput = false;
        nitroInput = false;
        pausePressed = false;
    }
}
