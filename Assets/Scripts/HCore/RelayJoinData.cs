using System;

// cấu trúc dữ liệu để lưu trữ thông tin về tham gia chuyển tiếp, bao gồm mã tham gia, địa chỉ IPv4, cổng, ID phân bổ, dữ liệu kết nối và dữ liệu kết nối của máy chủ
public struct RelayJoinData
{
	public string JoinCode;

	public string IPv4Address;

	public ushort Port;

	public Guid AllocationID;

	public byte[] AllocationIDBytes;

	public byte[] ConnectionData;

	public byte[] HostConnectionData;

	public byte[] Key;
}
