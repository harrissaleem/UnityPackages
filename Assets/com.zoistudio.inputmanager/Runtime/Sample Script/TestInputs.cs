﻿using UnityEngine;
using UnityEngine.UI;

namespace ZoiStudio.InputManager
{
	public class TestInputs : MonoBehaviour, IInputListener
	{
		public float ShowTextDuration = 0.1f;

		public Text tapTxt;
		public Text holdTxt;
		public Text velocity;


		float displayTextDuration;
		float displayHoldTextDuration;

		public void OnEnable()
		{
			// One way of doing it	
			//TouchInputManager.Instance.OnInput += TouchInputManager_OnInput;
			// The way I like
			InputEventManager.Subscribe(this, GameAction.Tap, GameAction.Hold, GameAction.SwipeLeft, GameAction.SwipeRight, GameAction.SwipeUp, GameAction.SwipeDown, GameAction.TapReleased);
		}

		void HandleInputEvent(InputActionArgs input)
		{
			displayHoldTextDuration = Time.time;
			switch (input.Action)
			{
				case GameAction.Hold:
					holdTxt.text = input.Action.ToString();
					break;
				case GameAction.Tap:
				case GameAction.TapReleased:
				case GameAction.SwipeLeft:
				case GameAction.SwipeRight:
				case GameAction.SwipeUp:
				case GameAction.SwipeDown:
					velocity.text = input.Velocity.ToString();
					displayTextDuration = Time.time;
					tapTxt.text = input.Action.ToString();
					break;
				default:
					break;
			}
		}

		private void Update()
		{
			if (Time.time - displayHoldTextDuration > ShowTextDuration)
			{
				displayHoldTextDuration = Time.time;
				holdTxt.text = "";
			}

			if (Time.time - displayTextDuration > ShowTextDuration)
			{
				displayTextDuration = Time.time;
				tapTxt.text = "";
			}
		}

		private void TouchInputManager_OnInput(InputActionArgs action)
		{
			// HandleInputEvent(action);
		}

		public void OnTap()
        {
			tapTxt.text = "Tap";
		}

		public void SwifeLeft()
        {
			tapTxt.text = "Swipe Left";
		}

		public void OnDisable()
		{
			// One way of doing it
			//TouchInputManager.Instance.OnInput -= TouchInputManager_OnInput;		
			// The way I like - but you should in practice always unsubscribe to all actions if the object is getting disabled or deleted
			InputEventManager.UnSubscribe(this, GameAction.Tap);
		}

		public void OnInput(InputActionArgs input)
		{
			HandleInputEvent(input);
		}
	}
}