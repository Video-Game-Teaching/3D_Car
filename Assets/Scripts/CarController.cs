using UnityEngine;
using System.Collections;

public class CarController : MonoBehaviour
{
    // -------------- Four wheels --------------
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform rearLeftWheel;
    public Transform rearRightWheel;

    // -------------- Movement Settings--------------
    public float maxSpeed = 50f;        // ~80 km/h
    public float acceleration = 90f;   // increased acceleration for higher speeds
    // steering
    public float turnSpeed = 1.5f;
    public float maxSteerAngle = 45f;
    // wheels
    public float wheelRotationSpeed = 75f;
    [Header("Reverse Settings")]
    public float reverseAcceleration = 60f;
    public float reverseMaxSpeed = 15f;
    public float reverseActivationSpeed = 1.5f;

    // -------------- Nitro System Settings --------------
    public float nitroMaxSpeed = 65f;   // ~160 km/h (nitro max speed)
    private float currentMaxSpeed;        // current max speed (switch between max speed and nitro max speed)

    // New discrete charge-based nitro system
    public int nitroChargesStored = 0;   // Current nitro charges (0-3)
    public const int nitroMaxCharges = 3;
    public float nitroBoostPowerLevel1 = 150f;  // 1.0x multiplier (1 charge)
    public float nitroBoostPowerLevel2 = 225f;  // 1.5x multiplier (2 charges)
    public float nitroBoostPowerLevel3 = 300f;  // 2.0x multiplier (3 charges)
    public float nitroActiveDuration = 3f;  // Duration for nitro boost
    private bool isNitroActive = false;
    private float nitroActiveTimeRemaining = 0f;
    private float currentNitroBoostPower = 0f;

    // -------------- Nitro VFX Settings --------------
    [Header("Nitro VFX")]
    public GameObject nitroVFXLevel1;  // VFX prefab for Level 1 (1 charge)
    public GameObject nitroVFXLevel2;  // VFX prefab for Level 2 (2 charges)
    public GameObject nitroVFXLevel3;  // VFX prefab for Level 3 (3 charges)
    public Transform nitroVFXSpawnPoint;  // Transform where VFX should spawn (if null, uses rear center)
    [Range(0.1f, 5f)]
    public float nitroVFXScale = 3f;  // Scale multiplier for nitro VFX (1 = normal size)
    private GameObject currentNitroVFX;  // Currently active VFX instance

    // -------------- Brake Settings--------------
    public float brakeForce = 5f;
    public float handbrakeForce = 5f;
    public float dragAmount = 2f;         // normal drag
    public float brakeDrag = 6f;          // extra drag when braking
    public float handbrakeDrag = 8f;     // extra drag when handbraking

    // -------------- Terrain Conforming Settings --------------
    public LayerMask groundLayerMask; // Layers considered as ground for raycasting (set in Inspector)
    public float raycastDistance = 2f;
    public float rotationSmoothing = 8f;    // the smooth parameter for the terrain-alignment: higher value => faster adjustment => more suitable for fast-moving cars
    public float extraGravityForce = 20f;  // Extra downward force when car is in the air

    // -------------- Collision Settings --------------
    [Header("Collision Slowdown")]
    public float collisionSlowdownMultiplier = 0.5f; // How much collision affects speed (0-1)
    public float minCollisionForce = 5f; // Minimum collision force to trigger slowdown
    public string[] collisionTags = { "Barrier", "AICar", "Player" }; // Tags that cause slowdown
    public float bounceBackForce = 10f; // Force to push car away from obstacle
    public float bounceBackDuration = 0.3f; // How long the bounce effect lasts

    // -------------- Audio Settings --------------
    [Header("Audio")]
    public AudioClip crashSound; // Sound effect for crashes
    public AudioClip nitroSound; // Sound effect for nitro activation
    [Range(0f, 1f)]
    public float crashSoundVolume = 1f; // Volume for crash sound (0-1)
    [Range(0f, 1f)]
    public float nitroSoundVolume = 0.5f; // Volume for nitro sound (0-1)
    private AudioSource audioSource; // AudioSource component for playing sounds
    private bool isCrashSoundPlaying = false; // Flag to prevent overlapping crash sounds

    // -------------- Off-Track Detection Settings --------------
    [Header("Off-Track Penalty")]
    public bool enableOffTrackPenalty = true; // Enable/disable off-track penalty
    public LayerMask trackLayerMask; // Layer for the track surface (set in Inspector)
    public string offTrackTag = "OffTrack"; // Tags for off-track surfaces
    public float offTrackDecelerationMultiplier = 0.8f; // Speed multiplier when off-track (0.8 = 20% speed reduction)
    public float offTrackDragIncrease = 1.5f; // Additional drag when off-track
    public float offTrackMaxSpeed = 30f; // Maximum speed when off-track (lower than normal maxSpeed)
    private bool isOffTrack = false; // Current off-track status

    private Rigidbody rb;
    private ICarInputProvider inputProvider;    // to fit the new input system..

    // Track wheel rotations separately to avoid euler angle issues
    private float frontLeftWheelRotation = 0f;
    private float frontRightWheelRotation = 0f;
    private float rearLeftWheelRotation = 0f;
    private float rearRightWheelRotation = 0f;

    // Bounce back system
    private bool isBouncing = false;
    private float bounceTimer = 0f;
    private Vector3 bounceDirection = Vector3.zero;

    // -------------- Drift Tracking System --------------
    private bool isDrifting = false;
    private float driftDuration = 0f;
    private float driftStartSpeed = 0f;  // Speed when drift started - maintain this speed
    private Vector3 driftStartDirection = Vector3.forward;  // Direction when drift started - for Mario Kart style blending
    private float driftInitialSteerDirection = 0f;  // Initial steering direction when drift started (for anti-cheat)
    private bool driftLevel1Awarded = false;  // Prevents duplicate awards per drift session
    private bool driftLevel2Awarded = false;
    private bool driftLevel3Awarded = false;
    private const float driftLevel1Threshold = 1f;    // 1 second for Level 1
    private const float driftLevel2Threshold = 1.8f;   // 1.8 seconds for Level 2
    private const float driftLevel3Threshold = 2.5f;   // 2.5 seconds for Level 3
    private const float driftDirectionBlendTime = 1.5f;  // Time to blend from start direction to current direction
    private const float driftSteerSwitchThreshold = 0.3f;  // Minimum steer input to be considered "turning"

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        inputProvider = GetComponent<ICarInputProvider>();

        // Set up ground layer mask to include both Ground and OffTrack layers
        groundLayerMask = LayerMask.GetMask("Ground", "OffTrack");

        frontLeftWheel = transform.Find("FL");
        frontRightWheel = transform.Find("FR");
        rearLeftWheel = transform.Find("RL");
        rearRightWheel = transform.Find("RR");

        rb.drag = dragAmount;  // maybe try to use a litle drag, for more realistic driving..?
        rb.angularDrag = 3f;  // also try angular drag..

        // initialize the nitro system
        currentMaxSpeed = maxSpeed;
        nitroChargesStored = 0;

        // Set up AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            // If no AudioSource exists, create one
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound (0 = 2D, 1 = 3D)
        }
    }

    void Update()
    {
        UpdateNitroSystem();
        UpdateDriftSystem();
        UpdateBounceBack();
    }

    private void UpdateNitroSystem()
    {
        // Update nitro active timer
        if (isNitroActive)
        {
            nitroActiveTimeRemaining -= Time.deltaTime;
            if (nitroActiveTimeRemaining <= 0f)
            {
                DeactivateNitro();
            }
        }
    }

    private void UpdateDriftSystem()
    {
        if (inputProvider == null) return;

        bool driftInput = inputProvider.Drift;

        // Check if drift started
        if (driftInput && !isDrifting)
        {
            // Start drift session - store current speed and direction for Mario Kart style blending
            float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
            driftStartSpeed = Mathf.Max(forwardSpeed, rb.velocity.magnitude * 0.9f); // Use forward speed or total speed
            if (driftStartSpeed < 5f) driftStartSpeed = 5f; // Minimum speed for drift

            // Store initial drift direction - this is where the drift will start
            // If we have significant velocity, use velocity direction, otherwise use forward
            if (rb.velocity.magnitude > 3f)
            {
                driftStartDirection = rb.velocity.normalized;
            }
            else
            {
                driftStartDirection = transform.forward;
            }

            // Store initial steering direction for anti-cheat (prevent A/D spam)
            float steerInput = inputProvider.Steer;
            // Only lock direction if player is actively steering when drift starts
            // If no steering input, allow steering in either direction
            if (Mathf.Abs(steerInput) > 0.1f)
            {
                driftInitialSteerDirection = Mathf.Sign(steerInput); // -1 for left, 1 for right
            }
            else
            {
                driftInitialSteerDirection = 0f; // No initial direction - allow steering in either direction
            }

            isDrifting = true;
            driftDuration = 0f;
            driftLevel1Awarded = false;
            driftLevel2Awarded = false;
            driftLevel3Awarded = false;
        }
        // Check if drift ended - award charges on release
        else if (!driftInput && isDrifting)
        {
            // Award charges based on total drift duration when releasing Space
            // Charges are ADDED to existing charges (stack up)
            // Level 3 (2.5+ seconds) - awards 3 charges
            if (driftDuration >= driftLevel3Threshold)
            {
                AwardNitroCharges(3);
            }
            // Level 2 (1.8-2.5 seconds) - awards 2 charges
            else if (driftDuration >= driftLevel2Threshold)
            {
                AwardNitroCharges(2);
            }
            // Level 1 (1-1.8 seconds) - awards 1 charge
            else if (driftDuration >= driftLevel1Threshold)
            {
                AwardNitroCharges(1);
            }

            // End drift session
            isDrifting = false;
            driftDuration = 0f;
            driftLevel1Awarded = false;
            driftLevel2Awarded = false;
            driftLevel3Awarded = false;
        }

        // Update drift duration while holding Space
        if (isDrifting)
        {
            driftDuration += Time.deltaTime;
        }
    }

    private void AwardNitroCharges(int charges)
    {
        // Add charges (stacks them up) - don't replace existing charges
        // Cap at maximum to be safe
        nitroChargesStored = Mathf.Min(nitroChargesStored + charges, nitroMaxCharges);
    }

    private void ActivateNitro()
    {
        if (nitroChargesStored <= 0) return; // No charges available

        int chargesUsed = nitroChargesStored;

        // Calculate boost power based on charges used
        if (chargesUsed == 1)
        {
            currentNitroBoostPower = nitroBoostPowerLevel1;
        }
        else if (chargesUsed == 2)
        {
            currentNitroBoostPower = nitroBoostPowerLevel2;
        }
        else if (chargesUsed >= 3)
        {
            currentNitroBoostPower = nitroBoostPowerLevel3;
        }

        // Consume all charges
        nitroChargesStored = 0;

        // Activate nitro
        isNitroActive = true;
        nitroActiveTimeRemaining = nitroActiveDuration;
        currentMaxSpeed = nitroMaxSpeed;

        // Play nitro sound effect
        PlayNitroSound();

        // Spawn appropriate VFX based on charges used
        SpawnNitroVFX(chargesUsed);
    }

    private void DeactivateNitro()
    {
        isNitroActive = false;
        currentMaxSpeed = maxSpeed;
        currentNitroBoostPower = 0f;

        // Destroy VFX when nitro deactivates
        DestroyNitroVFX();
    }

    private void SpawnNitroVFX(int nitroLevel)
    {
        // Destroy any existing VFX first
        DestroyNitroVFX();

        // Select the appropriate VFX prefab based on nitro level
        GameObject vfxPrefab = null;
        if (nitroLevel == 1 && nitroVFXLevel1 != null)
        {
            vfxPrefab = nitroVFXLevel1;
        }
        else if (nitroLevel == 2 && nitroVFXLevel2 != null)
        {
            vfxPrefab = nitroVFXLevel2;
        }
        else if (nitroLevel >= 3 && nitroVFXLevel3 != null)
        {
            vfxPrefab = nitroVFXLevel3;
        }

        // If no VFX prefab is assigned for this level, don't spawn anything
        if (vfxPrefab == null)
        {
            return;
        }

        // Determine local spawn position and rotation relative to car
        Vector3 localSpawnPosition;
        Quaternion localSpawnRotation;

        if (nitroVFXSpawnPoint != null)
        {
            // Use assigned spawn point's local position/rotation
            localSpawnPosition = nitroVFXSpawnPoint.localPosition;
            localSpawnRotation = nitroVFXSpawnPoint.localRotation;
        }
        else
        {
            // Calculate position between rear wheels (center rear of car) in local space
            if (rearLeftWheel != null && rearRightWheel != null)
            {
                Vector3 rearCenterWorld = (rearLeftWheel.position + rearRightWheel.position) / 2f;
                localSpawnPosition = transform.InverseTransformPoint(rearCenterWorld);
            }
            else
            {
                // Fallback: spawn behind the car in local space
                localSpawnPosition = new Vector3(0, 0, -1.5f);
            }
            localSpawnRotation = Quaternion.identity;
        }

        // Instantiate the VFX as a child of the car
        currentNitroVFX = Instantiate(vfxPrefab, transform);
        currentNitroVFX.transform.localPosition = localSpawnPosition;
        currentNitroVFX.transform.localRotation = localSpawnRotation;

        // Apply scale to make VFX bigger or smaller
        currentNitroVFX.transform.localScale = Vector3.one * nitroVFXScale;
    }

    private void DestroyNitroVFX()
    {
        if (currentNitroVFX != null)
        {
            Destroy(currentNitroVFX);
            currentNitroVFX = null;
        }
    }

    void FixedUpdate()
    {
        Movement();
        Steering();
        TerrainConforming();
        WheelsRotation();
    }

    private void Movement()
    {
        if (inputProvider == null) return;

        // check nitro input (edge detection - press to activate all charges)
        if (inputProvider.Nitro && !isNitroActive)
        {
            ActivateNitro();
        }

        // Apply nitro boost if active
        if (isNitroActive)
        {
            rb.AddForce(transform.forward * currentNitroBoostPower, ForceMode.Acceleration);
        }

        // get input values separately
        bool throttleInput = inputProvider.Throttle;       // throttle (true/false)
        bool brakeInput = inputProvider.Brake;            // brake (true/false)
        bool handbrakeInput = inputProvider.Handbrake;    // handbrake (true/false)
        bool driftInput = inputProvider.Drift;            // drift (Mario Kart style)

        // reset drag to normal
        rb.drag = dragAmount;

        // Apply off-track penalty if enabled and car is off-track
        if (enableOffTrackPenalty && isOffTrack)
        {
            ApplyOffTrackPenalty();
        }

        // handle handbrake - highest priority (emergency stop/slide)
        if (handbrakeInput)
        {
            ApplyHandbrake();
            return; // handbrake overrides all other movement
        }

        // handle drift (Mario Kart style) - smooth drifting around corners
        if (driftInput)
        {
            ApplyDrift();
            // Don't apply throttle or brake while drifting - maintain constant speed
        }
        else
        {
            // Only apply throttle/brake when NOT drifting
            float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);

            // handle brake (digital brake) or reverse depending on current movement
            if (brakeInput)
            {
                if (forwardSpeed > reverseActivationSpeed)
                {
                    ApplyBrake();
                }
                else
                {
                    ApplyReverseThrust();
                }
            }

            // handle thrust (digital throttle)
            if (throttleInput)
            {
                ApplyThrust();
            }
        }

        // limit max speed
        LimitMaxSpeed();
    }

    private void Steering()
    {
        if (inputProvider == null) return;

        // Don't apply normal steering if drifting - drift has its own steering
        if (isDrifting) return;

        float steerInput = inputProvider.Steer;
        float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);

        // When backing up, steering inputs feel reversed in the real world
        if (forwardSpeed < -reverseActivationSpeed)
        {
            steerInput = -steerInput;
        }

        // Direct rotation control instead of torque - provides immediate steering response
        // Calculate desired angular velocity based on input and speed
        // Reduced turn angle for normal driving (not drifting)
        float currentSpeed = rb.velocity.magnitude;
        float speedFactor = Mathf.Clamp01(currentSpeed / maxSpeed); // Steering is more effective at higher speeds

        // Balanced turn speed for normal driving - reduced but still responsive
        float normalTurnSpeed = turnSpeed * 0.75f; // 75% of turn speed for normal driving
        float targetAngularVelocity = steerInput * normalTurnSpeed * (0.3f + speedFactor * 0.7f);
        rb.angularVelocity = new Vector3(rb.angularVelocity.x, targetAngularVelocity, rb.angularVelocity.z);
    }

    private void TerrainConforming()
    {
        // cast rays down from the four corners of the car, to determine ground slope
        Vector3 frontLeft = transform.position + transform.TransformDirection(-0.5f, 0, 1f);
        Vector3 frontRight = transform.position + transform.TransformDirection(0.5f, 0, 1f);
        Vector3 rearLeft = transform.position + transform.TransformDirection(-0.5f, 0, -1f);
        Vector3 rearRight = transform.position + transform.TransformDirection(0.5f, 0, -1f);

        // store the hit counts & the hit-point-infos (out-parameters)
        RaycastHit hitFL, hitFR, hitRL, hitRR;
        // Use QueryTriggerInteraction.Ignore to ignore triggers and only hit the topmost collider
        bool frontLeftHit = Physics.Raycast(frontLeft, Vector3.down, out hitFL, raycastDistance, groundLayerMask, QueryTriggerInteraction.Ignore);
        bool frontRightHit = Physics.Raycast(frontRight, Vector3.down, out hitFR, raycastDistance, groundLayerMask, QueryTriggerInteraction.Ignore);
        bool rearLeftHit = Physics.Raycast(rearLeft, Vector3.down, out hitRL, raycastDistance, groundLayerMask, QueryTriggerInteraction.Ignore);
        bool rearRightHit = Physics.Raycast(rearRight, Vector3.down, out hitRR, raycastDistance, groundLayerMask, QueryTriggerInteraction.Ignore);

        // -------------- Off-Track Detection --------------
        // Check if car is on track or off-track by checking all raycast hits
        // The raycast will hit the FIRST collider (track or terrain), so we check what it hit
        if (enableOffTrackPenalty)
        {
            DetectOffTrack(frontLeftHit, hitFL, frontRightHit, hitFR, rearLeftHit, hitRL, rearRightHit, hitRR);
        }

        // if we got at least 3 hit points, we can calculate the terrain normal
        int hitCount = (frontLeftHit ? 1 : 0) + (frontRightHit ? 1 : 0) + (rearLeftHit ? 1 : 0) + (rearRightHit ? 1 : 0);
        if (hitCount >= 3)
        {
            Vector3 avgNormal = Vector3.zero;
            int normalCount = 0;
            if (frontLeftHit) { avgNormal += hitFL.normal; normalCount++; }
            if (frontRightHit) { avgNormal += hitFR.normal; normalCount++; }
            if (rearLeftHit) { avgNormal += hitRL.normal; normalCount++; }
            if (rearRightHit) { avgNormal += hitRR.normal; normalCount++; }
            avgNormal /= normalCount;

            // calculate the desired rotation to align with terrain
            Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, avgNormal) * Quaternion.Euler(0, transform.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothing * Time.fixedDeltaTime);
        }
        else
        {
            // Car is in the air (not enough ground contact) - apply extra downward force to prevent floating
            rb.AddForce(Vector3.down * extraGravityForce, ForceMode.Acceleration);
        }
    }

    private void DetectOffTrack(bool frontLeftHit, RaycastHit hitFL, bool frontRightHit, RaycastHit hitFR,
                                bool rearLeftHit, RaycastHit hitRL, bool rearRightHit, RaycastHit hitRR)
    {
        // Count how many wheels are off-track
        int offTrackWheelCount = 0;
        int totalWheelsChecked = 0;

        // Check each wheel raycast hit
        if (frontLeftHit)
        {
            totalWheelsChecked++;
            if (IsOffTrackSurface(hitFL))
            {
                offTrackWheelCount++;
            }
        }

        if (frontRightHit)
        {
            totalWheelsChecked++;
            if (IsOffTrackSurface(hitFR))
            {
                offTrackWheelCount++;
            }
        }

        if (rearLeftHit)
        {
            totalWheelsChecked++;
            if (IsOffTrackSurface(hitRL))
            {
                offTrackWheelCount++;
            }
        }

        if (rearRightHit)
        {
            totalWheelsChecked++;
            if (IsOffTrackSurface(hitRR))
            {
                offTrackWheelCount++;
            }
        }

        // Car is considered off-track if at least 2 wheels are off-track
        // This prevents false positives when slightly touching the edge
        if (totalWheelsChecked >= 2)
        {
            isOffTrack = offTrackWheelCount >= 2;
        }
        else
        {
            // Not enough ground contact - assume off-track for safety
            isOffTrack = true;
        }
    }

    private bool IsOffTrackSurface(RaycastHit hit)
    {
        // WHITELIST approach: Check if the object IS on the track (inverse logic)
        // This prevents issues when track is built on top of terrain

        // DEBUG: Uncomment to see what raycast is hitting
        // Debug.Log($"Raycast hit: {hit.collider.gameObject.name}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}, Tag: {hit.collider.gameObject.tag}");

        // First check by tag for off-track surfaces (explicit off-track marking)
        string surfaceTag = hit.collider.gameObject.tag;

        if (!string.IsNullOrEmpty(offTrackTag) && surfaceTag == offTrackTag)
        {
            return true; // Explicitly marked as off-track
        }

        // Then check if trackLayerMask is set
        if (trackLayerMask != 0)
        {
            // If the object's layer IS in the track layer mask, it's ON track (return false = not off-track)
            // If the object's layer is NOT in the track layer mask, it's OFF track (return true = off-track)
            int hitLayer = hit.collider.gameObject.layer;
            bool isOnTrackLayer = (trackLayerMask & (1 << hitLayer)) != 0;

            // If it's on the track layer, it's NOT off-track
            if (isOnTrackLayer)
            {
                return false; // On track
            }
            else
            {
                return true; // Not on track layer = off-track
            }
        }

        // If no trackLayerMask is set and no off-track tags matched, assume on track
        return false;
    }

    private void WheelsRotation()
    {
        if (inputProvider == null) return;

        bool throttleInput = inputProvider.Throttle;
        bool brakeInput = inputProvider.Brake;
        bool handbrakeInput = inputProvider.Handbrake;
        float wheelRotationAmount = CalculateWheelRotationSpeed(throttleInput, brakeInput, handbrakeInput);

        // calculate steering angle
        float steerInput = inputProvider.Steer;
        float steerWheelAngle = steerInput * maxSteerAngle;

        // update accumulated wheel rotations (to avoid euler angle issues)
        frontLeftWheelRotation += wheelRotationAmount;
        frontRightWheelRotation += wheelRotationAmount;
        rearLeftWheelRotation += wheelRotationAmount;
        rearRightWheelRotation += wheelRotationAmount;

        // rotate rear wheels (ONLY forward rotation)
        rearLeftWheel.localRotation = Quaternion.Euler(rearLeftWheelRotation, 0, 0);
        rearRightWheel.localRotation = Quaternion.Euler(rearRightWheelRotation, 0, 0);

        // rotate front wheels (forward + steering)
        frontLeftWheel.localRotation = Quaternion.Euler(frontLeftWheelRotation, steerWheelAngle, 0);
        frontRightWheel.localRotation = Quaternion.Euler(frontRightWheelRotation, steerWheelAngle, 0);
    }


    // ------------------Helper Functions------------------
    private float CalculateWheelRotationSpeed(bool throttleInput, bool brakeInput, bool handbrakeInput)
    {
        // get current speed for wheel rotation
        float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
        float baseRotationSpeed = Mathf.Abs(forwardSpeed) * this.wheelRotationSpeed * Time.fixedDeltaTime;

        // -------------wheels barely rotate but have slight sliding during brakes-------------
        if (handbrakeInput) // The HIGHEST priority!!
        {
            return baseRotationSpeed * 0.6f * Mathf.Sign(forwardSpeed);
        }
        if (brakeInput)
        {
            return baseRotationSpeed * 0.8f * Mathf.Sign(forwardSpeed);
        }
        // -------------wheels barely rotate but have slight sliding during brakes-------------


        // normal case: adjust wheel rotation based on throttle input
        if (throttleInput)
        {
            return baseRotationSpeed * Mathf.Sign(forwardSpeed);
        }

        // neutral: wheels gradually stop rotating
        return baseRotationSpeed * 0.95f * Mathf.Sign(forwardSpeed);
    }

    private void ApplyThrust()
    {
        float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);

        // J-turn boost
        if (forwardSpeed < 0)
        {
            Vector3 reverseBoost = transform.forward * acceleration * 1.5f;  // full boost for reversing
            rb.AddForce(reverseBoost, ForceMode.Acceleration);
        }
        else
        {
            Vector3 forceDirection = transform.forward * acceleration;
            rb.AddForce(forceDirection, ForceMode.Acceleration);
        }
    }

    private void ApplyReverseThrust()
    {
        // Apply reverse acceleration in world space so the car backs up realistically
        Vector3 reverseForce = -transform.forward * reverseAcceleration;
        rb.AddForce(reverseForce, ForceMode.Acceleration);
    }

    private void ApplyOffTrackPenalty()
    {
        // Increase drag to slow down the car when off-track
        rb.drag = dragAmount + offTrackDragIncrease;

        // Apply a deceleration force to slow the car down
        float currentSpeed = rb.velocity.magnitude;
        if (currentSpeed > offTrackMaxSpeed)
        {
            // Apply stronger deceleration when exceeding off-track max speed
            Vector3 decelerationForce = -rb.velocity.normalized * acceleration * 0.5f * (1f - offTrackDecelerationMultiplier);
            rb.AddForce(decelerationForce, ForceMode.Acceleration);
        }
        else if (currentSpeed > 0)
        {
            // Apply gradual deceleration when within off-track speed range --> to keep a steady slow speed with rare speed acceleration/deceleration
            Vector3 decelerationForce = -rb.velocity.normalized * acceleration * 0.2f * (1f - offTrackDecelerationMultiplier);
            rb.AddForce(decelerationForce, ForceMode.Acceleration);
        }
    }

    private void ApplyBrake()
    {
        rb.drag = dragAmount + brakeDrag;

        Vector3 brakeForceDirection = -transform.forward * brakeForce;
        rb.AddForce(brakeForceDirection, ForceMode.Acceleration);
    }

    private void ApplyHandbrake()
    {
        float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
        if (Mathf.Abs(forwardSpeed) > 1.0f)
        {
            rb.AddForce(-transform.forward * handbrakeForce * 0.6f, ForceMode.Acceleration);
        }

        // CRITICAL FOR DRIFT: Reduce the drag to allow sliding
        // Lower drag = easier to slide sideways
        rb.drag = dragAmount * 0.3f;

        // Add a slight OUTWARD force to help initiate the drift
        float steerInput = inputProvider.Steer;
        if (Mathf.Abs(steerInput) > 0.1f && Mathf.Abs(forwardSpeed) > 5f)
        {
            // Push the rear out in the opposite direction of steering
            Vector3 driftKickForce = -transform.right * steerInput * forwardSpeed * 0.1f;
            rb.AddForce(driftKickForce, ForceMode.Acceleration);
        }

        float driftAngularVelocity = steerInput * turnSpeed * 4f;
        rb.angularVelocity = new Vector3(rb.angularVelocity.x, driftAngularVelocity, rb.angularVelocity.z);
    }

    private void ApplyDrift()
    {
        float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
        float currentSpeed = rb.velocity.magnitude;

        // Only drift when moving forward and at a reasonable speed (Mario Kart style)
        if (forwardSpeed < 3f)
        {
            // Not fast enough to drift effectively
            return;
        }

        float steerInput = inputProvider.Steer;

        // Anti-cheat: Lock steering direction - prevent opposite direction input while drifting
        // If player started drifting with A (left), they cannot use D (right) until they release Space
        if (driftInitialSteerDirection != 0f)
        {
            float currentSteerDirection = Mathf.Sign(steerInput);

            // If trying to steer in opposite direction, ignore the input
            if (currentSteerDirection != 0f &&
                Mathf.Sign(driftInitialSteerDirection) != Mathf.Sign(currentSteerDirection))
            {
                // Block opposite direction input - player must release Space to change direction
                steerInput = 0f; // No steering force in opposite direction
            }
            // Allow steering in the same direction or neutral
            else if (Mathf.Sign(driftInitialSteerDirection) == Mathf.Sign(currentSteerDirection))
            {
                // Same direction - allow steering
                // steerInput remains unchanged
            }
        }

        // Mario Kart style drift: blend between initial direction and current facing direction
        // This creates smooth arc turns - drifts toward where it was facing AND where it's facing now

        // Calculate blend factor based on drift duration
        // Start with initial direction, gradually blend toward current forward direction
        float blendFactor = Mathf.Clamp01(driftDuration / driftDirectionBlendTime);

        // Blend between initial drift direction and current forward direction
        // At start: 100% initial direction
        // Over time: gradually shifts toward current forward direction
        // This creates the smooth arc turn effect
        Vector3 blendedDirection = Vector3.Slerp(driftStartDirection, transform.forward, blendFactor);

        // Normalize to ensure consistent magnitude
        blendedDirection.Normalize();

        // Mario Kart style drift: smooth, controlled sliding with angle turning
        // Reduce drag more for better sliding - allows sideways movement
        rb.drag = dragAmount * 0.4f;

        // Maintain speed in blended direction - creates smooth arc drift
        // The car drifts toward a blend of initial direction and current facing
        Vector3 targetVelocity = blendedDirection * driftStartSpeed;

        // Apply force to maintain speed in blended direction (creates arc turn)
        Vector3 velocityDifference = targetVelocity - rb.velocity;
        Vector3 maintenanceForce = velocityDifference * acceleration * 0.6f;
        rb.AddForce(maintenanceForce, ForceMode.Acceleration);

        // Enhanced steering during drift - allows turning at perfect angles
        if (Mathf.Abs(steerInput) > 0.1f)
        {
            // Enhanced angular velocity for bigger turns while drifting
            float driftAngularVelocity = steerInput * turnSpeed * 1.25f;
            rb.angularVelocity = new Vector3(rb.angularVelocity.x, driftAngularVelocity, rb.angularVelocity.z);

            // Stronger outward force to create Mario Kart style drift angle
            // This makes the rear slide out more, creating the classic drift effect
            Vector3 driftKickForce = -transform.right * steerInput * forwardSpeed * 0.15f;
            rb.AddForce(driftKickForce, ForceMode.Acceleration);

            // Add forward momentum in the current facing direction
            // This helps the car gradually turn toward the new direction
            Vector3 forwardMomentum = transform.forward * forwardSpeed * 0.08f;
            rb.AddForce(forwardMomentum, ForceMode.Acceleration);
        }
        else
        {
            // Even when not actively steering, maintain speed during drift
            // This ensures no speed loss during drift
        }
    }

    private void LimitMaxSpeed()
    {
        float currentSpeed = rb.velocity.magnitude;
        float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);

        // Soft cap for nitroMaxSpeed -> maxSpeed
        if (currentSpeed > currentMaxSpeed)
        {
            float excessSpeed = currentSpeed - currentMaxSpeed;
            float resistanceFactor = excessSpeed / currentSpeed;

            // apply counter force to limit speed
            Vector3 resistanceForce = -rb.velocity.normalized * resistanceFactor * acceleration * 0.5f;
            rb.AddForce(resistanceForce, ForceMode.Acceleration);
        }

        // Hard cap for both
        if (currentSpeed > nitroMaxSpeed)
        {
            rb.velocity = rb.velocity.normalized * nitroMaxSpeed;
            forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
        }

        // Dedicated reverse clamp so backing up stays slower than forward driving
        if (forwardSpeed < -reverseMaxSpeed)
        {
            Vector3 forwardComponent = Vector3.Project(rb.velocity, transform.forward);
            Vector3 lateralComponent = rb.velocity - forwardComponent;
            Vector3 clampedForward = -transform.forward * reverseMaxSpeed;
            rb.velocity = lateralComponent + clampedForward;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if the collided object has a tag that should cause slowdown
        bool shouldSlowdown = false;
        string objectTag = collision.gameObject.tag;

        foreach (string tag in collisionTags)
        {
            // Use string comparison instead of CompareTag to avoid errors
            if (!string.IsNullOrEmpty(tag) && objectTag == tag)
            {
                shouldSlowdown = true;
                break;
            }
        }

        // Only apply slowdown if it's a tagged object
        if (shouldSlowdown)
        {
            // Calculate collision impact force
            float impactForce = collision.relativeVelocity.magnitude;

            // Only apply slowdown if collision force is above threshold
            if (impactForce > minCollisionForce)
            {
                // Calculate slowdown factor based on impact force
                // Harder hits = more slowdown (capped at collisionSlowdownMultiplier)
                float slowdownFactor = Mathf.Clamp01(impactForce / 30f) * collisionSlowdownMultiplier;

                // Ensure minimum speed is maintained so car can still move
                float currentSpeed = rb.velocity.magnitude;
                float minSpeed = 2f; // Minimum speed to maintain after collision

                // Only reduce speed if it's above minimum threshold
                if (currentSpeed > minSpeed)
                {
                    // Reduce current velocity but ensure it doesn't go below minimum
                    Vector3 newVelocity = rb.velocity * (1f - slowdownFactor);
                    if (newVelocity.magnitude < minSpeed)
                    {
                        newVelocity = newVelocity.normalized * minSpeed;
                    }
                    rb.velocity = newVelocity;
                }

                // Reduce angular velocity to prevent spinning out too much
                rb.angularVelocity *= 0.7f;

                // Play crash sound effect
                PlayCrashSound();

                // Start bounce back effect
                StartBounceBack(collision);
            }
        }
    }

    private void StartBounceBack(Collision collision)
    {
        // Calculate bounce direction (away from collision point)
        Vector3 collisionPoint = collision.contacts[0].point;
        Vector3 carPosition = transform.position;
        bounceDirection = (carPosition - collisionPoint).normalized;

        // Start bounce effect
        isBouncing = true;
        bounceTimer = bounceBackDuration;
    }

    private void UpdateBounceBack()
    {
        if (isBouncing)
        {
            bounceTimer -= Time.deltaTime;

            if (bounceTimer > 0f)
            {
                // Apply bounce force
                float bounceStrength = bounceTimer / bounceBackDuration; // Fade out over time
                Vector3 bounceForce = bounceDirection * bounceBackForce * bounceStrength;
                rb.AddForce(bounceForce, ForceMode.Acceleration);
            }
            else
            {
                // End bounce effect
                isBouncing = false;
                bounceTimer = 0f;
            }
        }
    }

    // -------------- Audio Helper Methods --------------
    private void PlayCrashSound()
    {
        if (audioSource != null && crashSound != null)
        {
            // Only play if not already playing a crash sound (prevents stacking/overlapping crash sounds)
            if (!isCrashSoundPlaying)
            {
                audioSource.PlayOneShot(crashSound, crashSoundVolume);
                isCrashSoundPlaying = true;
                // Start coroutine to reset flag after sound finishes
                StartCoroutine(ResetCrashSoundFlag(crashSound.length));
            }
        }
    }

    private IEnumerator ResetCrashSoundFlag(float soundDuration)
    {
        yield return new WaitForSeconds(soundDuration);
        isCrashSoundPlaying = false;
    }

    private void PlayNitroSound()
    {
        if (audioSource != null && nitroSound != null)
        {
            audioSource.PlayOneShot(nitroSound, nitroSoundVolume);
        }
    }
}
