using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICarInputProvider
{
    float Steer { get; }      // -1..1
    bool Throttle { get; }    // true/false (digital throttle)
    bool Brake { get; }       // true/false (digital brake)
    bool Handbrake { get; }
    bool Drift { get; }       // Mario Kart style drift
    bool Nitro { get; }       // Nitro Boost
    bool PausePressed { get; }
}

