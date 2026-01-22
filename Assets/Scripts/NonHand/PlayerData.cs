/// <summary>
/// PlayerData là "Quản lý hình ảnh" của nhân vật (Thứ đập vào mắt người chơi).
/// 
/// Tác dụng:
/// - Quản lý Giao diện: Hiển thị tên (Nickname), tô màu quần áo (Color/Material) cho từng người riêng biệt.
/// - Hiệu ứng hình ảnh: Xử lý việc tàng hình khi chết (Ghost) hoặc hiển thị mờ ảo đối với đồng bọn ma.
/// - Hoạt họa (Animation): Đồng bộ tốc độ chạy thực tế với hành động khua chân của mô hình 3D, giúp nhân vật đi lại tự nhiên.
/// 
/// Nếu thiếu script này:
/// - Game toàn "Tượng sáp": Nhân vật di chuyển nhưng chân không bước (trượt trên sàn).
/// - Mù màu & Vô danh: Tất cả người chơi đều chung một màu xám xịt, không có tên trên đầu, không biết ai là ai.
/// - Lỗi hiển thị Ma: Người sống có thể nhìn thấy người chết, hoặc người chết không thấy nhau, làm lộ hết bí mật game.
/// </summary>

using Helpers.Collections;
using UnityEngine;

namespace NonHand
{
	/// <summary>
	/// Behaviour that handles non-networked information for players such as animation location on the map.
	/// </summary>
	public class PlayerData : MonoBehaviour
	{
		PlayerObject pObj;

		public Animator anim;
		public Renderer[] modelParts;
		public Transform uiPoint;

		public float animationBlending = 5f;

		// WorldCanvasNickname nicknameUI;
		public Material BodyMaterial { get; private set; }
		public Material TrimMaterial { get; private set; }
		public Material VisorMaterial { get; private set; }

		float moveSpeed = 0;

		const string MOVE_SPEED_PARAMETER = "MoveSpeed";

		private void Awake()
		{
			pObj = GetComponent<PlayerObject>();

			// BodyMaterial = Instantiate(GameManager.rm.playerBodyMaterial);
			// TrimMaterial = Instantiate(GameManager.rm.playerTrimMaterial);
			// VisorMaterial = Instantiate(GameManager.rm.playerVisorMaterial);

			// modelParts.ForEach(ren =>
			// {
			// 	Material[] mats = ren.sharedMaterials;
			// 	for (int i = 0; i < mats.Length; i++)
			// 	{
			// 		if (mats[i] == GameManager.rm.playerBodyMaterial)
			// 			mats[i] = BodyMaterial;
			// 		else if (mats[i] == GameManager.rm.playerTrimMaterial)
			// 			mats[i] = TrimMaterial;
			// 		else if (mats[i] == GameManager.rm.playerVisorMaterial)
			// 			mats[i] = VisorMaterial;
			// 		else
			// 			Debug.LogWarning($"{ren} is using an unmanaged material {mats[i]}");
			// 	}
			// 	ren.sharedMaterials = mats;
			// });
			Debug.Log($"Finished initializing materials", gameObject);
		}

		public void SetNickname(string nickname)
		{
			// if (nicknameUI == null)
			// {
			// 	nicknameUI = Instantiate(
			// 		GameManager.rm.worldCanvasNicknamePrefab,
			// 		uiPoint.transform.position,
			// 		Quaternion.identity,
			// 		GameManager.im.nicknameHolder);
			// 	nicknameUI.target = uiPoint;
			// }
			// nicknameUI.worldNicknameText.text = nickname;
		}
		
		public void SetColour(Color col)
		{
			Debug.Log($"{pObj.Nickname} changed color <color=#{ColorUtility.ToHtmlStringRGB(col)}>\u2588</color>");
			BodyMaterial.color = col;
		}

		// public void SetGhost(bool on)
		// {
		// 	if (on)
		// 	{
		// 		if (PlayerMovement.Local.IsDead)
		// 		{
		// 			// semi-visible
		// 			modelParts.ForEach(m => m.enabled = true);
		// 			BodyMaterial.shader = GameManager.rm.ghostShader;
		// 			TrimMaterial.shader = GameManager.rm.ghostShader;
		// 			VisorMaterial.shader = GameManager.rm.ghostShader;
		// 		}
		// 		else
		// 		{
		// 			// full invisible
		// 			modelParts.ForEach(m => m.enabled = false);
		// 		}
		// 	}
		// 	else
		// 	{
		// 		modelParts.ForEach(m => m.enabled = true);
		// 		BodyMaterial.shader = GameManager.rm.playerBodyMaterial.shader;
		// 		TrimMaterial.shader = GameManager.rm.playerTrimMaterial.shader;
		// 		VisorMaterial.shader = GameManager.rm.playerVisorMaterial.shader;
		// 	}
		// }

		// internal void UpdateAnimation(PlayerMovement playerMovement)
		// {
		// 	moveSpeed = Mathf.Min(1f, Mathf.MoveTowards(moveSpeed, playerMovement.simpleCC.RealSpeed, Time.deltaTime * animationBlending));
		// 	anim.SetFloat(MOVE_SPEED_PARAMETER, moveSpeed);
		// }
	}
}