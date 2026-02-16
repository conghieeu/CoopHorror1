using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class InitializeGame : MonoBehaviour
{
	public bool runBootUpScreen = true;

	public Animator bootUpAnimation;

	public AudioSource bootUpAudio;

	public PlayerActions playerActions;

	private bool canSkip;

	private bool hasSkipped;

	private void OnEnable()
	{
		playerActions.Movement.OpenMenu.performed += OpenMenu_performed;
		playerActions.Movement.Enable();
	}

	private void OnDisable()
	{
		playerActions.Movement.OpenMenu.performed -= OpenMenu_performed;
		playerActions.Movement.Disable();
	}

	private void Awake()
	{
		playerActions = new PlayerActions();
		Application.backgroundLoadingPriority = ThreadPriority.Normal;
	}

	public void OpenMenu_performed(InputAction.CallbackContext context)
	{
		if (context.performed && canSkip && !hasSkipped)
		{
			hasSkipped = true;
			SceneManager.LoadScene("MainMenu");
		}
	}

	private IEnumerator SendToNextScene()
	{
		if (runBootUpScreen)
		{
			bootUpAudio.Play();
			yield return new WaitForSeconds(0.2f);
			canSkip = true;
			bootUpAnimation.SetTrigger("playAnim");
			yield return new WaitForSeconds(3f);
		}
		yield return new WaitForSeconds(0.2f);
		SceneManager.LoadScene("MainMenu");
	}

	private void Start()
	{
		StartCoroutine(SendToNextScene());
	}
}
