using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Supervivencia")]
    public int currentLives = 3;      // Vidas iniciales según tus specs
    public int currentShields = 0;    // Escudo protector

    [Header("Economía")]
    public int coins = 0;             // Contador de monedas para cofres

    [Header("Stamina (Energía)")]
    public float maxStamina = 100f;   
    public float currentStamina;
    public float staminaRegenRate = 20f; // Puntos por segundo
    public float regenDelay = 3f;        // Delay de 3 segundos de inactividad

    private float _lastActionTime;     // Timestamp para el cooldown

    void Awake()
    {
        // Inicialización al arrancar
        currentStamina = maxStamina;
    }

    void Update()
    {
        // Lógica de Regeneración:
        // Si ha pasado el 'regenDelay' desde el último uso, recuperamos stamina
        if (Time.time - _lastActionTime >= regenDelay && currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        }
    }

    // Método para consumir energía (Correr/Saltar)
    public void UseStamina(float amount)
    {
        currentStamina -= amount;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        
        // Reseteamos el contador de inactividad cada vez que se usa
        _lastActionTime = Time.time; 
    }

    // Métodos de ayuda para los cofres (5, 15, 25 monedas)
    public void AddCoins(int amount)
    {
        coins += amount;
        Debug.Log("Monedas recolectadas: " + coins);
    }
}