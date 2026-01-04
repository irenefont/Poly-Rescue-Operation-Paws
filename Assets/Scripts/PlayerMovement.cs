using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Configuración de Velocidad")]
    [Tooltip("Define la velocidad máxima de movimiento del jugador en metros por segundo.")]
    public float walkSpeed = 5f;
    
    [Tooltip("Define la velocidad máxima del jugador al correr en metros por segundo.")]
    public float runSpeed = 9f;      
    
    [Tooltip("Define la velocidad de rotación del jugador en radianes por segundo.")]
    public float rotationSpeed = 10f;

    
    [Header("Salto")]
    [Tooltip("Define la fuerza del salto del jugador.")]
    public float jumpForce = 5f;

    [Tooltip("Define el retardo del salto del jugador.")]
    public float jumpDelay = 0.2f;
    
    [Tooltip("Define la distancia desde la que se detecta el suelo para el salto.")]
    public float groundCheckDistance = 0.2f; 
    
    [Tooltip("Define la capa del terreno que detecta el suelo.")]
    public LayerMask groundMask;

    
    [FormerlySerializedAs("_stats")]
    [Header("Referencias")]
    [SerializeField] private PlayerStats stats;
    
    private Rigidbody _rb;
    private Vector3 _moveDirection;
    private bool _isGrounded;
    private bool _jumpQueued = false;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }
    
    void Awake() 
    {
        stats = GetComponent<PlayerStats>(); 
    }
    
    void Update()
    {
        // 1. CAPTURA DE INPUT (WASD y Flechas) [cite: 13, 14]
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // 2. CÁLCULO RELATIVO A LA CÁMARA
        // Tomamos el forward de la cámara y lo aplanamos (y=0) para no caminar hacia el suelo
        Vector3 camForward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 camRight = Camera.main.transform.right;

        _moveDirection = (vertical * camForward + horizontal * camRight).normalized;

        // 3. LÓGICA DE CORRER Y STAMINA
        bool isRunningInput = Input.GetKey(KeyCode.LeftControl); // Control para correr 
        bool canRun = isRunningInput && stats.currentStamina > 0 && _moveDirection.magnitude > 0.1f;

        if (canRun)
        {
            stats.UseStamina(15f * Time.deltaTime); // Consume stamina gradualmente 
        }

        // 4. DETECCIÓN DE SUELO Y SALTO 
        _isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance + 0.1f, groundMask);

        if (Input.GetKeyDown(KeyCode.Space) && _isGrounded && !_jumpQueued && stats.currentStamina >= 20f)
        {
            _jumpQueued = true;
            stats.UseStamina(20f); // El salto consume un bloque fijo
            StartCoroutine(JumpWithDelay());
        }
    }

    void FixedUpdate()
    {
        // 5. MOVIMIENTO FÍSICO (FixedUpdate es como el loop de un servidor: tiempo constante)
        if (_moveDirection.magnitude >= 0.1f)
        {
            // Determinar velocidad actual
            bool isRunning = Input.GetKey(KeyCode.LeftControl) && stats.currentStamina > 0;
            float currentSpeed = isRunning ? runSpeed : walkSpeed;

            // ROTACIÓN: Se orienta hacia donde camina (puedes verle la cara si pulsas S) 
            Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
            _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

            // MOVIMIENTO: Aplicamos posición física para que choque con paredes
            Vector3 move = _moveDirection * (currentSpeed * Time.fixedDeltaTime);
            _rb.MovePosition(_rb.position + move);
        }
    }

    private IEnumerator JumpWithDelay()
    {
        yield return new WaitForSeconds(jumpDelay);

        if (_isGrounded)
        {
            // Limpiamos velocidad vertical antes del impulso para saltos consistentes
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        _jumpQueued = false;
    }
}