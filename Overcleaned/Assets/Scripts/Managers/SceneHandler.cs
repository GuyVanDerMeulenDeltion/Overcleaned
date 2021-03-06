﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneHandler : MonoBehaviour, IServiceOfType
{
	[System.Serializable]
	public struct SceneSetting
	{
		public string sceneName;
		public Vector2 bottomLeftCameraAnchor, upperRightCameraAnchor;
	}

	[Header("Scene Settings")]
	public SceneSetting[] sceneSettings;
	public int currentSceneSetting { get; private set; }

	public delegate void OnSceneIsLoadedAndReady(string sceneName);
	public static OnSceneIsLoadedAndReady onSceneIsLoadedAndReady;

	[Header("Fade Properties")]
	public float fadeDuration;
	public Image fadeScreen;

	private Coroutine sceneLoadingRoutine;
	private Coroutine runningFadeRoutine;
	private bool isFading;
	private bool isLoadingScene;

	[Header("Debugging")]
	public float waitInLoadingScreen;
	public bool logMode;

	#region Initalize Service
	private void Awake()
	{
		if (ServiceLocator.TryAddServiceOfType(this))
			OnInitialise();
		else
			Destroy(gameObject);
	}
	private void OnDestroy() => OnDeinitialise();
	public void OnInitialise() => ServiceLocator.TryAddServiceOfType(this);
	public void OnDeinitialise() => ServiceLocator.TryRemoveServiceOfType(this);
	#endregion

	private void Start()
	{
		DontDestroyOnLoad(gameObject);
		Init();
	}

	private void Init()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
		FadeIn();
	}

	#region Scene Loading

	public void LoadScene(string buildName, bool showLoadingScreen = true)
	{
		currentSceneSetting = GetSceneSettingIndex(buildName);

		if (sceneLoadingRoutine != null)
		{
			StopCoroutine(sceneLoadingRoutine);
		}

		FadeOut();
		sceneLoadingRoutine = StartCoroutine(SceneLoading(buildName, showLoadingScreen));
	}

	public void LoadScene(int arrayIndex, bool showLoadingScreen = true)
	{
		currentSceneSetting = arrayIndex;

		if (sceneLoadingRoutine != null)
		{
			StopCoroutine(sceneLoadingRoutine);
		}

		FadeOut();
		sceneLoadingRoutine = StartCoroutine(SceneLoading(sceneSettings[arrayIndex].sceneName, showLoadingScreen));
	}

	private IEnumerator SceneLoading(string buildName, bool showLoadingScreen = true)
	{
		yield return new WaitUntil(() => isFading == false);

		if (showLoadingScreen)
			SceneManager.LoadScene(1, LoadSceneMode.Single);

		fadeScreen = null;
		isLoadingScene = true;

		if (showLoadingScreen)
			yield return new WaitForSeconds(waitInLoadingScreen);

		if (logMode)
			Debug.Log("Loading the scene with the name: " + buildName);

		SceneManager.LoadScene(buildName, showLoadingScreen ? LoadSceneMode.Additive : LoadSceneMode.Single);

		yield return new WaitUntil(() => isLoadingScene == false);

		if (showLoadingScreen)
			SceneManager.UnloadSceneAsync(1);

		if (onSceneIsLoadedAndReady != null)
			onSceneIsLoadedAndReady.Invoke(buildName);

		FadeIn();
	}

	private int GetSceneSettingIndex(string sceneName)
	{
		for (int i = 0; i < sceneSettings.Length; i++)
		{
			if (sceneSettings[i].sceneName == sceneName)
			{
				return i;
			}
		}

		return -1;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
	{
		if (scene.buildIndex == 1)
			return;

		SceneManager.SetActiveScene(scene);
		isLoadingScene = false;
	}

	#endregion

	#region Screen Fading

	public void FadeOut()
	{
		if (fadeScreen == null)
			fadeScreen = CreateFadeScreen();

		if (runningFadeRoutine != null)
		{
			StopCoroutine(runningFadeRoutine);
		}

		isFading = true;
		runningFadeRoutine = StartCoroutine(FadeOutRoutine(fadeDuration));
	}

	public void FadeIn()
	{
		if (fadeScreen == null)
			fadeScreen = CreateFadeScreen();

		if (runningFadeRoutine != null)
		{
			StopCoroutine(runningFadeRoutine);
		}

		isFading = true;
		runningFadeRoutine = StartCoroutine(FadeInRoutine(fadeDuration));
	}

	private Image CreateFadeScreen()
	{
		GameObject canvas = Resources.Load("Common Prefabs/Fading Canvas") as GameObject;
		return Instantiate(canvas, Vector3.zero, Quaternion.identity).transform.GetChild(0).GetComponent<Image>();
	}

	private IEnumerator FadeInRoutine(float duration)
	{
		while (fadeScreen.color.a > 0)
		{
			fadeScreen.color = new Color(fadeScreen.color.r, fadeScreen.color.g, fadeScreen.color.b, fadeScreen.color.a - Time.deltaTime / duration);
			yield return new WaitForEndOfFrame();
		}

		fadeScreen.color = new Color(fadeScreen.color.r, fadeScreen.color.g, fadeScreen.color.b, 0);
		isFading = false;
	}

	private IEnumerator FadeOutRoutine(float duration)
	{
		while (fadeScreen.color.a < 1)
		{
			fadeScreen.color = new Color(fadeScreen.color.r, fadeScreen.color.g, fadeScreen.color.b, fadeScreen.color.a + Time.deltaTime / duration);
			yield return new WaitForEndOfFrame();
		}

		fadeScreen.color = new Color(fadeScreen.color.r, fadeScreen.color.g, fadeScreen.color.b, 1);
		isFading = false;
	}

	#endregion
}
