using UnityEngine;

public class facePlayerOnAxis : MonoBehaviour
{
	private Transform playerCamera;

	public Transform turnAxis;

	private bool gotPlayer;

	private void Update()
	{
		if (!gotPlayer)
		{
			if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.gameHasStarted)
			{
				playerCamera = GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform;
				gotPlayer = true;
			}
		}
		else
		{
			base.transform.LookAt(playerCamera, turnAxis.up);
		}
	}
}
