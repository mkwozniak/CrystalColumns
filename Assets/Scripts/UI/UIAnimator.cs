using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Wozware.CrystalColumns;

[RequireComponent(typeof(Animator))]
public sealed class UIAnimator : MonoBehaviour
{
	public event Action OnStateEnter;
	public event Action OnStateExit;
	public event Action OnStateIdleReady;
	public event Action OnStateFadeOut;
	public event Action<UIStates> OnSwitchState;
	public event Action OnButtonHovered;

	public Animator GameAnimator;

	private Animator _anim;

	private void Awake()
	{
		_anim = GetComponent<Animator>();
	}

	public void StateEnter()
	{
		if (OnStateEnter != null)
			OnStateEnter();
	}

	public void StateExit()
	{
		if (OnStateExit != null)
			OnStateExit();
	}

	public void StateIdleReady()
	{
		if (OnStateIdleReady != null)
			OnStateIdleReady();
	}

	public void StateFadeOut()
	{
		if (OnStateFadeOut != null)
			OnStateFadeOut();
	}

	public void SwitchState(UIStates state)
	{
		OnSwitchState(state);
	}

	public void SwitchToMenuState()
	{
		OnSwitchState(UIStates.MainMenu);
	}

	public void SwitchToBlitzState()
	{
		OnSwitchState(UIStates.BlitzMenu);
	}

	public void SwitchToPrestageState()
	{
		OnSwitchState(UIStates.Prestage);
	}

	public void SwitchToPregameState()
	{
		OnSwitchState(UIStates.Pregame);
	}

	public void SwitchToSettingsState()
	{
		OnSwitchState(UIStates.Settings);
	}

	public void SwitchToAudioSettingsState()
	{
		OnSwitchState(UIStates.AudioSettings);
	}

	public void SwitchToGameSettingsState()
	{
		OnSwitchState(UIStates.GameSettings);
	}

	public void SwitchToPracticeState()
	{
		OnSwitchState(UIStates.PracticeMenu);
	}

	public void SwitchToAchievementsState()
	{
		OnSwitchState(UIStates.Achievements);
	}

	public void SetAnimatorState(int state)
	{
		_anim.SetInteger("state", state);
	}

	public void SetGameAnimatorState(int state)
	{
		GameAnimator.SetInteger("state", state);
	}

	public void SetSpeed(float speed)
	{
		_anim.speed = speed;
	}

	public void ButtonHover()
	{
		if (OnButtonHovered != null)
			OnButtonHovered.Invoke();
	}
}
