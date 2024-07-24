using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(Animator))]
public class WorldAnimator : MonoBehaviour
{
	public event Action OnStateIdleReady;

	public ParticleSystem FallFX;

	private Animator _anim;

	private void Awake()
	{
		_anim = GetComponent<Animator>();
	}

	public void StateIdleReady()
	{
		if (OnStateIdleReady != null)
		{
			Debug.Log("World Animator: OnStateIdleReady");
			OnStateIdleReady.Invoke();
		}
	}

	public void SwitchState(int state)
	{
		_anim.SetInteger("state", state);
	}

	public void SetSpeed(float speed)
	{
		_anim.speed = speed;
	}
}
