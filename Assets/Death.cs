using UnityEngine;
using Fusion;

public class DeadZoneController : MonoBehaviour
{
    [SerializeField] private GameObject gameOverScreen; // UI панель экрана поражения

    private NetworkRunner _runner;

    // Этот метод вызывается из другого скрипта после создания NetworkRunner
    public void SetRunner(NetworkRunner runner)
    {
        _runner = runner;
        Debug.Log("NetworkRunner успешно установлен в DeadZoneController.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_runner == null)
        {
            Debug.LogWarning("NetworkRunner ещё не инициализирован.");
            return;
        }

        // Проверяем, есть ли у объекта компонент NetworkObject
        NetworkObject networkObject = other.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            // Получаем игрока, который контролирует этот объект
            PlayerRef playerRef = networkObject.InputAuthority;

            // Проверяем, является ли это локальным игроком
            if (_runner.LocalPlayer == playerRef)
            {
                ShowGameOverScreen();
            }
        }
    }

    private void ShowGameOverScreen()
    {
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true); // Показать экран поражения
        }

        Debug.Log("Игрок поражён!");
    }
}
