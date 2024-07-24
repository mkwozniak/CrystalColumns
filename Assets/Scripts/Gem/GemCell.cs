using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wozware.CrystalColumns
{
	/// <summary> The object for each cell in the grid. </summary>
	public sealed class GemCell
	{
		/// <summary> 
		/// How many checks before giving up settling 
		/// (should be the height to the highest cell in the level + 1). 
		/// </summary>
		private static int MAX_SETTLECHECKS = 64;

		// hidden reference for world
		[HideInInspector] public World world;

		#region Members

		// Fields
		public bool ChainChecked
		{
			get { return _chainChecked; }
			set { _chainChecked = value; }
		}

		public Bounds Bounds
		{
			get { return _bounds; }
			set { _bounds = value; }
		}

		public Vector2 Position
		{
			get { return _position; }
		}

		public string GemWithin
		{
			get { return _gemWithin; }
			set { _gemWithin = value; }
		}

		/// <summary> The bounds of the cells. </summary>
		private Bounds _bounds;

		/// <summary> The name of the gem within. </summary>
		private string _gemWithin;

		/// <summary> Position of the cell in grid coordinates. </summary>
		private Vector2 _position;

		/// <summary> If the cell has a gem. </summary>
		private bool _hasGem;

		/// <summary> If the cell has been checked in a chain already </summary>
		private bool _chainChecked;

		/// <summary> The queue to store any nearby cells that are checking for a valid chain </summary>
		private Queue<Vector2> _chainQueue;

		/// <summary> The positions of any valid matching chained cells </summary>
		private List<Vector2> _chainConnected;

		// The neighbors of this cell.
		public Neighbor northCell;
		public Neighbor southCell;
		public Neighbor westCell;
		public Neighbor eastCell;

		#endregion

		#region Initialization 

		public GemCell(Vector2 position, World world)
		{
			this.world = world;
			_position = position;
			_gemWithin = "";
			_hasGem = false;
			_chainChecked = false;
			_chainQueue = new Queue<Vector2>();
			_chainConnected = new List<Vector2>();
			northCell = new Neighbor(true);
			southCell = new Neighbor(true);
			westCell = new Neighbor(true);
			eastCell = new Neighbor(true);
			_bounds = new Bounds(position, new Vector2(1, 1));
		}

		#endregion

		#region Standard Methods
		public void SetNorthCell(Vector2 pos) 
		{ 
			northCell = new Neighbor(pos); 
		}

		public void SetSouthCell(Vector2 pos) 
		{ 
			southCell = new Neighbor(pos); 
		}

		public void SetWestCell(Vector2 pos) 
		{
			westCell = new Neighbor(pos); 	
		}

		public void SetEastCell(Vector2 pos) 
		{ 
			eastCell = new Neighbor(pos); 
		}

		public void SetGemWithin(string value)
		{
			_gemWithin = value; 
			_hasGem = true;
		}

		/// <summary>
		/// Fills the cell and calls to check for any valid linear chains.
		/// </summary>
		/// <param name="gem"></param>
		public void FillCell(Gem gem)
		{
			gem.Attach(_position);
			_gemWithin = gem.GetName();
			_hasGem = true;
			// check the chain
			world.StartCheckChain(_position, ref _chainQueue, ref _chainConnected);
		}

		/// <summary>
		/// Calls upon world to perform a check for any linear chains of similar type.
		/// </summary>
		public void StartCheckChain()
		{
			// if this cell has been chain checked already or there is its empty, return
			if (_chainChecked || _gemWithin.Length <= 0)
				return;

			// clear the chain list
			_chainConnected.Clear();
			// give the position and relevant refs to check the chain
			world.StartCheckChain(_position, ref _chainQueue, ref _chainConnected);
		}

		/// <summary>
		/// Clears a cell with an optional explosion for that particular cell type.
		/// </summary>
		/// <param name="spawnExplosion"></param>
		public void ClearCell(bool spawnExplosion)
		{
			world.ClearCell(_position, spawnExplosion, _gemWithin);
			_gemWithin = "";
			_hasGem = false;
			_chainChecked = false;
			_chainQueue.Clear();
		}

		/// <summary>
		/// Settles the cell by continuously checking for the next available north cell
		/// </summary>
		public void Settle()
		{
			if (!northCell.valid)
				return;

			Vector2 currentCell = _position;

			int checks = 0;
			while (checks < MAX_SETTLECHECKS)
			{
				// if the north cell is not valid, break settling
				if (!world.Cells[currentCell].northCell.valid)
					return;

				Vector2 northCell = world.Cells[currentCell].northCell.position;

				// if the north cell is gem filled, break settling
				if (world.Cells[northCell]._gemWithin.Length != 0)
					return;

				// if the north cell is empty, swap and keep settling
				if (world.Cells[northCell]._gemWithin.Length == 0)
				{
					world.GemSwapCopy(currentCell, northCell);
					currentCell = northCell;
				}

				checks += 1;
			}

			Debug.LogWarning("Settling exceeded maximum settle checks.");
		}

		/// <summary>
		/// Unsubscribes from the worlds events completely.
		/// </summary>
		/// <param name="world"></param>
		public void RemoveFromWorld(World world)
		{
			world.OnClearCells -= ClearCell;
			world.OnDestroyWorld -= RemoveFromWorld;
		}

		#endregion
	}
}
	
