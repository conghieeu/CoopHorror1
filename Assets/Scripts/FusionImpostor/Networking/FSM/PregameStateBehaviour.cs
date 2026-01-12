using Fusion;
using Fusion.Addons.FSM;

namespace FusionImpostor
{
	/// <summary>
	/// The state players enter when starting the game and before starting a main gameplay session and after a gameplay session.
	/// </summary>
	public class PregameStateBehaviour : StateBehaviour
	{
		protected override void OnEnterState()
		{
			PlayerRegistry.ForEach(pObj =>
			{
				pObj.Controller.IsDead = false;
				pObj.Controller.IsSuspect = false;
				pObj.Controller.cc.SetPosition(GameManager.Instance.preGameMapData.GetSpawnPosition(pObj.Index));
				pObj.Controller.RPC_EndInteraction();
				pObj.Controller.Server_UpdateDeadState();

				pObj.Killer = PlayerRef.None;
			});

			GameManager.rm.Purge();

			GameManager.Instance.TasksCompleted = 0;
		}
		protected override void OnEnterStateRender()
		{
			GameManager.Instance.taskDisplayList.Clear();
			GameManager.Instance.taskDisplayAmounts.Clear();

			GameManager.im.gameUI.InitPregame(Machine.Runner);
			GameManager.im.gameUI.colorUI.Init();

			if (TaskUI.ActiveUI) TaskUI.ActiveUI.CloseTask();
		}
	}
}
