using UnityEngine;
using Debug = UnityEngine.Debug;
using UnityEngine.InputSystem;
using FMODUnity;

[RequireComponent(typeof(Rigidbody), typeof(ShipStats))]
public class ShipController : MonoBehaviour
{
    [Header("KAMERY")]
    [SerializeField] private bool isFPPMode = true;
    [SerializeField] private GameObject tppCameraObject;
    [SerializeField] private GameObject fppCameraObject;
    private Vector2 _accumulatedMouseDelta;

    [Header("TRYB LOTU")]
    [SerializeField] private bool flightAssist = false;
    [SerializeField] private float autoBrakeStrength = 2.0f;

    [Header("STEROWANIE")]
    [SerializeField] private float fppMouseSensitivity = 0.5f;
    [SerializeField] private float verticalAccelerationTime = 0.4f;
    public float forwardAccelerationTime = 1.0f; // Czas rozpędzania do przodu/tyłu
    public float maxOverallSpeed = 20f;

    [Header("WIZUALNY PRZECHYL")]
    [SerializeField] private Transform shipVisualModel;
    [SerializeField] private float maxRollAngle = 15f;
    [SerializeField] private float rollSmoothSpeed = 5f;

    [Header("STRZELANIE")]
    private HeavyKineticLauncher launcher;

    private Rigidbody rb;
    private ShipStats stats;

    private float currentVerticalThrust = 0f;
    private float verticalVelocityRef = 0f;

    public bool isInteractingWithUI = false;
    private float currentForwardThrust = 0f;
    private float forwardVelocityRef = 0f;
    private float currentVisualRoll = 0f;
    private float previousLoadPercent = -1f;
    private bool lowFuelWarningTriggered = false;

    private string FMOD_PARAM = "Blend_Filter";

    void Start()
    {
        launcher = GetComponent<HeavyKineticLauncher>();

        rb = GetComponent<Rigidbody>();
        stats = GetComponent<ShipStats>();

        rb.useGravity = false;

        ApplyCameraMode();
        UpdatePhysics();
    }

    void Update()
    {
        // 1. Zmiana kamery
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            isFPPMode = !isFPPMode;
            ApplyCameraMode();
            UpdatePhysics();
        }

        if (Mouse.current != null && !isInteractingWithUI)
            _accumulatedMouseDelta += Mouse.current.delta.ReadValue();

        // 2. Zmiana trybu lotu
        if (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame)
        {
            flightAssist = !flightAssist;
            UpdatePhysics();
        }


        // 3. Obsługa wagi i ładunku
        float currentLoadPercent = stats.GetMaxCargo() > 0 ? stats.CurrentCargo / stats.GetMaxCargo() : 0f;
        if (Mathf.Abs(currentLoadPercent - previousLoadPercent) > 0.001f)
        {
            UpdatePhysics();
            previousLoadPercent = currentLoadPercent;
        }

        // 4. strzelanie
        if (Mouse.current.leftButton.wasPressedThisFrame)
            launcher.TryFire();
        ChangeAudioFilter();
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.currentState != GameState.Exploration &&
            GameManager.Instance.currentState != GameState.Fighting)
        {
            return;
        }

        HandleMovement();
        CheckFuelWarning();
        ApplyDirectionalDamping();

        if (rb.linearVelocity.magnitude > maxOverallSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxOverallSpeed;
        }
    }

    private void ApplyCameraMode()
    {
        if (tppCameraObject != null) tppCameraObject.SetActive(!isFPPMode);
        if (fppCameraObject != null) fppCameraObject.SetActive(isFPPMode);

        if (!isFPPMode)
        {
            Vector3 currentRotation = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, currentRotation.y, 0f);
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void UpdatePhysics()
    {
        rb.mass = stats.GetTotalMass();

        float loadRatio = stats.GetMaxCargo() > 0 ? stats.CurrentCargo / stats.GetMaxCargo() : 0f;

        rb.angularDamping = Mathf.Lerp(1.5f, 0.9f, loadRatio);
        rb.linearDamping = 0f;

        if (isFPPMode)
            rb.constraints = RigidbodyConstraints.None;
        else
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    // Asystent lotu tylko dla lotu naprzod
    private void ApplyDirectionalDamping()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);

        float loadRatio = stats.GetMaxCargo() > 0 ? stats.CurrentCargo / stats.GetMaxCargo() : 0f;
        float loadMultiplier = Mathf.Lerp(1f, 0.1f, loadRatio);
        float currentDrag = autoBrakeStrength * loadMultiplier;

        float dragX = localVelocity.x * currentDrag;
        float dragY = localVelocity.y * currentDrag;

        float dragZ = flightAssist ? (localVelocity.z * currentDrag) : 0f;

        rb.AddRelativeForce(new Vector3(-dragX, -dragY, -dragZ), ForceMode.Acceleration);
    }

    void HandleMovement()
    {
        float gasInput = 0f;
        float turnInput = 0f;
        float rollInput = 0f;
        float verticalInput = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) gasInput = 1f;
            if (Keyboard.current.sKey.isPressed) gasInput = -1f;

            if (Keyboard.current.aKey.isPressed) turnInput = -1f;
            if (Keyboard.current.dKey.isPressed) turnInput = 1f;

            if (Keyboard.current.qKey.isPressed) rollInput = 1f;
            if (Keyboard.current.eKey.isPressed) rollInput = -1f;

            if (Keyboard.current.spaceKey.isPressed) verticalInput = 1f;
            if (Keyboard.current.leftShiftKey.isPressed) verticalInput = -1f;
        }

        bool hasFuel = stats.CurrentEnergy > 0f;
        float currentPerformance = hasFuel ? 1f : stats.EmergencySpeedMultiplier;

        float targetForwardThrust = 0f;
        if (gasInput > 0) targetForwardThrust = stats.MaxMainThrust * currentPerformance;
        else if (gasInput < 0) targetForwardThrust = -stats.BrakeThrust * currentPerformance;

        currentForwardThrust = Mathf.SmoothDamp(currentForwardThrust, targetForwardThrust, ref forwardVelocityRef, forwardAccelerationTime);

        if (Mathf.Abs(currentForwardThrust) > 10f)
        {
            rb.AddRelativeForce(Vector3.forward * currentForwardThrust);
        }

        float targetVerticalThrust = verticalInput * stats.LiftThrust * currentPerformance;
        currentVerticalThrust = Mathf.SmoothDamp(currentVerticalThrust, targetVerticalThrust, ref verticalVelocityRef, verticalAccelerationTime);

        if (Mathf.Abs(currentVerticalThrust) > 10f)
        {
            rb.AddRelativeForce(Vector3.up * currentVerticalThrust);
        }

        if (isFPPMode)
        {
            float mouseX = 0f, mouseY = 0f;
            if (Mouse.current != null)
            {
                mouseX = _accumulatedMouseDelta.x * fppMouseSensitivity * Time.fixedDeltaTime * 50f;
                mouseY = _accumulatedMouseDelta.y * fppMouseSensitivity * Time.fixedDeltaTime * 50f;
                _accumulatedMouseDelta = Vector2.zero;
            }

            float pitchForce = -mouseY * stats.ManeuverForce * currentPerformance;
            float yawForce = mouseX * stats.ManeuverForce * currentPerformance;
            float rollTorque = -rollInput * stats.RollForce * currentPerformance;

            rb.AddRelativeTorque(new Vector3(pitchForce, yawForce, rollTorque));
        }
        else
        {
            if (turnInput != 0)
            {
                rb.AddTorque(Vector3.up * turnInput * stats.ManeuverForce * currentPerformance);
            }

            float targetRoll = -rollInput * maxRollAngle;
            currentVisualRoll = Mathf.Lerp(currentVisualRoll, targetRoll, Time.fixedDeltaTime * rollSmoothSpeed);

            if (shipVisualModel != null)
            {
                shipVisualModel.localRotation = Quaternion.Euler(0f, 0f, currentVisualRoll);
            }
        }

        bool isMoving = gasInput != 0 || turnInput != 0 || verticalInput != 0 || rollInput != 0 || (isFPPMode && Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.1f);
        if (isMoving && hasFuel)
        {
            stats.UseEnergy(stats.NormalDrainRate * Time.fixedDeltaTime);
        }
    }

    private void CheckFuelWarning()
    {
        if (stats.CurrentEnergy <= stats.LowFuelThreshold && !lowFuelWarningTriggered && stats.CurrentEnergy > 0)
        {
            lowFuelWarningTriggered = true;
            Debug.LogWarning("<color=red><b>UWAGA: Niski poziom paliwa!</b></color>");
        }
        else if (stats.CurrentEnergy > stats.LowFuelThreshold && lowFuelWarningTriggered)
        {
            lowFuelWarningTriggered = false;
        }
    }

    private void ChangeAudioFilter()
    {
        FMOD.RESULT result = FMODUnity.RuntimeManager.StudioSystem.setParameterByName(FMOD_PARAM, isFPPMode ? 1.0f : 0.0f);

        Debug.Log(result);

        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"FMOD: Błąd ustawiania parametru {FMOD_PARAM}: {result}");
        }
    }
}
