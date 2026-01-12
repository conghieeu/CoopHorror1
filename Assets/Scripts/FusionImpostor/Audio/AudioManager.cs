using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FusionImpostor
{
	/// <summary>
	/// Lớp quản lý âm thanh dùng để điều chỉnh âm lượng, phát âm thanh và lưu các tham số audio.
	/// Không có class này, game sẽ không có hệ thống âm thanh tập trung, gây khó khăn trong việc quản lý nhạc nền và hiệu ứng âm thanh.
	/// </summary>
	public class AudioManager : MonoBehaviour
	{
		/// <summary>
		/// Nguồn phát nhạc nền của game.
		/// Không có nó, game sẽ không thể phát nhạc nền liên tục trong suốt gameplay.
		/// </summary>
		public AudioSource musicSource;

		/// <summary>
		/// Mixer chính điều khiển tất cả các kênh âm thanh.
		/// Không có nó, không thể điều chỉnh âm lượng tổng thể của game.
		/// </summary>
		public AudioMixer masterMixer;
		
		/// <summary>
		/// Mixer group cho hiệu ứng âm thanh (Sound Effects).
		/// Không có nó, các tiếng hiệu ứng sẽ không được phân loại và điều chỉnh riêng biệt.
		/// </summary>
		public AudioMixerGroup sfxMixer;
		
		/// <summary>
		/// Mixer group cho âm thanh giao diện người dùng.
		/// Không có nó, âm thanh UI không thể được điều chỉnh độc lập với các âm thanh khác.
		/// </summary>
		public AudioMixerGroup uiMixer;
		
		/// <summary>
		/// Mixer group cho âm thanh môi trường xung quanh.
		/// Không có nó, nhạc nền và âm thanh môi trường sẽ không có kênh riêng để điều chỉnh.
		/// </summary>
		public AudioMixerGroup ambienceMixer;
		
		/// <summary>
		/// Mixer mặc định được sử dụng khi không chỉ định mixer cụ thể.
		/// Không có nó, hệ thống sẽ không biết dùng mixer nào khi gọi hàm Play() không tham số mixer.
		/// </summary>
		public DefaultMixerTarget defaultMixer = DefaultMixerTarget.None;

		/// <summary>
		/// Tên tham số cho âm lượng chính trong AudioMixer.
		/// Không có nó, không thể truy cập và điều chỉnh âm lượng tổng thể qua code.
		/// </summary>
		public static readonly string mainVolumeParam = "MasterVol";
		
		/// <summary>
		/// Tên tham số cho âm lượng hiệu ứng âm thanh trong AudioMixer.
		/// Không có nó, không thể điều chỉnh riêng âm lượng SFX qua code.
		/// </summary>
		public static readonly string sfxVolumeParam = "SFXVol";
		
		/// <summary>
		/// Tên tham số cho âm lượng UI trong AudioMixer.
		/// Không có nó, không thể điều chỉnh riêng âm lượng giao diện qua code.
		/// </summary>
		public static readonly string uiVolumeParam = "UIVol";
		
		/// <summary>
		/// Tên tham số cho âm lượng môi trường trong AudioMixer.
		/// Không có nó, không thể điều chỉnh riêng âm lượng ambience qua code.
		/// </summary>
		public static readonly string ambienceVolumeParam = "AmbienceVol";
		
		/// <summary>
		/// Tên tham số cho âm lượng giọng nói trong AudioMixer.
		/// Không có nó, không thể điều chỉnh riêng âm lượng voice chat qua code.
		/// </summary>
		public static readonly string voiceVolumeParam = "VoiceVol";

		/// <summary>
		/// Ngân hàng chứa tất cả các audio clip hiệu ứng âm thanh.
		/// Không có nó, không thể quản lý và truy xuất các sound effects một cách có tổ chức.
		/// </summary>
		[SerializeField] private AudioBank soundBank;
		
		/// <summary>
		/// Ngân hàng chứa tất cả các audio clip nhạc nền.
		/// Không có nó, không thể quản lý và phát nhạc nền từ danh sách có sẵn.
		/// </summary>
		[SerializeField] private AudioBank musicBank;

		// Singleton

		/// <summary>
		/// Instance duy nhất của AudioManager (Singleton pattern).
		/// Không có nó, không thể truy cập AudioManager từ bất kỳ script nào khác trong game.
		/// </summary>
		public static AudioManager Instance { get; private set; }

		/// <summary>
		/// Khởi tạo Singleton và setup các ngân hàng âm thanh.
		/// Không có hàm này, AudioManager sẽ không được khởi tạo và game sẽ không có âm thanh.
		/// </summary>
		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
				DontDestroyOnLoad(gameObject);
				InitBanks();
				musicSource.outputAudioMixerGroup = ambienceMixer;
			}
			else
			{
				Destroy(gameObject);
			}
		}

		/// <summary>
		/// Khởi tạo các giá trị âm lượng từ PlayerPrefs đã lưu.
		/// Không có hàm này, cài đặt âm lượng của người chơi sẽ không được khôi phục khi chơi lại.
		/// </summary>
		private void Start()
		{
			InitMixer();
		}

		// Initialization

		/// <summary>
		/// Xây dựng dictionary cho soundBank và musicBank để tra cứu nhanh.
		/// Không có hàm này, các audio clip sẽ không được load vào bộ nhớ và không thể phát được.
		/// </summary>
		private void InitBanks()
		{
			soundBank.Build();
			musicBank.Build();
		}

		/// <summary>
		/// Khởi tạo tất cả các tham số âm lượng của mixer từ PlayerPrefs.
		/// Không có hàm này, âm lượng sẽ không được set từ cài đặt đã lưu, dùng giá trị mặc định.
		/// </summary>
		private void InitMixer()
		{
			SetMixerFromPref(mainVolumeParam);
			SetMixerFromPref(ambienceVolumeParam);
			SetMixerFromPref(sfxVolumeParam);
			SetMixerFromPref(uiVolumeParam);
			SetMixerFromPref(voiceVolumeParam);
		}

		// Public Functions

		// Play Sounds

		/// <summary>
		/// Phát một audio clip với mixer group và vị trí tùy chọn (2D hoặc 3D).
		/// Không có hàm này, không thể phát hiệu ứng âm thanh trong game một cách linh hoạt.
		/// </summary>
		public static void Play(string clip, AudioMixerGroup mixerTarget, Vector3? position = null, float pitch = 1.0f)
		{
			if (Instance.soundBank.TryGetAudio(clip, out AudioClip audioClip))
			{
				GameObject clipObj = new GameObject(clip, typeof(AudioDestroyer));
				AudioSource src = clipObj.AddComponent<AudioSource>();
				if (position.HasValue)
				{
					clipObj.transform.position = position.Value;
					src.spatialBlend = 1;
					src.rolloffMode = AudioRolloffMode.Linear;
					src.maxDistance = 20;
					src.dopplerLevel = 0;
				}
				src.clip = audioClip;
				src.pitch = pitch;
				src.outputAudioMixerGroup = mixerTarget;
				src.Play();
			}
			else
			{
				Debug.LogWarning($"AudioClip '{clip}' not present in audio bank");
			}
		}

		/// <summary>
		/// Phát audio clip với MixerTarget enum thay vì AudioMixerGroup trực tiếp.
		/// Không có hàm này, phải truyền AudioMixerGroup thủ công, gây bất tiện khi code.
		/// </summary>
		public static void Play(string clip, MixerTarget mixerTarget, Vector3? position = null, float pitch = 1.0f)
		{
			Play(clip, Instance.GetMixerGroup(mixerTarget), position, pitch);
		}

		/// <summary>
		/// Phát audio clip với tên mixer dạng string để tìm trong masterMixer.
		/// Không có hàm này, không thể phát âm thanh với mixer custom được tạo thêm trong project.
		/// </summary>
		public static void Play(string clip, string mixerTarget, Vector3? position = null, float pitch = 1.0f)
		{
			Play(clip, Instance.GetMixerGroup(mixerTarget), position, pitch);
		}

		/// <summary>
		/// Phát audio clip với mixer mặc định, chỉ cần tên clip.
		/// Không có hàm này, phải chỉ định mixer mỗi lần gọi, gây dài dòng và phức tạp.
		/// </summary>
		public static void Play(string clip, Vector3? position = null, float pitch = 1.0f)
		{
			Play(clip, MixerTarget.Default, position, pitch);
		}

		/// <summary>
		/// Phát âm thanh 3D và tự động theo dõi một Transform (object di chuyển).
		/// Không có hàm này, âm thanh sẽ cố định tại vị trí ban đầu, không phù hợp với object di chuyển.
		/// </summary>
		public static void PlayAndFollow(string clip, Transform target, MixerTarget mixerTarget)
		{
			if (Instance.soundBank.TryGetAudio(clip, out AudioClip audioClip))
			{
				GameObject clipObj = new GameObject(clip, typeof(AudioDestroyer));
				AudioSource src = clipObj.AddComponent<AudioSource>();
				FollowTarget follow = clipObj.AddComponent<FollowTarget>();
				src.spatialBlend = 1;
				src.rolloffMode = AudioRolloffMode.Linear;
				src.maxDistance = 50;
				src.dopplerLevel = 0;
				src.clip = audioClip;
				src.outputAudioMixerGroup = Instance.GetMixerGroup(mixerTarget);
				follow.target = target;
				src.Play();
			}
			else
			{
				Debug.LogWarning($"AudioClip '{clip}' not present in audio bank");
			}
		}

		/// <summary>
		/// Phát âm thanh theo dõi Transform với mixer mặc định.
		/// Không có hàm này, phải chỉ định mixer mỗi lần, gây bất tiện khi dùng thường xuyên.
		/// </summary>
		public static void PlayAndFollow(string clip, Transform target)
		{
			PlayAndFollow(clip, target, MixerTarget.Default);
		}

		// Play Music

		/// <summary>
		/// Phát nhạc nền từ music bank.
		/// Không có hàm này, game sẽ không có cách tiện lợi để phát nhạc nền từ danh sách có sẵn.
		/// </summary>
		public static void PlayMusic(string music)
		{
			if (string.IsNullOrEmpty(music) == false)
			{
				if (Instance.musicBank.TryGetAudio(music, out AudioClip audio))
				{
					Instance.musicSource.clip = audio;
					Instance.musicSource.Play();
				}
				else
				{
					Debug.LogWarning($"AudioClip '{music}' not present in music bank");
				}
			}
		}

		/// <summary>
		/// Tạm dừng nhạc nền đang phát.
		/// Không có hàm này, không thể pause nhạc khi cần (như mở menu pause).
		/// </summary>
		public static void PauseMusic()
		{
			Instance.musicSource.Pause();
		}

		/// <summary>
		/// Tiếp tục phát nhạc nền đã tạm dừng.
		/// Không có hàm này, không thể resume nhạc sau khi pause, chỉ có thể dừng hẳn.
		/// </summary>
		public static void UnpauseMusic()
		{
			Instance.musicSource.UnPause();
		}

		/// <summary>
		/// Dừng hẳn nhạc nền và xóa clip đang phát.
		/// Không có hàm này, không thể dừng hoàn toàn nhạc nền khi cần thiết.
		/// </summary>
		public static void StopMusic()
		{
			Instance.musicSource.Stop();
			Instance.musicSource.clip = null;
		}

		#region Volume

		/// <summary>
		/// Đặt âm lượng tổng thể của game (0-1) và lưu vào PlayerPrefs.
		/// Không có hàm này, người chơi không thể điều chỉnh âm lượng tổng thể trong settings.
		/// </summary>
		public static void SetVolumeMaster(float value)
		{
			Instance.masterMixer.SetFloat(mainVolumeParam, ToDecibels(value));
			SetPref(mainVolumeParam, value);
		}

		/// <summary>
		/// Đặt âm lượng hiệu ứng âm thanh (0-1) và lưu vào PlayerPrefs.
		/// Không có hàm này, không thể điều chỉnh riêng âm lượng SFX độc lập với âm thanh khác.
		/// </summary>
		public static void SetVolumeSFX(float value)
		{
			Instance.masterMixer.SetFloat(sfxVolumeParam, ToDecibels(value));
			SetPref(sfxVolumeParam, value);
		}

		/// <summary>
		/// Đặt âm lượng giao diện người dùng (0-1) và lưu vào PlayerPrefs.
		/// Không có hàm này, không thể điều chỉnh riêng âm lượng UI sounds.
		/// </summary>
		public static void SetVolumeUI(float value)
		{
			Instance.masterMixer.SetFloat(uiVolumeParam, ToDecibels(value));
			SetPref(uiVolumeParam, value);
		}

		/// <summary>
		/// Đặt âm lượng môi trường/nhạc nền (0-1) và lưu vào PlayerPrefs.
		/// Không có hàm này, không thể điều chỉnh riêng âm lượng ambience và music.
		/// </summary>
		public static void SetVolumeAmbience(float value)
		{
			Instance.masterMixer.SetFloat(ambienceVolumeParam, ToDecibels(value));
			SetPref(ambienceVolumeParam, value);
		}

		/// <summary>
		/// Đặt âm lượng giọng nói/voice chat (0-1) và lưu vào PlayerPrefs.
		/// Không có hàm này, không thể điều chỉnh riêng âm lượng voice communication.
		/// </summary>
		public static void SetVolumeVoice(float value)
		{
			Instance.masterMixer.SetFloat(voiceVolumeParam, ToDecibels(value));
			SetPref(voiceVolumeParam, value);
		}

		/// <summary>
		/// Chuyển đổi giá trị tuyến tính (0-1) sang decibel (-80 đến 0 dB).
		/// Không có hàm này, không thể set âm lượng cho AudioMixer vì nó yêu cầu giá trị decibel.
		/// </summary>
		public static float ToDecibels(float value)
		{
			if (value == 0) return -80;
			return Mathf.Log10(value) * 20;
		}

		/// <summary>
		/// Chuyển đổi giá trị decibel về dạng tuyến tính (0-1).
		/// Không có hàm này, không thể hiển thị giá trị âm lượng dễ hiểu cho người dùng (slider).
		/// </summary>
		public static float FromDecibels(float db)
		{
			if (db == -80) return 0;
			return Mathf.Pow(10, db / 20);
		}

		/// <summary>
		/// Lấy giá trị âm lượng chuẩn hóa (0-1) từ một tham số trong mixer.
		/// Không có hàm này, không thể đọc giá trị âm lượng hiện tại để hiển thị trong UI.
		/// </summary>
		public static float GetFloatNormalized(string param)
		{
			if (Instance.masterMixer.GetFloat(param, out float v)) return FromDecibels(v);
			return -1;
		}

		#endregion

		#region Player Prefs

		/// <summary>
		/// Lấy giá trị âm lượng tuyến tính (0-1) đã lưu từ PlayerPrefs, mặc định 0.75.
		/// Không có hàm này, không thể khôi phục cài đặt âm lượng đã lưu của người chơi.
		/// </summary>
		private static float GetPref(string pref)
		{
			float v = PlayerPrefs.GetFloat(pref, 0.75f);
			return v;
		}

		/// <summary>
		/// Lưu giá trị âm lượng tuyến tính (0-1) vào PlayerPrefs.
		/// Không có hàm này, cài đặt âm lượng sẽ không được lưu lại giữa các phiên chơi.
		/// </summary>
		private static void SetPref(string pref, float val)
		{
			PlayerPrefs.SetFloat(pref, val);
		}

		/// <summary>
		/// Đặt giá trị mixer từ PlayerPrefs đã lưu (chuyển sang decibel).
		/// Không có hàm này, mixer sẽ không được khôi phục từ cài đặt đã lưu khi game khởi động.
		/// </summary>
		private void SetMixerFromPref(string pref)
		{
			masterMixer.SetFloat(pref, ToDecibels(GetPref(pref)));
		}

		#endregion

		#region Mixer & Other

		/// <summary>
		/// Lấy AudioMixerGroup mặc định đã cấu hình.
		/// Không có hàm này, không biết mixer nào dùng khi gọi hàm với MixerTarget.Default.
		/// </summary>
		private AudioMixerGroup DefaultMixerGroup()
		{
			return GetMixerGroup((MixerTarget)Instance.defaultMixer);
		}

		/// <summary>
		/// Lấy AudioMixerGroup tương ứng từ MixerTarget enum.
		/// Không có hàm này, không thể chuyển đổi từ enum sang mixer group thực tế.
		/// </summary>
		private AudioMixerGroup GetMixerGroup(MixerTarget target)
		{
			if (target == MixerTarget.None) return null;
			if (target == MixerTarget.Default) return GetMixerGroup((MixerTarget)defaultMixer);
			if (target == MixerTarget.SFX) return sfxMixer;
			if (target == MixerTarget.UI) return uiMixer;
			throw new System.Exception("Invalid MixerTarget");
		}

		/// <summary>
		/// Tìm AudioMixerGroup trong masterMixer theo tên string.
		/// Không có hàm này, không thể sử dụng các mixer group custom được tạo thêm.
		/// </summary>
		private AudioMixerGroup GetMixerGroup(string target)
		{
			AudioMixerGroup[] foundGroups = masterMixer.FindMatchingGroups(target);
			if (foundGroups.Length > 0) return foundGroups[0];
			throw new System.Exception($"No mixer group by the name {target} could be found");
		}

		/// <summary>
		/// Enum định nghĩa các loại mixer target cơ bản.
		/// Không có enum này, phải dùng string hoặc truyền mixer trực tiếp, dễ lỗi và khó maintain.
		/// </summary>
		public enum MixerTarget { None, Default, SFX, UI }
		
		/// <summary>
		/// Enum cho mixer mặc định, giới hạn các option hợp lệ trong Inspector.
		/// Không có enum này, có thể chọn nhầm Default làm default mixer gây lỗi vòng lặp.
		/// </summary>
		public enum DefaultMixerTarget { None = MixerTarget.None, SFX = MixerTarget.SFX, UI = MixerTarget.UI }

		#endregion

		/// <summary>
		/// Class lưu cặp Key-Value cho audio bank (tên clip - AudioClip object).
		/// Không có class này, không thể serialize danh sách audio trong Inspector một cách có tổ chức.
		/// </summary>
		[System.Serializable]
		public class BankKVP
		{
			/// <summary>
			/// Tên key để tra cứu audio clip.
			/// Không có nó, không biết dùng tên gì để gọi Play("tên_này").
			/// </summary>
			public string Key;
			
			/// <summary>
			/// AudioClip tương ứng với key.
			/// Không có nó, không có audio để phát khi gọi key.
			/// </summary>
			public AudioClip Value;
		}

		/// <summary>
		/// Class quản lý ngân hàng audio clips với dictionary để tra cứu nhanh.
		/// Không có class này, phải quản lý audio clips thủ công, không hiệu quả và dễ lỗi.
		/// </summary>
		[System.Serializable]
		public class AudioBank
		{
			/// <summary>
			/// Mảng các cặp key-value được serialize trong Inspector.
			/// Không có nó, không thể cấu hình danh sách audio trong Unity Editor.
			/// </summary>
			[SerializeField] private BankKVP[] kvps;
			
			/// <summary>
			/// Dictionary để tra cứu AudioClip nhanh theo tên key.
			/// Không có nó, phải duyệt mảng mỗi lần tìm audio, rất chậm và không hiệu quả.
			/// </summary>
			private readonly Dictionary<string, AudioClip> dictionary = new Dictionary<string, AudioClip>();

			/// <summary>
			/// Kiểm tra tính hợp lệ của audio bank (không rỗng, không có key trùng).
			/// Không có hàm này, có thể có key trùng lặp gây lỗi khi Build dictionary.
			/// </summary>
			public bool Validate()
			{
				if (kvps.Length == 0) return false;

				List<string> keys = new List<string>();
				foreach (var kvp in kvps)
				{
					if (keys.Contains(kvp.Key)) return false;
					keys.Add(kvp.Key);
				}
				return true;
			}

			/// <summary>
			/// Xây dựng dictionary từ mảng kvps để tra cứu nhanh.
			/// Không có hàm này, dictionary sẽ rỗng và không thể tìm được audio clip nào.
			/// </summary>
			public void Build()
			{
				if (Validate())
				{
					for (int i = 0; i < kvps.Length; i++)
					{
						dictionary.Add(kvps[i].Key, kvps[i].Value);
					}
				}
			}

			/// <summary>
			/// Thử lấy AudioClip từ dictionary theo key, trả về true nếu tìm thấy.
			/// Không có hàm này, không có cách an toàn để kiểm tra và lấy audio từ bank.
			/// </summary>
			public bool TryGetAudio(string key, out AudioClip audio)
			{
				return dictionary.TryGetValue(key, out audio);
			}
		}

	#if UNITY_EDITOR
		/// <summary>
		/// Custom drawer cho AudioBank trong Unity Inspector, hiển thị danh sách kvps.
		/// Không có class này, AudioBank sẽ hiển thị xấu và khó chỉnh sửa trong Inspector.
		/// </summary>
		[CustomPropertyDrawer(typeof(AudioBank))]
		public class AudioBankDrawer : PropertyDrawer
		{
			/// <summary>
			/// Tính chiều cao cần thiết để vẽ property trong Inspector.
			/// Không có hàm này, Inspector sẽ không cấp đủ không gian hiển thị, bị cắt xén.
			/// </summary>
			public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			{
				return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("kvps"));
			}

			/// <summary>
			/// Vẽ giao diện custom cho AudioBank trong Inspector.
			/// Không có hàm này, AudioBank sẽ dùng UI mặc định, không thân thiện người dùng.
			/// </summary>
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				EditorGUI.BeginProperty(position, label, property);
				EditorGUI.PropertyField(position, property.FindPropertyRelative("kvps"),label, true);
				EditorGUI.EndProperty();
			}
		}

		/// <summary>
		/// Custom drawer cho BankKVP, hiển thị Key và Value song song trong Inspector.
		/// Không có class này, BankKVP sẽ hiển thị dạng dropdown, khó xem và chỉnh sửa.
		/// </summary>
		[CustomPropertyDrawer(typeof(BankKVP))]
		public class BankKVPDrawer : PropertyDrawer
		{
			/// <summary>
			/// Vẽ BankKVP với Key bên trái và Value bên phải trên cùng một dòng.
			/// Không có hàm này, mỗi BankKVP sẽ chiếm nhiều dòng, rất tốn không gian Inspector.
			/// </summary>
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{

				EditorGUI.BeginProperty(position, label, property);

				Rect rect1 = new Rect(position.x, position.y, position.width / 2 - 4, position.height);
				Rect rect2 = new Rect(position.center.x + 2, position.y, position.width / 2 - 4, position.height);

				EditorGUI.PropertyField(rect1, property.FindPropertyRelative("Key"), GUIContent.none);
				EditorGUI.PropertyField(rect2, property.FindPropertyRelative("Value"), GUIContent.none);

				EditorGUI.EndProperty();
			}
		}
	#endif
	}
}