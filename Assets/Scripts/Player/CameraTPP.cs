using UnityEngine;
using UnityEngine.InputSystem;

public class kameraTpp : MonoBehaviour
{
    public Transform target;

    [Header("Przesuniêcie Celu")]
    public Vector3 targetOffset = new Vector3(0, 0f, 0);

    [Header("Ustawienia Orbity")]
    [SerializeField] private float sensitivity = 0.5f;
    [SerializeField] private float yMinLimit = -20f;
    [SerializeField] private float yMaxLimit = 80f;

    [Header("Ustawienia Zoomu")]
    [SerializeField] private float targetDistance = 15.0f;
    [SerializeField] private float minDistance = 5.0f;
    [SerializeField] private float maxDistance = 50.0f;
    [SerializeField] private float zoomSpeed = 2.0f;
    [SerializeField] private float zoomSmoothness = 10f;

    private float currentDistance;
    private float mouseX = 0.0f;
    private float mouseY = 0.0f;
    private float currentX = 0.0f;
    private float currentY = 0.0f;

    [SerializeField] private float smoothSpeed = 10f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentDistance = targetDistance;

        if (target != null)
        {
            Vector3 angles = target.eulerAngles;
            mouseX = angles.y;
            currentX = angles.y;

            mouseY = 15f;
            currentY = 15f;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        if (GameManager.Instance.currentState != GameState.Mining)
        {
            if (Mouse.current != null)
            {
                // --- OBRÓT ---
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                mouseX += mouseDelta.x * sensitivity * 0.5f;
                mouseY -= mouseDelta.y * sensitivity * 0.5f;
                mouseY = Mathf.Clamp(mouseY, yMinLimit, yMaxLimit);

                // --- ZOOM ---
                float scrollInput = Mouse.current.scroll.ReadValue().y;
                if (scrollInput != 0)
                {
                    float scrollDirection = Mathf.Sign(scrollInput);
                    targetDistance -= scrollDirection * zoomSpeed;
                    targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
                }
            }
        }

        currentX = Mathf.Lerp(currentX, mouseX, smoothSpeed * Time.deltaTime);
        currentY = Mathf.Lerp(currentY, mouseY, smoothSpeed * Time.deltaTime);
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, zoomSmoothness * Time.deltaTime);

        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        Vector3 lookAtPoint = target.position + targetOffset;
        Vector3 position = lookAtPoint - (rotation * Vector3.forward * currentDistance);

        transform.rotation = rotation;
        transform.position = position;
    }
}