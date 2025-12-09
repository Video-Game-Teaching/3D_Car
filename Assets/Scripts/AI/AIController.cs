using UnityEngine;

public class AIController : MonoBehaviour
{
    [Header("AI Settings")]
    public float maxSpeed = 25f; // Slightly slower than player for challenge
    public float acceleration = 50f;
    public float brakeForce = 4f;
    public float steerSensitivity = 1.2f;
    public float lookAheadDistance = 10f;
    public float waypointReachDistance = 8f;

    [Header("AI Behavior")]
    [Range(0f, 1f)]
    public float aggressiveness = 0.7f; // How aggressive the AI is
    [Range(0f, 1f)]
    public float skillLevel = 0.8f; // AI skill level (affects precision)
    public float nitroUsageThreshold = 0.3f; // When to use nitro (0.3 = 30% of max speed)
    public float brakeThreshold = 0.8f; // When to brake before turns

    [Header("References")]
    public WaypointSystem waypointSystem;
    public AIDrivingInput aiInput;
    public CarController carController;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private Rigidbody rb;
    private Transform currentTarget;
    private Vector3 targetDirection;
    private float currentSpeed;
    private float nitroCooldown = 0f;
    private bool isRacing = false;

    // AI state
    private enum AIState
    {
        Racing,
        Braking,
        Accelerating,
        Cornering
    }
    private AIState currentState = AIState.Racing;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        aiInput = GetComponent<AIDrivingInput>();
        carController = GetComponent<CarController>();

        // Find waypoint system if not assigned
        if (waypointSystem == null)
        {
            waypointSystem = FindObjectOfType<WaypointSystem>();
        }

        // Adjust car controller settings for AI
        if (carController != null)
        {
            carController.maxSpeed = maxSpeed;
            carController.acceleration = acceleration;
        }
    }

    void Update()
    {
        if (!isRacing) return;

        UpdateNitroCooldown();
        UpdateAIBehavior();
    }

    void FixedUpdate()
    {
        if (!isRacing) return;

        UpdateMovement();
    }

    void UpdateNitroCooldown()
    {
        if (nitroCooldown > 0f)
        {
            nitroCooldown -= Time.deltaTime;
        }
    }

    void UpdateAIBehavior()
    {
        if (waypointSystem == null || waypointSystem.GetCurrentWaypoint() == null) return;

        currentTarget = waypointSystem.GetCurrentWaypoint();
        currentSpeed = rb.velocity.magnitude;

        // Calculate direction to target
        targetDirection = (currentTarget.position - transform.position).normalized;

        // Check if we've reached the current waypoint
        if (waypointSystem.IsAtCurrentWaypoint(transform.position))
        {
            waypointSystem.AdvanceToNextWaypoint();
            currentTarget = waypointSystem.GetCurrentWaypoint();
            if (currentTarget != null)
            {
                targetDirection = (currentTarget.position - transform.position).normalized;
            }
        }

        // Determine AI state based on situation
        DetermineAIState();

        // Apply AI behavior based on state
        ApplyAIBehavior();
    }

    void DetermineAIState()
    {
        if (currentTarget == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        float speedRatio = currentSpeed / maxSpeed;

        // Look ahead to see if there's a sharp turn coming
        Transform nextWaypoint = waypointSystem.GetNextWaypoint();
        if (nextWaypoint != null)
        {
            Vector3 currentDirection = targetDirection;
            Vector3 nextDirection = (nextWaypoint.position - currentTarget.position).normalized;
            float turnAngle = Vector3.Angle(currentDirection, nextDirection);

            if (turnAngle > 45f && speedRatio > brakeThreshold)
            {
                currentState = AIState.Braking;
            }
            else if (speedRatio < 0.3f)
            {
                currentState = AIState.Accelerating;
            }
            else if (turnAngle > 30f)
            {
                currentState = AIState.Cornering;
            }
            else
            {
                currentState = AIState.Racing;
            }
        }
        else
        {
            currentState = AIState.Racing;
        }
    }

    void ApplyAIBehavior()
    {
        if (aiInput == null) return;

        // Calculate steering
        float steerInput = CalculateSteering();
        aiInput.SetSteer(steerInput);

        // Calculate throttle and brake
        bool shouldThrottle = false;
        bool shouldBrake = false;
        bool shouldHandbrake = false;
        bool shouldNitro = false;

        switch (currentState)
        {
            case AIState.Racing:
                shouldThrottle = currentSpeed < maxSpeed * 0.9f;
                shouldNitro = ShouldUseNitro();
                break;

            case AIState.Accelerating:
                shouldThrottle = true;
                shouldNitro = ShouldUseNitro();
                break;

            case AIState.Braking:
                shouldBrake = true;
                shouldThrottle = currentSpeed < maxSpeed * 0.5f;
                break;

            case AIState.Cornering:
                shouldThrottle = currentSpeed < maxSpeed * 0.7f;
                // Use handbrake for sharp turns if aggressive enough
                if (aggressiveness > 0.6f && Mathf.Abs(steerInput) > 0.7f)
                {
                    shouldHandbrake = true;
                    shouldThrottle = false;
                }
                break;
        }

        // Apply skill level adjustments
        ApplySkillLevelAdjustments(ref shouldThrottle, ref shouldBrake, ref shouldHandbrake, ref shouldNitro);

        // Set inputs
        aiInput.SetThrottle(shouldThrottle);
        aiInput.SetBrake(shouldBrake);
        aiInput.SetHandbrake(shouldHandbrake);
        aiInput.SetNitro(shouldNitro);
    }

    float CalculateSteering()
    {
        if (currentTarget == null) return 0f;

        // Calculate the angle between car's forward direction and target direction
        Vector3 carForward = transform.forward;
        Vector3 targetDirection = (currentTarget.position - transform.position).normalized;

        // Project onto the horizontal plane
        carForward.y = 0f;
        targetDirection.y = 0f;

        float angle = Vector3.SignedAngle(carForward, targetDirection, Vector3.up);
        // float steerInput = Mathf.Clamp(angle / 45f, -1f, 1f); // Normalize to -1 to 1
        float steerInput = Mathf.Clamp(angle / 20f, -1f, 1f); // try a more sensitive steering

        // Apply skill level - higher skill = more precise steering
        steerInput *= (0.5f + skillLevel * 0.5f);

        // Apply aggressiveness - more aggressive = more responsive steering
        steerInput *= (0.7f + aggressiveness * 0.3f);

        return steerInput * steerSensitivity;
    }

    bool ShouldUseNitro()
    {
        if (nitroCooldown > 0f) return false;

        float speedRatio = currentSpeed / maxSpeed;
        return speedRatio < nitroUsageThreshold && currentState != AIState.Braking;
    }

    void ApplySkillLevelAdjustments(ref bool throttle, ref bool brake, ref bool handbrake, ref bool nitro)
    {
        // Lower skill level means more mistakes and less optimal decisions
        float mistakeChance = (1f - skillLevel) * 0.3f;

        if (Random.Range(0f, 1f) < mistakeChance)
        {
            // Randomly make mistakes
            if (Random.Range(0f, 1f) < 0.5f)
            {
                throttle = !throttle;
            }
            else
            {
                brake = !brake;
            }
        }

        // Reduce nitro usage for lower skill
        if (nitro && skillLevel < 0.5f)
        {
            nitro = Random.Range(0f, 1f) < skillLevel;
        }
    }

    void UpdateMovement()
    {
        // The CarController handles the actual movement based on AI input
        // This method can be used for additional AI-specific movement adjustments
    }

    public void StartRacing()
    {
        isRacing = true;
        if (waypointSystem != null)
        {
            waypointSystem.ResetToStart();
        }
    }

    public void StopRacing()
    {
        isRacing = false;
        if (aiInput != null)
        {
            aiInput.ResetInputs();
        }
    }

    public void SetDifficulty(float difficulty)
    {
        // Clamp difficulty between 0 and 1
        difficulty = Mathf.Clamp01(difficulty);

        // Adjust AI parameters based on difficulty
        skillLevel = difficulty;
        aggressiveness = 0.5f + difficulty * 0.5f; // More aggressive at higher difficulty
        maxSpeed = 20f + difficulty * 10f; // Faster at higher difficulty

        if (carController != null)
        {
            carController.maxSpeed = maxSpeed;
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebugInfo || currentTarget == null) return;

        // Draw line to current target
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, currentTarget.position);

        // Draw look ahead point
        Gizmos.color = Color.blue;
        Vector3 lookAheadPoint = transform.position + transform.forward * lookAheadDistance;
        Gizmos.DrawWireSphere(lookAheadPoint, 2f);

        // Draw current state
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, currentState.ToString());
#endif
    }
}
