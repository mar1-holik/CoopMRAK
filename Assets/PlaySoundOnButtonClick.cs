using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySoundOnButtonClick : MonoBehaviour
{
    public AudioSource audioSource; // Источник звука

    // Метод для вызова при нажатии кнопки
    public void PlaySound()
    {
        if (audioSource != null)
        {
            audioSource.Play(); // Проигрывает звук
        }
        else
        {
            Debug.LogWarning("AudioSource не назначен!");
        }
    }
}
