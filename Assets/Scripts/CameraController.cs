using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform target;      // Player/objetivo a seguir
    public Camera cam;            // La cámara hija

    [Header("Orbit / Mouse")]
    public float mouseXSensitivity = 180f; // grados/seg
    public float mouseYSensitivity = 120f; // grados/seg
    public bool invertY = false;
    public float minPitch = -30f; // límite inferior
    public float maxPitch = 75f;  // límite superior

    [Header("Seguimiento")]
    public float followLerp = 12f;   // suavizado del rig al seguir al target
    public Vector3 targetOffset = new Vector3(0f, 1.6f, 0f); // altura a mirar (cabeza)

    [Header("Zoom")]
    public float minDistance = 2.0f;
    public float maxDistance = 7.0f;
    public float zoomSpeed = 6f;     // unidades por vuelta de rueda (aprox)
    public float zoomLerp = 10f;     // suavizado del zoom
    [Tooltip("Distancia inicial")]
    public float startDistance = 4.5f;

    [Header("Colisión de cámara")]
    public float collisionRadius = 0.2f;   // esfera para evitar clip
    public float collisionPadding = 0.2f;  // separarse un poco de la pared/suelo
    public LayerMask collisionMask = ~0;   // por defecto choca con todo
    [Tooltip("Ignora al propio player si es necesario (ponlo en 'Player' y excluye esa capa).")]

    // estado interno
    float yaw;      // rotación horizontal acumulada
    float pitch;    // rotación vertical acumulada
    float targetDistance; // distancia deseada por zoom
    float currentDistance; // distancia actual suavizada

    void Reset()
    {
        cam = GetComponentInChildren<Camera>();
    }

    void Start()
    {
        if (!target) { Debug.LogWarning("ThirdPersonCamera: Asigna 'target' (player)."); enabled = false; return; }
        if (!cam) cam = GetComponentInChildren<Camera>();

        targetDistance = Mathf.Clamp(startDistance, minDistance, maxDistance);
        currentDistance = targetDistance;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // --- Input de ratón (en grados/seg para frame-rate independence)
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * mouseXSensitivity * Time.deltaTime;
        float yDelta = (invertY ? mouseY : -mouseY) * mouseYSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch + yDelta, minPitch, maxPitch);

        // --- Zoom con rueda
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > Mathf.Epsilon)
        {
            targetDistance = Mathf.Clamp(
                targetDistance - scroll * zoomSpeed, // rueda positiva acerca
                minDistance,
                maxDistance
            );
        }

        // Bloquear/mostrar cursor con ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            }
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // 1) Seguir al target con suavizado (solo posición del rig)
        Vector3 desiredRigPos = Vector3.Lerp(transform.position, target.position, 1f - Mathf.Exp(-followLerp * Time.deltaTime));
        transform.position = desiredRigPos;

        // 2) Aplicar rotación de órbita al rig (yaw/pitch)
        Quaternion rigRot = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = rigRot;

        // 3) Calcular distancia suavizada (zoom)
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, 1f - Mathf.Exp(-zoomLerp * Time.deltaTime));

        // 4) Posición deseada de la cámara (en espacio del mundo)
        Vector3 focusPoint = target.position + targetOffset; // dónde mira
        Vector3 camDir = (transform.position - focusPoint).normalized; // desde foco hacia rig
        if (camDir.sqrMagnitude < 0.0001f) camDir = -transform.forward;

        Vector3 desiredCamPos = focusPoint + (-transform.forward) * currentDistance;

        // 5) Colisión: spherecast desde el foco hacia la cámara deseada
        Vector3 castDir = (desiredCamPos - focusPoint);
        float castDist = castDir.magnitude;
        if (castDist > 0.0001f) castDir /= castDist;

        bool hitSomething = Physics.SphereCast(
            focusPoint,
            collisionRadius,
            castDir,
            out RaycastHit hit,
            castDist,
            collisionMask,
            QueryTriggerInteraction.Ignore
        );

        Vector3 finalCamPos = desiredCamPos;
        if (hitSomething)
        {
            finalCamPos = hit.point - castDir * collisionPadding;
            // Si queda demasiado cerca, clamp
            float minClamp = minDistance * 0.35f;
            if (Vector3.Distance(finalCamPos, focusPoint) < minClamp)
                finalCamPos = focusPoint + castDir * minClamp;
        }

        // 6) Posicionar cámara hija y mirar al foco
        if (cam)
        {
            cam.transform.position = finalCamPos;
            cam.transform.rotation = Quaternion.LookRotation(focusPoint - cam.transform.position, Vector3.up);
        }
    }
}
