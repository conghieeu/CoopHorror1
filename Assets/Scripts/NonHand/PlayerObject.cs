/// <summary>
/// PlayerObject đại diện cho "Hồ sơ cá nhân" của từng người chơi trong môi trường mạng (Network Entity).
/// Nó lưu trữ và đồng bộ các dữ liệu đặc thù như: Nickname, Màu sắc, ID, và trạng thái sống/chết.
/// 
/// Tác dụng:
/// - Tự động đăng ký bản thân vào PlayerRegistry khi được sinh ra (Spawned), tạo mối liên kết giữa kết nối mạng và nhân vật game.
/// - Cung cấp biến tĩnh "Local" giúp Client truy cập ngay lập tức vào nhân vật của chính mình mà không cần tìm kiếm.
/// - Xử lý các logic gameplay quan trọng (Bị giết, RPC đổi tên) và đồng bộ visual (OnChangedRender) cho tất cả người chơi khác thấy.
/// 
/// Nếu thiếu script này:
/// - Game mất khái niệm "Người chơi": Chỉ có các object di chuyển vô hồn, không có tên tuổi hay dữ liệu gắn kèm.
/// - PlayerRegistry trở nên vô dụng vì không có đối tượng để quản lý.
/// - Client không thể xác định "Tôi là ai" (biến Local null), dẫn đến không thể điều khiển input hoặc tương tác UI.
/// </summary>

using Fusion;
using Photon.Voice.Fusion;
using UnityEngine;

namespace NonHand
{
	/// <summary>
	/// Network Behaviour that defines information for the players other than movement.
	/// </summary>
	public class PlayerObject : NetworkBehaviour
	{
		/// <summary>
		/// A static reference to the local player
		/// </summary>
		public static PlayerObject Local { get; private set; }

		[Networked]
		public PlayerRef Ref { get; set; }
		[Networked]
		public byte Index { get; set; }
		[Networked, OnChangedRender(nameof(NicknameChanged))]
		public NetworkString<_16> Nickname { get; set; }
		[Networked, OnChangedRender(nameof(ColorChanged))]
		public byte ColorIndex { get; set; }

		// public Color GetColor => GameManager.rm.playerColours[ColorIndex];
		// public string GetStyledNickname => $"<color=#{ColorUtility.ToHtmlStringRGB(GetColor)}>{Nickname}</color>";

		[field: Header("References"), SerializeField] public PlayerMovement Controller { get; private set; }
		[field: SerializeField] public VoiceNetworkObject VoiceObject { get; private set; }
		[field: SerializeField] public SphereCollider KillRadiusTrigger { get; private set; }

		[Networked, Tooltip("Which PlayerRef killed this player.")]
		public PlayerRef Killer { get; set; } = PlayerRef.None;

		public void Server_Init(PlayerRef pRef, byte index, byte color)
		{
			Debug.Assert(Runner.IsServer);

			Ref = pRef;
			Index = index;
			ColorIndex = color;
		}

		public override void Spawned()
		{
			base.Spawned();

			if (Object.HasStateAuthority)
			{
				PlayerRegistry.Server_Add(Runner, Object.InputAuthority, this);
			}

			if (Object.HasInputAuthority)
			{
				Local = this;
				Rpc_SetNickname(PlayerPrefs.GetString("nickname"));
			}

			// Sets the proper nicknae and color on spawn.
			NicknameChanged();
			ColorChanged();
		}

		[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
		void Rpc_SetNickname(string nick)
		{
			Nickname = nick;
		}

		[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
		public void Rpc_SetColor(byte c)
		{
			if (PlayerRegistry.IsColorAvailable(c))
				ColorIndex = c;
		}

		void NicknameChanged()
		{
			GetComponent<PlayerData>().SetNickname(Nickname.Value);
		}
		void ColorChanged()
		{
			// GetComponent<PlayerData>().SetColour(GetColor);
			// if (Local != null)
			// {
			// 	if (Local.Controller.activeInteractable is ColorKiosk ck)
			// 	{
			// 		ck.colorUI.Refresh();
			// 	}
			// }
		}

		internal void AttemptKill(PlayerRef killerInputAuthority)
		{
			// if (!Runner.IsServer || !HasStateAuthority)
			// {
			// 	Debug.LogWarning("Only the state authority and server can determine when to kill a player.");
			// 	return;
			// }

			// if (Killer != PlayerRef.None)
			// {
			// 	Debug.LogWarning($"{GetStyledNickname} is already dead.  Ignoring this call.");
			// 	return;
			// }

			// PlayerObject src = PlayerRegistry.GetPlayer(killerInputAuthority);

			// // Checks to see if the killer is in range by doing a simple Sees if the killer is in range by doing a simple sphere check.
			// //if (Vector3.SqrMagnitude(transform.position - Controller.cc.Collider.ClosestPoint(src.transform.position)) <= KillRadiusTrigger.radius * KillRadiusTrigger.radius)
			// {
			// 	Controller.IsDead = true;

			// 	var corpse = Runner.Spawn(GameManager.rm.deadPlayer.GetComponent<NetworkObject>(), transform.position, transform.rotation);
			// 	corpse.GetComponent<DeadPlayer>().Ref = Ref;

			// 	src.Controller.KillTimer = TickTimer.CreateFromSeconds(Runner, 30);

			// 	Debug.Log($"[SPOILER]\n\n{src.GetStyledNickname} killed {GetStyledNickname}");

			// 	Controller.Server_UpdateDeadState();

			// 	Killer = killerInputAuthority;

			// 	GameManager.OnPlayerKilled();
			//}
		}
	}
}