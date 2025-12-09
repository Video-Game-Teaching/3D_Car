using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerDrivingInput : MonoBehaviour, ICarInputProvider
{
    private CarInputActions _actions;
    private bool _pausePressed;
    private bool _nitroPressed;

    public float Steer { get; private set; }
    public bool Throttle { get; private set; }
    public bool Brake { get; private set; }
    public bool Handbrake { get; private set; }
    public bool Drift { get; private set; }
    public bool Nitro
    {
        get
        {
            bool pressed = _nitroPressed;
            _nitroPressed = false;
            return pressed;
        }
    }
    public bool PausePressed => _pausePressed;

    void Awake()
    {
        _actions = new CarInputActions();
        _actions.Driving.Pause.performed += _ => _pausePressed = true;
    }

    void OnEnable() => _actions.Enable();
    void OnDisable() => _actions.Disable();

    void Update()
    {
        Steer = _actions.Driving.Steer.ReadValue<float>();
        Throttle = _actions.Driving.Throttle.IsPressed();  // 改为数字油门 true/false
        Brake = _actions.Driving.Brake.IsPressed();
        Handbrake = _actions.Driving.Handbrake.IsPressed();
        Drift = _actions.Driving.Drift.IsPressed();

        // Edge detection for Nitro - latch until consumed by movement logic
        if (_actions.Driving.Nitro.WasPressedThisFrame())
        {
            _nitroPressed = true;
        }

        // 边沿：本帧暴露 PausePressed = true，随后清零
        if (_pausePressed) _pausePressed = false;
    }
}

