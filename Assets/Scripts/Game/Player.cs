using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Wozware.CrystalColumns
{
	public class Player : MonoBehaviour
	{
		#region Events

		public Action<string> PlaySFX;
		public Action OnTriggeredPause;

		#endregion

		#region Members

		public float AutoShootTime
		{
			set { _autoShootTime = value; }
		}

		[Header("References")]

		/// <summary> World reference. </summary>
		[SerializeField] private World _world;

		/// <summary> The line renderer for the alpha aim line. </summary>
		[SerializeField] private LineRenderer _aimLine;

		/// <summary> The line renderer for the alpha upper aim line. </summary>
		[SerializeField] private LineRenderer _upperAimLine;

		/// <summary> The sprite renderer for the preview aimer gem. </summary>
		[SerializeField] private SpriteRenderer _aimer;

		[Header("Settings")]

		/// <summary> The collision layer for the level. </summary>
		[SerializeField] private LayerMask _levelMask;

		/// <summary> The input threshold to move player with key. </summary>
		[SerializeField] private float _horizontalKeyThreshold = 0.1f;

		/// <summary> The delay between gem launches. </summary>
		[SerializeField] private float _launchDelay = 0.15f;

		/// <summary> The move time delay when using keys. </summary>
		[SerializeField] private float _moveDelay = 0.1f;

		/// <summary> The preview aimer gem Y position offset. </summary>
		[SerializeField] private float _aimerOffsetY = 0.5f;

		/// <summary> The line renderers end Y position offset. </summary>
		[SerializeField] private float _lineOffsetY = 0.5f;

		/// <summary> The line renderers raycast hit offset. </summary>
		[SerializeField] private float _lineHitOffset = 1.0f;

		/// <summary> The line renderers raycast hit top offset. </summary>
		[SerializeField] private float _lineHitTopOffset = -0.5f;

		/// <summary> The offset from the cursor to level column. </summary>
		[SerializeField] private float _cursorColumnOffset = 0.5f;

		/// <summary> The delay between auto shooting. </summary>
		[SerializeField] private float _autoShootTime = 3.0f;

		/// <summary> How fast the aimer gem flashes. </summary>
		[SerializeField] private float _aimerFlashSpeed = 5.0f;

		/// <summary> The maximum aimer flash alpha. </summary>
		[SerializeField] private float _aimerFlashAlphaMax = 1.0f;

		/// <summary> The minimum aimer flash alpha. </summary>
		[SerializeField] private float _aimerFlashAlphaMin = 0.25f;

		/// <summary> The input ID for gem launch. </summary>
		[SerializeField] private string _inputID_Launch = "LaunchGem";

		/// <summary> The input ID for cursor position. </summary>
		[SerializeField] private string _inputID_Cursor = "Cursor";

		/// <summary> The input ID for gem swapping. </summary>
		[SerializeField] private string _inputID_GemSwap = "GemSwap";

		/// <summary> The input ID for pausing. </summary>
		[SerializeField] private string _inputID_Pause = "Pause";

		/// <summary> Input reference. </summary>
		private Input _input;

		/// <summary> Current cell tilemap reference. </summary>
		private Tilemap _cellMap;

		/// <summary> Main camera reference. </summary>
		private Camera _cam;

		/// <summary> The cached Z position of the current camera. </summary>
		private float _camZ;

		/// <summary> The current color of the players aimer. </summary>
		private Color _currAimerColor;

		/// <summary> The current alpha color value of the players aimer. </summary>
		private float _currAimerAlpha;

		/// <summary> If the aimer is currently flashing alpha up or down. </summary>
		private int _currAimerFlashDirection;

		private float _currAimerFlashSpeed = 0f;

		/// <summary> Move delay timer </summary>
		private float _moveDelayTimer = 0f;

		/// <summary> Launch delay timer </summary>
		private float _launchDelayTimer = 0f;

		/// <summary> Auto shoot timer </summary>
		private float _autoShootTimer = 0f;

		/// <summary> Stores the cursors screen position. </summary>
		private Vector3 _screenCursorPos;

		/// <summary> If the player is shooting a gem. </summary>
		private bool _shooting = false;

		/// <summary> If the player is pressing the swap input. </summary>
		private bool _pressingSwap = false;

		/// <summary> If the players aimer is currently visible. </summary>
		private bool _aimerVisible = false;

		private bool _pausing = false;
		private bool _paused = false;

		#endregion

		#region Methods

		/// <summary> First automatic initialization. </summary>
		private void Awake()
		{
			_cam = Camera.main;
			_camZ = _cam.transform.position.z;

			_input = new Input();
			_input.Initialize();

			_currAimerColor = _aimer.color;
			SetAimerVisible(false);
			_currAimerFlashSpeed = _aimerFlashSpeed;
			_screenCursorPos = new Vector3(_input.GetCursorPositionX(), 0, _camZ);
		}

		public void SetCellMap(Tilemap tilemap)
		{
			_cellMap = tilemap;
		}

		public Vector3 GetAimerPosition()
		{
			return _aimer.transform.position;
		}

		public void UpdateLogic()
		{
			_moveDelayTimer += Time.deltaTime;
			_launchDelayTimer += Time.deltaTime;
			UpdateAimerFlash();
			UpdateAimer();
			UpdateCursorLevelControl();
			UpdateKeyboardLevelControl();
		}

		public void CheckForPause()
		{
			if (!_input.ActiveInputs[_inputID_Pause])
			{
				_pausing = false;
			}

			if (_input.ActiveInputs[_inputID_Pause])
			{
				_pausing = true;
			}

			if(!_paused && _pausing)
			{
				OnTriggeredPause();
				_paused = true;
			}
		}

		public void SetUnpaused()
		{
			_paused = false;
		}

		/// <summary>
		/// Sets the preview aimer gem.
		/// </summary>
		/// <param name="g"></param>
		public void SetAimerGem(MetaGem g)
		{
			_aimer.sprite = _world.CurrentPossibleGems[_world.CurrentPossibleGems.IndexOf(g)].aim_sprite;
		}

		public void SetAimerVisible(bool val)
		{
			_aimer.gameObject.SetActive(val);
			_aimLine.gameObject.SetActive(val);
			// upperAimLine.gameObject.SetActive(val);
			_aimerVisible = val;
		}

		public bool IsAimerVisible() { return _aimerVisible; }

		private void TryLeftMovement()
		{
			if (_moveDelayTimer < _moveDelay)
				return;

			if (_world.NextAvailableCells.ContainsKey(transform.position.x + Vector3.left.x))
			{
				transform.position = transform.position + Vector3.left;
				_moveDelayTimer = 0f;
			}
		}

		private void TryRightMovement()
		{
			if (_moveDelayTimer < _moveDelay)
				return;

			if (_world.NextAvailableCells.ContainsKey(transform.position.x + Vector3.right.x))
			{
				transform.position = transform.position + Vector3.right;
				_moveDelayTimer = 0f;
			}
		}

		private void LaunchGem()
		{
			_shooting = true;
			_world.SpawnGemFromQueue(transform.position);
		}

		private void UpdateCursorLevelControl()
		{
			// get new x pos of cursor
			_screenCursorPos.x = _input.GetCursorPositionX();

			// convert to world point
			Vector3 cursorToWorld = _cam.ScreenToWorldPoint(_screenCursorPos);

			Vector3Int cellPos = _cellMap.WorldToCell(cursorToWorld);

			if(_cellMap.HasTile(cellPos))
			{
				Vector2 worldCellPos = _cellMap.CellToWorld(cellPos);
				transform.position = new Vector2(worldCellPos.x, transform.position.y);
				return;
			}

			// round with offset
			float xPos = -((float)Math.Round(cursorToWorld.x + _cursorColumnOffset));
			// get nearest column
			if (_world.Columns.ContainsKey(xPos))
			{
				transform.position = new Vector2(xPos, transform.position.y);
			}
		}

		private void UpdateKeyboardLevelControl()
		{
			if(!_input.ActiveInputs[_inputID_Launch])
			{
				_shooting = false;
			}
			else if (_input.ActiveInputs[_inputID_Launch] && !_shooting && _launchDelayTimer > _launchDelay)
			{
				_launchDelayTimer = 0f;
				_autoShootTimer = 0f;
				LaunchGem();
			}

			_autoShootTimer += Time.deltaTime;
			UpdateAimerFlashSpeed(_autoShootTimer / _autoShootTime);

			if (_autoShootTimer >= _autoShootTime && !_shooting && _launchDelayTimer > _launchDelay)
			{
				_launchDelayTimer = 0f;
				_autoShootTimer = 0f;
				LaunchGem();
			}

			if (_input.GetHorizontal() > _horizontalKeyThreshold)
			{
				TryRightMovement();
			}
			else if (_input.GetHorizontal() < -_horizontalKeyThreshold)
			{
				TryLeftMovement();
			}

			if (_input.ActiveInputs[_inputID_GemSwap] && !_pressingSwap)
			{
				_world.SwapGem();
				PlaySFX("swap_gem");
				_pressingSwap = true;
			}
			if(!_input.ActiveInputs[_inputID_GemSwap])
			{
				_pressingSwap = false;
			}
		}

		private void UpdateAimer()
		{
			RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector3.up, 1000, _levelMask);

			if (hit)
			{
				Vector2 offset = new Vector2(hit.point.x, hit.point.y - _lineHitOffset);
				Vector3 selfPos = new Vector3(transform.position.x, transform.position.y - _lineOffsetY, transform.position.z);
				Vector2 topCellPos = _world.Columns[transform.position.x][0];
				Vector2 topPosOffset = new Vector3(topCellPos.x, topCellPos.y - _lineHitTopOffset);

				_aimLine.SetPosition(0, selfPos);
				_aimLine.SetPosition(1, offset);
				_upperAimLine.SetPosition(0, offset);
				_upperAimLine.SetPosition(1, topPosOffset);
			}

			if (_world.NextAvailableCells.ContainsKey(transform.position.x))
			{
				_aimer.transform.position = _world.NextAvailableCells[transform.position.x];
			}
		}

		private void UpdateAimerFlashSpeed(float autoShootProgress)
		{
			if (autoShootProgress < 0.25f)
			{
				_currAimerFlashSpeed = _aimerFlashSpeed;
				return;
			}
			if (autoShootProgress > 0.25f && autoShootProgress < 0.5f)
			{
				_currAimerFlashSpeed = _aimerFlashSpeed * 2.0f;
				return;
			}
			if (autoShootProgress > 0.5f && autoShootProgress < 0.75f)
			{
				_currAimerFlashSpeed = _aimerFlashSpeed * 3.0f;
				return;
			}
			if (autoShootProgress > 0.75f && autoShootProgress < 0.9f)
			{
				_currAimerFlashSpeed = _aimerFlashSpeed * 4.0f;
				return;
			}
			if (autoShootProgress > 0.9f)
			{
				_currAimerFlashSpeed = _aimerFlashSpeed * 5.0f;
				return;
			}
		}

		private void UpdateAimerFlash()
		{
			if (_currAimerFlashDirection == 0)
			{
				if (_currAimerAlpha < _aimerFlashAlphaMax)
				{
					_currAimerAlpha += Time.deltaTime * _currAimerFlashSpeed;
				}
				if (_currAimerAlpha >= _aimerFlashAlphaMax)
				{
					_currAimerFlashDirection = 1;
				}
			}

			if (_currAimerFlashDirection == 1)
			{
				if (_currAimerAlpha > _aimerFlashAlphaMin)
				{
					_currAimerAlpha -= Time.deltaTime * _currAimerFlashSpeed;
				}
				if (_currAimerAlpha <= _aimerFlashAlphaMin)
				{
					_currAimerFlashDirection = 0;
				}
			}

			_currAimerColor.a = _currAimerAlpha;
			_aimer.color = _currAimerColor;
		}

		#endregion
	}
}

