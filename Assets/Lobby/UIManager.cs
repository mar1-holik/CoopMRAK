using UnityEngine;
using Fusion;

public class UIManager : MonoBehaviour
{
    public NetworkRunner networkRunner; // Ссылка на NetworkRunner
    private const string RoomName = "MyFixedRoom"; // Фиксированное имя комнаты
    private const int MaxPlayers = 4; // Максимальное количество игроков

    // Метод для создания комнаты
    public async void OnCreateRoomButtonClicked()
    {
        if (networkRunner == null)
        {
            Debug.LogError("NetworkRunner не настроен.");
            return;
        }

        var startGameArgs = new StartGameArgs
        {
            GameMode = GameMode.Host,
            SessionName = RoomName, // Используем фиксированное имя
            PlayerCount = MaxPlayers
        };

        Debug.Log("Попытка создать комнату...");
        var result = await networkRunner.StartGame(startGameArgs);

        if (result.Ok)
        {
            Debug.Log($"Комната '{RoomName}' успешно создана.");
        }
        else
        {
            Debug.LogError($"Ошибка при создании комнаты: {result.ShutdownReason}");
        }
    }

    // Метод для подключения к комнате
    public async void OnJoinRoomButtonClicked()
    {
        if (networkRunner == null)
        {
            Debug.LogError("NetworkRunner не настроен.");
            return;
        }

        var startGameArgs = new StartGameArgs
        {
            GameMode = GameMode.Client,
            SessionName = RoomName // Используем фиксированное имя
        };

        Debug.Log("Попытка подключиться к комнате...");
        var result = await networkRunner.StartGame(startGameArgs);

        if (result.Ok)
        {
            Debug.Log($"Успешно подключились к комнате '{RoomName}'.");
        }
        else
        {
            Debug.LogError($"Ошибка при подключении к комнате: {result.ShutdownReason}");
        }
    }
}
