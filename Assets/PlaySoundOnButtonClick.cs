using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySoundOnButtonClick : MonoBehaviour
{
    public AudioSource audioSource; // �������� �����

    // ����� ��� ������ ��� ������� ������
    public void PlaySound()
    {
        if (audioSource != null)
        {
            audioSource.Play(); // ����������� ����
        }
        else
        {
            Debug.LogWarning("AudioSource �� ��������!");
        }
    }
}
