/// <summary>
/// GameplayInput là "Cấu trúc gói tin" (Data Structure) dùng để đóng gói các lệnh điều khiển.
/// 
/// Tác dụng:
/// - Là ngôn ngữ chung giữa Client và Server: Giúp Server hiểu được đống dữ liệu 0101 mà Client gửi lên nghĩa là "Đi thẳng" hay "Nhảy".
/// - Tối ưu băng thông: Được thiết kế dạng struct (siêu nhẹ), loại bỏ mọi dữ liệu thừa để truyền tải qua mạng nhanh nhất có thể.
/// - NetworkButtons: Nén tất cả các nút bấm (Nhảy, Bắn, Ngồi...) vào một biến duy nhất để tiết kiệm dung lượng.
/// 
/// Nếu thiếu script này:
/// - Client và Server "bất đồng ngôn ngữ": Client gửi tín hiệu lên nhưng Server không biết cách đọc.
/// - Lag lòi mắt: Nếu không dùng struct tối ưu này mà gửi dữ liệu lung tung, mạng sẽ bị nghẽn, game giật tung chảo.
/// </summary>

using UnityEngine;
using Fusion;

namespace NonHand
{
	/// <summary>
	/// Input structure polled by Fusion. This is sent over network and processed by server, keep it optimized and remove unused data.
	/// </summary>
	public struct GameplayInput : INetworkInput
	{
		public Vector2        MoveDirection;
		public Vector2        LookRotationDelta;
		public NetworkButtons Actions;

		public const int JUMP_BUTTON = 0;
	}
}
