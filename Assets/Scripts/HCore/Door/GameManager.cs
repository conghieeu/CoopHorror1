using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine;
using Unity.Netcode;
using QFSW.QC;
using Unity.Collections;

public class GameManager : NetworkBehaviour
{
    // NetworkVariable with Custom Data Type, INetworkSerializable
    struct myCustomData : INetworkSerializable
    {
        public int intValue;
        public float floatValue;
        public FixedString32Bytes stringValue;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref intValue);
            serializer.SerializeValue(ref floatValue);
            serializer.SerializeValue(ref stringValue);
        }
    }

    private NetworkVariable<myCustomData> customData = new NetworkVariable<myCustomData>(
        new myCustomData { intValue = 0, floatValue = 0f, stringValue = "Initial" },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    // on network spawn
    public override void OnNetworkSpawn()
    {
        customData.OnValueChanged += OnCustomDataChanged;
    }

    void OnCustomDataChanged(myCustomData previousValue, myCustomData newValue)
    {
        Debug.Log($"Custom Data Changed: IntValue={newValue.intValue}, FloatValue={newValue.floatValue}, StringValue={newValue.stringValue}");
    }

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

    [Command("/send-message-to-client-1")]
    public void SendMessageToClient1()
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { 1 } // id của client người nhận
            }
        };
        SendMessageToClient1ClientRpc(clientRpcParams);
    }

    // gửi message đến client người có id là 1
    [ClientRpc]
    public void SendMessageToClient1ClientRpc(ClientRpcParams clientRpcParams)
    {
        Debug.Log("Message from Server to Client 1");
    }
}
