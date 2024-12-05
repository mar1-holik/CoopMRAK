using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Image[] hearts; // Массив иконок сердец
    private int currentHealth;

    private void Start()
    {
        currentHealth = hearts.Length; // Устанавливаем текущее здоровье равным количеству сердец
    }

    // Метод для уменьшения здоровья
    public void DecreaseHealth()
    {
        if (currentHealth > 0)
        {
            currentHealth--; // Уменьшаем текущее здоровье

            // Скрываем текущее сердце
            hearts[currentHealth].enabled = false;
        }
    }
}
