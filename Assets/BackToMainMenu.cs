using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;

public class BackToMainMenu : MonoBehaviour
{
    // Метод для полного перезапуска игры
    public void RestartGame()
    {
        Debug.Log("Перезапуск игры...");

        // 1. Отключаем Fusion Runner, если он активен
        NetworkRunner runner = FindObjectOfType<NetworkRunner>();
        if (runner != null)
        {
            Debug.Log("Отключаем Fusion Runner...");
            runner.Shutdown(); // Полностью останавливает сетевую сессию
        }

        // 2. Загружаем первую сцену (сцену по умолчанию)
        int firstSceneIndex = 0; // Индекс первой сцены в Build Settings
        Debug.Log("Загрузка стартовой сцены...");
        SceneManager.LoadScene(firstSceneIndex);
    }
}