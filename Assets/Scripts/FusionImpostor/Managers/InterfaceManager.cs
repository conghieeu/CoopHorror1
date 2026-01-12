using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FusionImpostor
{
	/// <summary>
	/// Contains references to various interface elements
	/// </summary>
	public class InterfaceManager : MonoBehaviour
	{
		public Transform worldCanvasTransform;
		public Transform nicknameHolder;
		public Transform mapIconHolder;
		public GameUI gameUI;

		public UIScreen mainMenuScreen;

		public TMP_Text imposterCountDisplay;
		public TMP_Text tasksCountDisplay;
		public TMP_Text emergencyMeetingsDisplay;
		public TMP_Text discussionTimeDisplay;
		public TMP_Text votingTimeDisplay;
		public TMP_Text walkSpeedDisplay;
		public TMP_Text playerCollisionDisplay;
	}
}