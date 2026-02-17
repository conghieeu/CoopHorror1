using System;

// cấu trúc dữ liệu để lưu trữ thông tin về máy chủ chuyển tiếp, bao gồm mã tham gia, địa chỉ IPv4, cổng, ID phân bổ và dữ liệu kết nối
public struct RelayHostData
{
	public string JoinCode;

	public string IPv4Address;

	public ushort Port;

	public Guid AllocationID;

	public byte[] AllocationIDBytes;

	public byte[] ConnectionData;

	public byte[] Key;
}
