using UnityEngine;

namespace FusionImpostor
{
	/// <summary>
	/// Class for creating 4-character room codes.
	/// </summary>
	public static class RoomCode
	{
		public static string Create(int length = 4)
		{
			char[] chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

			string str = "";
			for (int i = 0; i < length; i++)
			{
				str += chars[Random.Range(0, chars.Length)];
			}
			return str;
		}
	}
}