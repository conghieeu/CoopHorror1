// /// <summary>
// /// MeetingStateBehaviour là logic xử lý riêng cho giai đoạn "Họp Khẩn & Bỏ Phiếu". 
// /// Nó hoạt động như một mắt xích trong hệ thống FSM (Máy trạng thái).
// /// 
// /// Tác dụng:
// /// - Khi bắt đầu họp (OnEnter):
// ///   + Server: Kích hoạt trạng thái "Đang Vote".
// ///   + Client: Bắt buộc đóng các cửa sổ Task, bật màn hình Bỏ phiếu lên, và quan trọng nhất là BẬT MIC (Voice Global) để mọi người tranh luận.
// /// - Khi kết thúc họp (OnExit):
// ///   + Server: Xóa dữ liệu người bấm nút, tắt trạng thái Vote.
// ///   + Client: Tắt màn hình Bỏ phiếu, TẮT MIC (để quay lại trạng thái im lặng làm nhiệm vụ).
// /// 
// /// Nếu thiếu script này:
// /// - Game bị "Loạn nhịp": Server chuyển sang họp nhưng Client vẫn để màn hình chơi game bình thường.
// /// - Mất tính năng cốt lõi: Vào họp nhưng không hiện bảng Vote, không bật Mic được để biện hộ.
// /// - Kẹt UI: Hết giờ họp rồi mà bảng Vote vẫn treo trên màn hình che hết tầm nhìn.
// /// </summary>
// using Fusion.Addons.FSM;

// namespace NonHand
// {
// 	/// <summary>
// 	/// Behaviour that handles the meeting state
// 	/// </summary>
// 	public class MeetingStateBehaviour : StateBehaviour
// 	{
// 		/// <summary>
// 		/// Được gọi khi bắt đầu vào trạng thái Họp (trên Server/Shared).
// 		/// Đánh dấu game đang trong trạng thái màn hình Vote để các logic khác (như move, interact) biết mà dừng lại.
// 		/// </summary>
// 		protected override void OnEnterState()
// 		{
// 			GameManager.Instance.VotingScreenActive = true;
// 		}

// 		/// <summary>
// 		/// Được gọi khi bắt đầu vào trạng thái Họp (trên Client - Render).
// 		/// Xử lý hiển thị UI: Đóng UI nhiệm vụ đang làm dở, bật UI Bỏ phiếu.
// 		/// Xử lý âm thanh: Bật Mic Global để mọi người bàn luận (nếu còn sống).
// 		/// </summary>
// 		protected override void OnEnterStateRender()
// 		{
// 			if (TaskUI.ActiveUI)
// 				TaskUI.ActiveUI.CloseTask();

// 			GameManager.im.gameUI.votingScreen.SetActive(true);

// 			if (PlayerMovement.Local.IsDead == false)
// 				GameManager.vm.SetTalkChannel(VoiceManager.GLOBAL);
// 		}

// 		/// <summary>
// 		/// Được gọi khi kết thúc trạng thái Họp (trên Server/Shared).
// 		/// Reset lại thông tin người gọi họp và tắt cờ trạng thái đang Vote.
// 		/// Chuẩn bị cho phase tiếp theo (thường là trả về game hoặc end game).
// 		/// </summary>
// 		protected override void OnExitState()
// 		{
// 			GameManager.Instance.MeetingCaller = null;

// 			GameManager.Instance.VotingScreenActive = false;
// 		}

// 		/// <summary>
// 		/// Được gọi khi kết thúc trạng thái Họp (trên Client - Render).
// 		/// Dọn dẹp UI: Tắt màn hình Bỏ phiếu.
// 		/// Dọn dẹp âm thanh: Tắt Mic (chuyển về NONE) để quay lại gameplay im lặng (nếu còn sống).
// 		/// </summary>
// 		protected override void OnExitStateRender()
// 		{
// 			GameManager.im.gameUI.votingScreen.SetActive(false);
// 			if (PlayerMovement.Local.IsDead == false)
// 				GameManager.vm.SetTalkChannel(VoiceManager.NONE);
// 		}
// 	}
// }