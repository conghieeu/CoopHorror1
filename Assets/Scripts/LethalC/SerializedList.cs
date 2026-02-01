using Unity.Netcode;

// cấu trúc dữ liệu để tuần tự hóa và lưu trữ một danh sách các số nguyên không dấu 64 bit. vì nếu không làm vậy thì nó sẽ không hoạt động với mạng lưới.
internal struct SerializedList : INetworkSerializable
{
	public ulong[] Value;

	void INetworkSerializable.NetworkSerialize<T>(BufferSerializer<T> serializer)
	{
		serializer.SerializeValue(ref Value, default(FastBufferWriter.ForPrimitives));
	}
}
