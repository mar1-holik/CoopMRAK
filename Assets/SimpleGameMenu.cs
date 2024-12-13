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
            StartMenuCanvas.enabled = false; // ���������� ��������� ��������� ������
        }
        else
        {
            Debug.LogError("StartMenuCanvas �� ����� � ����������.");
        }
    }

    // ����� ��� ������ "�����"
    public void StartGame()
    {
        if (StartMenuCanvas != null)
        {
            StartMenuCanvas.enabled = true; // �������� ��������� ������
            gameObject.GetComponent<Canvas>().enabled = false; // ��������� ������� ������
        }
        else
        {
            Debug.LogError("StartMenuCanvas �� ����� � ����������.");
        }
    }

    // ����� ��� ������ "�����"
    public void ExitGame()
    {
        Debug.Log("����� �� ����");
        Application.Quit();
    }
}
