using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace __GEN
{
	internal class NetworkVariableSerializationHelper
	{
		[RuntimeInitializeOnLoadMethod]
		internal static void InitializeSerialization()
		{
			NetworkVariableSerializationTypedInitializers.InitializeSerializer_FixedString<FixedString128Bytes>();
			NetworkVariableSerializationTypedInitializers.InitializeEqualityChecker_UnmanagedIEquatable<FixedString128Bytes>();
		}
	}
}
