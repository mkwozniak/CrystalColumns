using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wozware.CrystalColumns
{
	[RequireComponent(typeof(RectTransform))]
	public class WigglyObject : MonoBehaviour
	{
		public bool WiggleRotationZ = false;
		public float WiggleRotZMin = 0f;
		public float WiggleRotZMax = 0f;
		public float WiggleRotZSpeed = 1f;

		public bool WigglePositionX = false;
		public float WigglePosXMin = 0f;
		public float WigglePosXMax = 0f;
		public float WigglePosXSpeed = 1f;

		public bool WigglePositionY = false;
		public float WigglePosYMin = 0f;
		public float WigglePosYMax = 0f;
		public float WigglePosYSpeed = 1f;

		private RectTransform _transform;
		private Quaternion _quat;
		private Vector3 _quatEuler;
		private Vector3 _position;

		private float _rotZ = 0f;
		private float _posX = 0f;
		private float _posY = 0f;

		private bool _reverseZ = false;
		private bool _reverseX = false;
		private bool _reverseY = false;

		private void Start()
		{
			_transform = GetComponent<RectTransform>();
			_quat = _transform.rotation;
			_rotZ = 0;
			_posX = 0f;
			_posY = 0f;
			_position = _transform.localPosition;
		}

		private void Update()
		{
			if(WiggleRotationZ)
			{
				WiggleRot();			
			}

			if(WigglePositionX)
			{
				WiggleX();
			}

			if (WigglePositionY)
			{
				WiggleY();
			}
		}

		private void WiggleRot()
		{
			_quat = _transform.rotation;
			_quatEuler = _quat.eulerAngles;

			if (!_reverseZ)
			{
				_quatEuler.z += WiggleRotZSpeed * Time.deltaTime;
				_rotZ += WiggleRotZSpeed * Time.deltaTime;
				_quat.eulerAngles = _quatEuler;
				transform.rotation = _quat;

				if (_rotZ > WiggleRotZMax)
				{
					_reverseZ = true;
				}
				return;
			}

			_quatEuler.z -= WiggleRotZSpeed * Time.deltaTime;
			_rotZ -= WiggleRotZSpeed * Time.deltaTime;
			_quat.eulerAngles = _quatEuler;
			transform.rotation = _quat;

			if (_rotZ < WiggleRotZMin)
			{
				_reverseZ = false;
			}

			return;
		}

		private void WiggleX()
		{
			_position = _transform.localPosition;

			if (!_reverseX)
			{
				_position.x += WigglePosXSpeed * Time.deltaTime;
				_posX += WigglePosXSpeed * Time.deltaTime;
				transform.localPosition = _position;

				if (_posX > WigglePosXMax)
				{
					_reverseX = true;
				}
				return;
			}

			_position.x -= WigglePosXSpeed * Time.deltaTime;
			_posX -= WigglePosXSpeed * Time.deltaTime;
			transform.localPosition = _position;

			if (_posX < WigglePosXMin)
			{
				_reverseX = false;
			}

			return;
		}

		private void WiggleY()
		{
			_position = _transform.localPosition;

			if (!_reverseY)
			{
				_position.y += WigglePosYSpeed * Time.deltaTime;
				_posY += WigglePosYSpeed * Time.deltaTime;
				transform.localPosition = _position;

				if (_posY > WigglePosYMax)
				{
					_reverseY = true;
				}
				return;
			}

			_position.y -= WigglePosYSpeed * Time.deltaTime;
			_posY -= WigglePosYSpeed * Time.deltaTime;
			transform.localPosition = _position;

			if (_posY < WigglePosYMin)
			{
				_reverseY = false;
			}

			return;
		}
	}
}


