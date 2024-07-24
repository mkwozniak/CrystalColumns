using System.Collections.Generic;
using System;
using UnityEngine;

namespace Wozware.CrystalColumns
{
	/// <summary>
	/// The object for Gems in movement. 
	/// </summary>
	public class Gem : MonoBehaviour
	{
		#region Public Members

		/// <summary> Valid update states for flying gems. </summary>
		public static HashSet<GameStates> VALID_STATES = new HashSet<GameStates>()
		{
			GameStates.Gameplay,
			GameStates.GameDrop,
		};

		public static int LAST_GEM_UID = 0;

		#region Events

		// events which gem may raise
		public Func<GameStates> OnGetCurrentState;
		public Action<Gem, List<Vector2>, Bounds> OnCheckColumnCollision;
		public GemPlacing OnPlaceGem;
		public Action<string> OnPlaySFX;

		public event Action<Gem> OnGemDestroy;

		#endregion

		/// <summary> Unity sprite renderer reference. </summary>
		public SpriteRenderer spriteRenderer;

		#endregion

		#region Private Members

		/// <summary> The name of the gem in motion. </summary>
		private string _gemName;

		/// <summary> If the gem is attached. </summary>
		private bool _attached;

		/// <summary> The collision bounds of the gem. </summary>
		private Bounds _bounds;

		/// <summary> The positions of the column the gem is flying in. </summary>
		private List<Vector2> _column;

		/// <summary> The current velocity of the gem. </summary>
		private Vector2 _velocity;

		/// <summary> If this gem is currently allowed to update its movement. </summary>
		private bool _updateMovement = false;

		private int _uid;

		#endregion

		/// <summary> Initializes a gem  </summary>
		/// <param name="sprite"> The sprite to use. </param>
		/// <param name="gemName"> The name of the gem. </param>
		/// <param name="column"> A list of positions which hold the gems column of cells. </param>
		public void InitializeGem(Sprite sprite, string gemName, List<Vector2> column, float speed)
		{
			if (gemName == "empty")
			{
				Destroy(gameObject);
				return;
			}
			
			_gemName = gemName;
			_column = column;
			_velocity = new Vector2(0, speed);
			_bounds = new Bounds(transform.position, new Vector2(1, 1));
			_updateMovement = true;
			spriteRenderer.sprite = sprite;
			LAST_GEM_UID++;
			_uid = LAST_GEM_UID;
		}

		/// <summary> Attaches a gem by placing a Unity Tile then destroying this GameObject. </summary>
		/// <param name="pos"></param>
		public void Attach(Vector2 pos)
		{
			// attach the gem temporarily
			_attached = true;
			transform.position = pos;

			// place a Tile version of this gem
			OnPlaceGem?.Invoke(pos, _gemName, uid: _uid);
			// play sfx
			OnPlaySFX?.Invoke("gemhit0");

			// destroy
			Destroy(gameObject, 0.1f);
		}

		public void SwitchedState(GameStates state)
		{
			if (VALID_STATES.Contains(state))
			{
				_updateMovement = true;
				return;
			}
			_updateMovement = false;
		}

		public void DestroyGem()
		{
			Destroy(gameObject);
		}

		/// <summary> Get the name of the gem. </summary>
		/// <returns>The name of the gem in motion. </returns>
		public string GetName() 
		{ 
			return _gemName; 
		}

		public void Update()
		{
			// if not updating movement or is already attached on this frame, return
			if (!_updateMovement || _attached)
				return;

			// update delta position
			transform.position += new Vector3(_velocity.x, _velocity.y, 0) * Time.deltaTime;

			// update bounds to position
			_bounds.center = transform.position;

			// invoke event to check collision
			OnCheckColumnCollision?.Invoke(this, _column, _bounds);
		}

		/// <summary> Automatically called by Unity when object is destroyed. </summary>
		public void OnDestroy()
		{
			// raise the ondestroy event to any listeners
			OnGemDestroy?.Invoke(this);
		}
	}
}
	
