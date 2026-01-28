using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using QFSW.QC;

public class GameManager : MonoBehaviour
{
    // hàm start host với netcode
    [Command("/start-host")]
    public void StartHost()
    {
        // code để khởi động host với netcode
        NetworkManager.Singleton.StartHost();
    }

    // hàm start client với netcode
    [Command("/start-client")]
    public void StartClient()
    {
        // code để khởi động client với netcode
        NetworkManager.Singleton.StartClient();
    }

    // hàm shutdown với netcode
    [Command("/shutdown")]
    public void Shutdown()
    {
        // code để tắt mạng với netcode
        NetworkManager.Singleton.Shutdown();
    }

    // log ra thông tin người chơi - đây là người chơi số mấy - id là gì
    [Command("/log-player-info")]
    public void LogPlayerInfo()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            Debug.Log($"Player ID: {client.ClientId}, Is Host: {client.ClientId == NetworkManager.Singleton.LocalClientId}");
        }
    }

    // log ra thông tin id của người chơi hiện tại
    [Command("/log-my-id")]
    public void LogMyId()
    {
        Debug.Log($"My Client ID: {NetworkManager.Singleton.LocalClientId}");
    }
}
