using System;
using UnityEngine;

namespace FusionImpostor
{
	/// <summary>
	/// Attribute to make a property read-only.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = true)]
	public class ReadOnlyAttribute : PropertyAttribute { }

	/// <summary>
	/// Property drawer for ReadOnly attribute.
	/// </summary>
	#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
	public class ReadOnlyAttributeDrawer : UnityEditor.PropertyDrawer
	{
		public override void OnGUI(Rect rect, UnityEditor.SerializedProperty prop, GUIContent label)
		{
			bool wasEnabled = GUI.enabled;
			GUI.enabled = false;
			UnityEditor.EditorGUI.PropertyField(rect, prop, new GUIContent(prop.displayName));
			GUI.enabled = wasEnabled;
		}
	}
	#endif
}