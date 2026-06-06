using UnityEngine;
using Debug = UnityEngine.Debug;
using UnityEngine.InputSystem;

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
    public float forwardAccelerationTime = 1.0f; 
    public float maxOverallSpeed = 20f;

    [Header("WIZUALNY PRZECHYL")]
    [SerializeField] private Transform shipVisualModel;

    [Header("STRZELANIE")]
    private HeavyKineticLauncher launcher;

    private Rigidbody rb;
    private ShipStats stats;

    public bool isInteractingWithUI = false;
    private float currentForwardThrust = 0f;
    private float forwardVelocityRef = 0f;
    private float previousLoadPercent = -1f;
    private bool lowFuelWarningTriggered = false;

    void Start()
    {
        launcher = GetComponent<HeavyKineticLauncher>();

        if (GetComponent<SharedUIManager>() == null)
            gameObject.AddComponent<SharedUIManager>();

        if (GetComponent<PlayerAimHud>() == null)
            gameObject.AddComponent<PlayerAimHud>();
        if (GetComponent<CombatHUD>() == null)
            gameObject.AddComponent<CombatHUD>();
        if (GetComponent<MinimapHUD>() == null)
            gameObject.AddComponent<MinimapHUD>();
        if (GetComponent<GameMessageHUD>() == null)
            gameObject.AddComponent<GameMessageHUD>();
        if (GetComponent<PauseMenu>() == null)
            gameObject.AddComponent<PauseMenu>();
        if (GetComponent<DeathScreenUI>() == null)
            gameObject.AddComponent<DeathScreenUI>();

        rb = GetComponent<Rigidbody>();
        stats = GetComponent<ShipStats>();

        rb.useGravity = false;

        ApplyCameraMode();
        UpdatePhysics();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            isFPPMode = !isFPPMode;
            ApplyCameraMode();
            UpdatePhysics();
        }

        if (Mouse.current != null && !isInteractingWithUI)
            _accumulatedMouseDelta += Mouse.current.delta.ReadValue();

        if (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame)
        {
            flightAssist = !flightAssist;
            UpdatePhysics();
        }

        float currentLoadPercent = stats.GetMaxCargo() > 0 ? stats.CurrentCargo / stats.GetMaxCargo() : 0f;
        if (Mathf.Abs(currentLoadPercent - previousLoadPercent) > 0.001f)
        {
            UpdatePhysics();
            previousLoadPercent = currentLoadPercent;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame && launcher != null)
            launcher.TryFire();
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

        rb.constraints = RigidbodyConstraints.None;
    }

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
        float pitchInput = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) gasInput = 1f;
            if (Keyboard.current.sKey.isPressed) gasInput = -1f;

            if (Keyboard.current.aKey.isPressed) turnInput = -1f;
            if (Keyboard.current.dKey.isPressed) turnInput = 1f;

            if (Keyboard.current.qKey.isPressed) rollInput = -1f; 
            if (Keyboard.current.eKey.isPressed) rollInput = 1f;  

            if (Keyboard.current.spaceKey.isPressed) pitchInput = -1f; 
            if (Keyboard.current.leftShiftKey.isPressed) pitchInput = 1f;  
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

        float pitchForce = pitchInput * stats.ManeuverForce * currentPerformance;
        float yawForce = turnInput * stats.ManeuverForce * currentPerformance;
        float rollTorque = -rollInput * stats.RollForce * currentPerformance;

        float currentX = transform.localEulerAngles.x;
        if (currentX > 180f) currentX -= 360f;

        float currentZ = transform.localEulerAngles.z;
        if (currentZ > 180f) currentZ -= 360f;

        float maxPitch = 40f;
        float maxRoll = 30f;

        if (rollInput == 0f) rollTorque = -currentZ * stats.RollForce * 0.02f * currentPerformance;
        if (pitchInput == 0f && !isFPPMode) pitchForce = -currentX * stats.ManeuverForce * 0.02f * currentPerformance;

        if (isFPPMode)
        {
            float mouseX = 0f, mouseY = 0f;
            if (Mouse.current != null)
            {
                mouseX = _accumulatedMouseDelta.x * fppMouseSensitivity * Time.fixedDeltaTime * 50f;
                mouseY = _accumulatedMouseDelta.y * fppMouseSensitivity * Time.fixedDeltaTime * 50f;
                _accumulatedMouseDelta = Vector2.zero;
            }

            pitchForce += -mouseY * stats.ManeuverForce * currentPerformance;
            yawForce += mouseX * stats.ManeuverForce * currentPerformance;
        }

        if (currentX > maxPitch && pitchForce > 0) pitchForce = 0f;
        if (currentX < -maxPitch && pitchForce < 0) pitchForce = 0f;
        
        if (currentZ > maxRoll && rollTorque > 0) rollTorque = 0f;
        if (currentZ < -maxRoll && rollTorque < 0) rollTorque = 0f;

        rb.AddRelativeTorque(new Vector3(pitchForce, yawForce, rollTorque));

        Vector3 localEulers = transform.localEulerAngles;
        float x = localEulers.x; if (x > 180f) x -= 360f;
        float z = localEulers.z; if (z > 180f) z -= 360f;

        bool clamped = false;
        if (x > maxPitch) { x = maxPitch; clamped = true; }
        if (x < -maxPitch) { x = -maxPitch; clamped = true; }
        if (z > maxRoll) { z = maxRoll; clamped = true; }
        if (z < -maxRoll) { z = -maxRoll; clamped = true; }

        if (clamped)
        {
            transform.localEulerAngles = new Vector3(x, localEulers.y, z);
            Vector3 localAngularVel = transform.InverseTransformDirection(rb.angularVelocity);
            if ((x >= maxPitch && localAngularVel.x > 0) || (x <= -maxPitch && localAngularVel.x < 0)) 
                localAngularVel.x = 0f;
            if ((z >= maxRoll && localAngularVel.z > 0) || (z <= -maxRoll && localAngularVel.z < 0)) 
                localAngularVel.z = 0f;
            rb.angularVelocity = transform.TransformDirection(localAngularVel);
        }

        if (shipVisualModel != null)
        {
            shipVisualModel.localRotation = Quaternion.identity;
        }

        bool isMoving = gasInput != 0 || turnInput != 0 || pitchInput != 0 || rollInput != 0 || (isFPPMode && Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.1f);
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
            Debug.LogWarning("<color=red><b>UWAGA: Niski stan paliwa w zbiornikach!</b></color>");
        }
        else if (stats.CurrentEnergy > stats.LowFuelThreshold && lowFuelWarningTriggered)
        {
            lowFuelWarningTriggered = false;
        }
    }
}
