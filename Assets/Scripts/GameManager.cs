using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using QFSW.QC;

public class GameManager : MonoBehaviour
{
    // hàm start host với netcode
    [Command("starthost")]
    public void StartHost()
    {
        // code để khởi động host với netcode
        NetworkManager.Singleton.StartHost();
    }

    // hàm start client với netcode
    [Command("startclient")]
    public void StartClient()
    {
        // code để khởi động client với netcode
        NetworkManager.Singleton.StartClient();
    }

    // hàm shutdown với netcode
    [Command("shutdown")]
    public void Shutdown()
    {
        // code để tắt mạng với netcode
        NetworkManager.Singleton.Shutdown();
    }
}
