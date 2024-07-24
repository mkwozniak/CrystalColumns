using System.Collections.Generic;
using InputAction = UnityEngine.InputSystem.InputAction;

namespace Wozware.CrystalColumns
{
	public sealed class Input
	{
		public Dictionary<string, bool> ActiveInputs = new Dictionary<string, bool>();

		private DefaultControls _defaultControls;
		private InputAction _horizontalInput;
		private InputAction _cursorInput;

		public void Initialize()
		{
			_defaultControls = new DefaultControls();

			_defaultControls.Gameplay.Horizontal.Enable();

			_defaultControls.Gameplay.Cursor.Enable();

			_defaultControls.Gameplay.LaunchGem.Enable();
			ActiveInputs[_defaultControls.Gameplay.LaunchGem.name] = false;
			_defaultControls.Gameplay.LaunchGem.performed += InputActionPerform;
			_defaultControls.Gameplay.LaunchGem.canceled += InputActionEnd;

			_defaultControls.Gameplay.GemSwap.Enable();
			ActiveInputs[_defaultControls.Gameplay.GemSwap.name] = false;
			_defaultControls.Gameplay.GemSwap.performed += InputActionPerform;
			_defaultControls.Gameplay.GemSwap.canceled += InputActionEnd;

			_defaultControls.Gameplay.Pause.Enable();
			ActiveInputs[_defaultControls.Gameplay.Pause.name] = false;
			_defaultControls.Gameplay.Pause.performed += InputActionPerform;
			_defaultControls.Gameplay.Pause.canceled += InputActionEnd;

			_horizontalInput = _defaultControls.Gameplay.Horizontal;
			_cursorInput = _defaultControls.Gameplay.Cursor;
		}

		public float GetHorizontal()
		{
			return _horizontalInput.ReadValue<float>();
		}

		public float GetCursorPositionX()
		{
			return _cursorInput.ReadValue<float>();
		}

		/// <summary> Hook to Unity new input perform event. Invokes an input perform event. </summary>
		/// <param name="obj"></param>
		private void InputActionPerform(InputAction.CallbackContext obj)
		{
			ActiveInputs[obj.action.name] = true; // this input is being pressed
		}

		/// <summary> Hook to Unity new input end event. Invokes an input end event. </summary>
		/// <param name="obj"></param>
		private void InputActionEnd(InputAction.CallbackContext obj)
		{
			ActiveInputs[obj.action.name] = false; // this input has released
		}
	}

}
