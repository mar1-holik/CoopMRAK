using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SimpleGameMenu : MonoBehaviour
{
    public Canvas StartMenuCanvas;

    private void Start()
    {
        if (StartMenuCanvas != null)
        {
            StartMenuCanvas.enabled = false; // Изначально выключаем указанный канвас
        }
        else
        {
            Debug.LogError("StartMenuCanvas не задан в инспекторе.");
        }
    }

    // Метод для кнопки "Старт"
    public void StartGame()
    {
        if (StartMenuCanvas != null)
        {
            StartMenuCanvas.enabled = true; // Включаем указанный канвас
            gameObject.GetComponent<Canvas>().enabled = false; // Отключаем текущий канвас
        }
        else
        {
            Debug.LogError("StartMenuCanvas не задан в инспекторе.");
        }
    }

    // Метод для кнопки "Выход"
    public void ExitGame()
    {
        Debug.Log("Выход из игры");
        Application.Quit();
    }
}
