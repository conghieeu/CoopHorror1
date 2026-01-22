// /// <summary>
// /// PlayStateBehaviour quản lý giai đoạn "Chơi Chính" (Gameplay) của trận đấu.
// /// Đây là nơi mọi hành động chính diễn ra sau khi rời phòng chờ.
// /// 
// /// Tác dụng (Khi bắt đầu vào game):
// /// - Sắp xếp đội hình: Dịch chuyển tất cả người chơi về vị trí xuất phát trên bản đồ.
// /// - Chia vai: Random chọn ai là Impostor (Sói) và ai là Crewmate (Người).
// /// - Giao bài tập về nhà: Random danh sách nhiệm vụ (Tasks) cho từng người chơi.
// /// - Cài đặt vũ khí: Kích hoạt thời gian hồi chiêu giết người (Kill Cooldown) cho Impostor.
// /// - Setup giao diện: Hiển thị danh sách nhiệm vụ lên màn hình và TẮT Voice Chat (để game im lặng kịch tính).
// /// 
// /// Nếu thiếu script này:
// /// - Game không thể bắt đầu: Người chơi kẹt mãi ở sảnh chờ.
// /// - Không có Sói: Chẳng ai được chỉ định làm Impostor để đi giết người cả.
// /// - Không có Nhiệm vụ: Crewmate không biết làm gì để thắng.
// /// </summary>

// using Fusion;
// using Fusion.Addons.FSM;
// using System.Collections.Generic;
// using UnityEngine;

// namespace NonHand
// {
//     /// <summary>
//     /// PlayStateBehaviour: Trạng thái "Đang Chơi". 
//     /// Đây là giai đoạn chính của game, nơi mọi người đi làm nhiệm vụ hoặc đi giết nhau.
//     /// </summary>
//     public class PlayStateBehaviour : StateBehaviour
//     {
//         // Thời gian chờ (giây) mà Impostor phải đợi đầu game mới được giết người.
//         // Tránh việc vừa vào game chưa kịp đi đâu đã bị giết.
//         [SerializeField, Tooltip("The amoount of time, in seconds, that an impostor has to wait before being able to kill a crewmate.")]
//         float initialKillTimer = 30;

//         // [SERVER ONLY] Hàm này chỉ chạy trên Server khi bắt đầu vào trạng thái này.
//         // Dùng để thiết lập luật chơi, vị trí, vai trò.
//         protected override void OnEnterState()
//         {
//             // 1. Dịch chuyển tất cả người chơi về điểm xuất phát (Spawn Point) trên bản đồ.
//             // Để tránh ai đó đang đứng ở vị trí cũ (ví dụ lúc ở sảnh chờ) bị kẹt tường.
//             PlayerRegistry.ForEach(
//                 obj => obj.Controller.cc.SetPosition(GameManager.Instance.mapData.GetSpawnPosition(obj.Index)));

//             // 2. Kiểm tra: Có phải chúng ta mới từ Sảnh Chờ (Pregame) vào không?
//             // - Nếu ĐÚNG: Nghĩa là ván mới toanh -> Cần chia vai Sói/Người.
//             // - Nếu SAI (ví dụ từ màn hình Vote quay lại): Nghĩa là game đang chơi dở -> Giữ nguyên vai trò, không chia lại.
//             if (Machine.PreviousState is PregameStateBehaviour)
//             {
//                 // Nếu có cài đặt số lượng Impostor > 0
//                 if (GameManager.Instance.Settings.numImposters > 0)
//                 {
//                     // Chọn ngẫu nhiên X người làm Impostor
//                     PlayerObject[] objs = PlayerRegistry.GetRandom(GameManager.Instance.Settings.numImposters);
//                     foreach (PlayerObject p in objs)
//                     {
//                         // Gán nhãn "Kẻ tình nghi" (Impostor) cho họ
//                         p.Controller.IsSuspect = true;
//                         // In ra Console để debug (đừng quên xóa dòng này khi release game kẻo lộ)
//                         Debug.Log($"[SPOILER]\n\n{p.GetStyledNickname} is suspect");
//                     }
//                 }

//                 // Cấp số lần bấm nút "Họp Khẩn Cấp" cho tất cả mọi người theo cài đặt
//                 PlayerRegistry.ForEach(pObj =>
//                 {
//                     pObj.Controller.EmergencyMeetingUses = GameManager.Instance.Settings.numEmergencyMeetings;
//                 });
//             }

//             // 3. Giao nhiệm vụ (Tasks) cho từng người chơi.
//             // Gọi hàm GetRandomTasks để lấy danh sách ngẫu nhiên, sau đó gán vào Controller của họ.
//             PlayerRegistry.ForEach(p => p.Controller.DefineTasks(GetRandomTasks(GameManager.Instance.Settings.numTasks)));

//             // 4. Kích hoạt thời gian hồi chiêu (Cooldown) giết người cho Impostor.
//             PlayerRegistry.ForEachWhere(
//                 p => p.Controller.IsSuspect, // Chỉ tìm những ai là Impostor
//                 p => p.Controller.KillTimer = TickTimer.CreateFromSeconds(GameManager.Instance.Runner, initialKillTimer)); // Set đồng hồ đếm ngược
//         }

//         // [CLIENT SIDE] Hàm này chạy trên máy người chơi để xử lý Giao diện (UI) và Hình ảnh.
//         protected override void OnEnterStateRender()
//         {
//             // Đóng các màn hình phủ (Overlay) ví dụ như màn hình "Kết quả Vote" hoặc màn hình chờ.
//             // Logic: Nếu trước đó là màn Vote thì đóng nhanh (0s), còn không thì fade từ từ (3s).
//             GameManager.im.gameUI.CloseOverlay(Machine.PreviousState is VotingResultsStateBehaviour ? 0 : 3);

//             // Nếu là ván game mới toanh (từ Sảnh chờ ra)
//             if (Machine.PreviousState is PregameStateBehaviour)
//             {
//                 // Tắt cái vỏ tàu vũ trụ (Lobby) đi vì giờ vào map chính rồi.
//                 GameManager.Instance.mapData.hull.SetActive(false);
                
//                 // Khởi tạo UI trong game (nút bấm, bản đồ mini...)
//                 GameManager.im.gameUI.InitGame();

//                 // --- Xử lý hiển thị danh sách nhiệm vụ bên góc màn hình ---
                
//                 // Xóa danh sách hiển thị cũ
//                 GameManager.Instance.taskDisplayList.Clear();
                
//                 // Lấy nhiệm vụ của CHÍNH MÌNH (Local Player) để hiển thị lên bảng Task
//                 foreach (TaskStation playerTask in PlayerRegistry.GetPlayer(Runner.LocalPlayer).Controller.tasks)
//                     GameManager.Instance.taskDisplayList.Add(playerTask);

//                 // Đếm tổng số lượng Task trên bản đồ để tính thanh tiến độ (Total Task Bar)
//                 TaskBase[] foundTasks = FindObjectsOfType<TaskBase>(true);
//                 foreach (TaskBase task in foundTasks)
//                 {
//                     // Lưu vào Dictionary để tiện tra cứu
//                     GameManager.Instance.taskDisplayAmounts.Add(task, (byte)GameManager.Instance.TaskCount(task));
//                 }

//                 // Vẽ lại UI danh sách Task
//                 GameManager.im.gameUI.InitTaskUI();

//                 // TẮT VOICE CHAT: Vào game rồi, phải im lặng (hoặc dùng Proximity Chat) chứ không được nói Global nữa.
//                 GameManager.vm.SetTalkChannel(VoiceManager.NONE);
//             }
//         }

//         // Hàm tiện ích: Lấy ngẫu nhiên một số lượng Task nhất định từ các Task có sẵn trên bản đồ.
//         List<TaskStation> GetRandomTasks(byte taskNumber)
//         {
//             // Tìm tất cả các trạm nhiệm vụ (TaskStation) đang có trong Scene
//             List<TaskStation> taskList = new List<TaskStation>(FindObjectsOfType<TaskStation>());

//             // --- Thuật toán xáo trộn (Shuffle) ---
//             // Đảo lộn thứ tự danh sách taskList ngẫu nhiên
//             int count = taskList.Count;
//             int last = count - 1;
//             for (int i = 0; i < last; ++i)
//             {
//                 int r = Random.Range(i, count);
//                 TaskStation tmp = taskList[i];
//                 taskList[i] = taskList[r];
//                 taskList[r] = tmp;
//             }

//             // Sau khi xáo trộn, cắt bớt danh sách đi, chỉ giữ lại đúng số lượng nhiệm vụ cần thiết (taskNumber).
//             // Ví dụ: Map có 20 task, nhưng cài đặt chỉ cần làm 5 task -> Xóa 15 cái thừa đi.
//             taskList.RemoveRange(0, taskList.Count - taskNumber);

//             return taskList;
//         }
//     }
// }