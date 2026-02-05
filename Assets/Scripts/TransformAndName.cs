using System;
using UnityEngine;

// định nghĩa một lớp để lưu trữ Transform và tên của một đối tượng, cùng với một cờ để xác định xem nó có phải là NPC hay không, được sử dụng trong các hệ thống quản lý đối tượng, tương tác hoặc AI, giúp dễ dàng tham chiếu và quản lý các đối tượng trong trò chơi.
[Serializable]
public class TransformAndName
{
	public Transform transform;

	public string name;

	public bool isNonPlayer;

	public TransformAndName(Transform newTransform, string newName, bool nonPlayer = false)
	{
		name = newName;
		transform = newTransform;
		isNonPlayer = nonPlayer;
	}
}
