using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

namespace Example
{
	/// <summary>
	/// Helper script for UI and keyboard shortcuts.
	/// </summary>
	public sealed class Menu : NetworkBehaviour
	{
		[SerializeField]
		private bool             _showGUI;
		[SerializeField]
		private GUISkin          _skin;
		[SerializeField]
		private FrameRateUpdater _frameRateUpdater;

		private GUIStyle _defaultStyle;
		private GUIStyle _activeStyle;
		private GUIStyle _inactiveStyle;

		private void Update()
		{
			Keyboard keyboard = Keyboard.current;
			if (keyboard == null)
				return;

			if (keyboard.f5Key.wasPressedThisFrame == true)
			{
				ToggleFrameRate();
			}

			if (keyboard.f7Key.wasPressedThisFrame == true)
			{
				ToggleVSync();
			}

			if (keyboard.f8Key.wasPressedThisFrame == true && Application.isMobilePlatform == false && Application.isEditor == false)
			{
				ToggleFullScreen();
			}

			if (keyboard.f12Key.wasPressedThisFrame == true)
			{
				Disconnect();
			}

			if (Application.isMobilePlatform == false || Application.isEditor == true)
			{
				if (keyboard.enterKey.wasPressedThisFrame == true || keyboard.numpadEnterKey.wasPressedThisFrame == true)
				{
					ToggleCursor();
				}
			}
		}

		private void OnGUI()
		{
			if (_showGUI == false)
				return;

			Initialize();

			if (Runner == null || Runner.IsRunning == false)
				return;

			float verticalSpace   = 5.0f;
			float horizontalSpace = 5.0f;

			GUILayout.BeginVertical();
			GUILayout.Space(verticalSpace);
			GUILayout.BeginHorizontal();
			GUILayout.Space(horizontalSpace);

			GUILayout.Button($"{Mathf.RoundToInt(1.0f / Runner.DeltaTime)}Hz", _defaultStyle);

			string   frameRate      = Application.targetFrameRate == 0 ? "Unlimited" : Application.targetFrameRate.ToString();
			GUIStyle frameRateStyle = Application.targetFrameRate == 0 ? _defaultStyle : _activeStyle;

			if (GUILayout.Button($"[F5] FPS ({frameRate} / {_frameRateUpdater.SmoothFrameRate})", frameRateStyle) == true)
			{
				ToggleFrameRate();
			}

			if (GUILayout.Button($"[F7] V-Sync ({(QualitySettings.vSyncCount == 0 ? "Off" : "On")})", QualitySettings.vSyncCount == 0 ? _defaultStyle : _activeStyle) == true)
			{
				ToggleVSync();
			}

			if (Application.isMobilePlatform == false && Application.isEditor == false)
			{
				if (GUILayout.Button($"[F8] FullScreen ({Screen.fullScreenMode})", _defaultStyle) == true)
				{
					ToggleFullScreen();
				}
			}

			if (GUILayout.Button($"[F12] Disconnect", _defaultStyle) == true)
			{
				Disconnect();
			}

			if (Application.isMobilePlatform == false || Application.isEditor == true)
			{
				if (GUILayout.Button($"[Enter] Cursor", _defaultStyle) == true)
				{
					ToggleCursor();
				}
			}

			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}

		private void Initialize()
		{
			if (_defaultStyle == null)
			{
				_defaultStyle = new GUIStyle(_skin.button);
				_defaultStyle.alignment = TextAnchor.MiddleCenter;

				if (Application.isMobilePlatform == true && Application.isEditor == false)
				{
					_defaultStyle.fontSize = 20;
					_defaultStyle.padding = new RectOffset(20, 20, 20, 20);
				}

				_activeStyle = new GUIStyle(_defaultStyle);
				_activeStyle.normal.textColor  = Color.green;
				_activeStyle.focused.textColor = Color.green;
				_activeStyle.hover.textColor   = Color.green;

				_inactiveStyle = new GUIStyle(_defaultStyle);
				_inactiveStyle.normal.textColor  = Color.red;
				_inactiveStyle.focused.textColor = Color.red;
				_inactiveStyle.hover.textColor   = Color.red;
			}
		}

		private void ToggleFrameRate()
		{
			_frameRateUpdater.Toggle();
		}

		private void ToggleVSync()
		{
			QualitySettings.vSyncCount = QualitySettings.vSyncCount == 0 ? 1 : 0;
		}

		private void ToggleFullScreen()
		{
			Resolution maxResolution            = default;
			int        maxResolutionSize        = default;
			int        maxResolutionRefreshRate = default;

			Resolution[] resolutions = Screen.resolutions;
			foreach (Resolution resolution in resolutions)
			{
				int resolutionSize = resolution.width * resolution.height;
				if (resolutionSize >= maxResolutionSize)
				{
					if (GetRefreshRate(resolution) >= maxResolutionRefreshRate)
					{
						maxResolutionSize        = resolutionSize;
						maxResolutionRefreshRate = GetRefreshRate(resolution);
						maxResolution            = resolution;
					}
				}
			}

			switch (Screen.fullScreenMode)
			{
				case FullScreenMode.ExclusiveFullScreen: { SetResolution(maxResolution.width / 2, maxResolution.height / 2, FullScreenMode.Windowed,            maxResolution); break;}
				case FullScreenMode.FullScreenWindow:    { SetResolution(maxResolution.width,     maxResolution.height,     FullScreenMode.ExclusiveFullScreen, maxResolution); break;}
				case FullScreenMode.MaximizedWindow:     { SetResolution(maxResolution.width,     maxResolution.height,     FullScreenMode.FullScreenWindow,    maxResolution); break;}
				case FullScreenMode.Windowed:            { SetResolution(maxResolution.width,     maxResolution.height,     FullScreenMode.MaximizedWindow,     maxResolution); break;}
				default:
				{
					throw new NotImplementedException(Screen.fullScreenMode.ToString());
				}
			}
		}

		private void ToggleCursor()
		{
			if (Application.isMobilePlatform == false || Application.isEditor == true)
			{
				if (Cursor.lockState == CursorLockMode.Locked)
				{
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
				}
				else
				{
					Cursor.lockState = CursorLockMode.Locked;
					Cursor.visible = false;
				}
			}
		}

		private void Disconnect()
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible   = true;

			#if UNITY_6000_0_OR_NEWER
			GameObject.FindFirstObjectByType<FusionBootstrap>().ShutdownAll();
			#else
			GameObject.FindObjectOfType<FusionBootstrap>().ShutdownAll();
			#endif
		}

		private bool GetNumberDown(int offset)
		{
			switch (offset)
			{
				case 0 : { return Keyboard.current.numpad1Key.wasPressedThisFrame == true || Keyboard.current.digit1Key.wasPressedThisFrame == true; }
				case 1 : { return Keyboard.current.numpad2Key.wasPressedThisFrame == true || Keyboard.current.digit2Key.wasPressedThisFrame == true; }
				case 2 : { return Keyboard.current.numpad3Key.wasPressedThisFrame == true || Keyboard.current.digit3Key.wasPressedThisFrame == true; }
				case 3 : { return Keyboard.current.numpad4Key.wasPressedThisFrame == true || Keyboard.current.digit4Key.wasPressedThisFrame == true; }
				case 4 : { return Keyboard.current.numpad5Key.wasPressedThisFrame == true || Keyboard.current.digit5Key.wasPressedThisFrame == true; }
				case 5 : { return Keyboard.current.numpad6Key.wasPressedThisFrame == true || Keyboard.current.digit6Key.wasPressedThisFrame == true; }
				case 6 : { return Keyboard.current.numpad7Key.wasPressedThisFrame == true || Keyboard.current.digit7Key.wasPressedThisFrame == true; }
				case 7 : { return Keyboard.current.numpad8Key.wasPressedThisFrame == true || Keyboard.current.digit8Key.wasPressedThisFrame == true; }
				case 8 : { return Keyboard.current.numpad9Key.wasPressedThisFrame == true || Keyboard.current.digit9Key.wasPressedThisFrame == true; }
			}

			return false;
		}

		private static void SetResolution(int width, int height, FullScreenMode fullscreenMode, Resolution resolution)
		{
			#if UNITY_2022_3_OR_NEWER
			Screen.SetResolution(width, height, fullscreenMode, resolution.refreshRateRatio);
			#else
			Screen.SetResolution(width, height, fullscreenMode, resolution.refreshRate);
			#endif
		}

		private static int GetRefreshRate(Resolution resolution)
		{
			#if UNITY_2022_3_OR_NEWER
			return (int)System.Math.Round(resolution.refreshRateRatio.value);
			#else
			return resolution.refreshRate;
			#endif
		}
	}
}
