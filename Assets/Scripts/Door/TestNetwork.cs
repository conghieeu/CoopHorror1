using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using QFSW.QC;

public class TestNetwork : NetworkBehaviour
{
    NetworkVariable<int> testValue = new NetworkVariable<int>(0);

    // hàm bình thường gọi lênh server yêu cầu testValue thay đổi
    [Command("/change-test-value")]
    public void ChangeValue(int newValue)
    {
        Debug.Log("Client requested to change testValue to: " + newValue);
        RequestChangeValueServerRpc(newValue, default);
    }

    // CHO PHÉP CLIENT KHÔNG SỞ HỮU CŨNG GỌI ĐƯỢC
    [ServerRpc(RequireOwnership = false)]
    void RequestChangeValueServerRpc(int newValue, ServerRpcParams rpcParams = default)
    {
        // cấp phép quyền sỡ hữu với client với id 1 print 
        if (rpcParams.Receive.SenderClientId == 1)
        {
            testValue.Value = newValue;
            Debug.Log("Server changed testValue to: " + testValue.Value);
        }
        testValue.Value = newValue;
        Debug.Log("Server changed testValue to: " + testValue.Value);
    }

    // chủ sở hữu mới gọi được thường là host
    [ServerRpc]
    void RequestChangeValueServerRpc(int newValue)
    {
        testValue.Value = newValue;
        Debug.Log("Server changed testValue to: " + testValue.Value);
    }

    void OnEnable()
    {
        testValue.OnValueChanged += OnTestValueChanged;
    }

    void OnDisable()
    {
        testValue.OnValueChanged -= OnTestValueChanged;
    }

    void OnTestValueChanged(int oldValue, int newValue)
    {
        Debug.Log("testValue changed from " + oldValue + " to " + newValue);
        NotifyClientsClientRpc("testValue is now: " + newValue);
    }

    [ClientRpc]
    void NotifyClientsClientRpc(string message)
    {
        Debug.Log("ClientRpc Message: " + message + " on ClientId: " + NetworkManager.Singleton.LocalClientId);
    }
}
