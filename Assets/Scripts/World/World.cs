using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

namespace Wozware.CrystalColumns
{
	public class World : MonoBehaviour
	{
		#region Public Members

		public static int MAX_CHAIN_CHECKS = 128;
		public static string FLAG_CLEAR = "clear";
		public static string FLAG_EMPTY = "empty";

		#region Events

		/// <summary> Invoked when the world finishes initializing. </summary>
		public event Action OnFinishedInitialize;

		/// <summary> Invoked when the world wants to switch the game state. </summary>
		public event StateSwitching OnSwitchState;

		/// <summary> Invoked when the game state was switched. </summary>
		public event Action<GameStates> OnStateSwitched;

		/// <summary> Invoked when the world finishes a single drop. </summary>
		public event Action OnEndDropSingle;

		/// <summary> Invoked when an amount of needed gems is removed. </summary>
		public event Action<int> OnRemoveGemsNeeded;

		/// <summary> Invoked when the world needs to add to existing gems. </summary>
		public event Action<bool> OnAddGemExisting;

		/// <summary> Invoked when the world clears its cells. </summary>
		public event Action<bool> OnClearCells;

		/// <summary> Invoked when the world needs to play sfx. </summary>
		public event Action<string> OnPlaySFX;

		/// <summary> Invoked when the world needs to spawn explode fx. </summary>
		public event Action<string, Vector2> OnSpawnGemExplodeFX;

		/// <summary> Invoked when the world needs to spawn attach fx. </summary>
		public event Action<string, Vector2> OnSpawnAttachFX;

		/// <summary> Invoked when the world is destroyed. </summary>
		public event Action<World> OnDestroyWorld;

		public event Action OnDestroyFlyingGems;
		public event Action OnBottomReached;

		#endregion

		/// <summary> Gem preparer reference </summary>
		public GemPreparer GemPreparer;

		/// <summary> The object to enable when drop is active. </summary>
		public GameObject SpriteDropCover;

		/// <summary> Default fallback drop amount. </summary>
		public int DefaultDropAmount = 24;

		/// <summary> The shadow tile prefab </summary>
		public TileBase GemShadowTilePrefab;

		/// <summary> Flying gem prefab. </summary>
		public GameObject FlyingGemPrefab;

		public int OfficialSeed = 123456;
		public bool IsOfficialSeed;

		/// <summary> All zone cells. </summary>
		public Dictionary<Vector2, GemCell> Cells = new();

		/// <summary> Zone cells organized by X position per column to avoid needless data iteration. </summary>
		public Dictionary<float, List<Vector2>> Columns = new();

		/// <summary> Keeps track of each columns next available empty cell. </summary>
		public Dictionary<float, Vector2> NextAvailableCells = new();

		/// <summary> Cached current possible gems to spawn for this level / stage. </summary>
		[HideInInspector] public List<MetaGem> CurrentPossibleGems = new();

		public int LastGemAmountDestroyed
		{
			get { return _amountLastGemsDestroyed; }
			set { _amountLastGemsDestroyed = value; }
		}

		public float GemSpeed
		{
			get { return _currentGemSpeed; }
			set { _currentGemSpeed = value; }
		}

		#endregion

		#region Private Members

		/// <summary> The cell tile map. </summary>
		private Tilemap _cellMap;

		/// <summary> The gem tile map. </summary>
		private Tilemap _gemMap;

		/// <summary> The ghost gem tile map. </summary>
		private Tilemap _ghostGemMap;

		/// <summary> The shadow tile map. </summary>
		private Tilemap _shadowMap;

		/// <summary> The hatch tile map. </summary>
		private Tilemap _hatchMap;

		/// <summary> Current explosion chain positions. </summary>
		private Vector2[] _currentChain;

		/// <summary> Current previous drop position. </summary>
		private Vector2 _previousDropPos = new Vector2(int.MaxValue, int.MaxValue);

		/// <summary> Current possible hatches in the level. </summary>
		private List<Vector2> _possibleHatches = new();

		/// <summary> Current level top boundary cell positions. </summary>
		private List<Vector2> _topBoundaryCells = new();

		/// <summary> Current previous drop gem. </summary>
		private string _previousDropGem = "";

		/// <summary> Current gem drop speed. </summary>
		private float _currentGemSpeed;

		/// <summary> The last amount of gems destroyed in a cell explosion. </summary>
		private int _amountLastGemsDestroyed = 0;

		private Dictionary<int, Vector3Int> _activeGhostGems = new();

		#endregion

		#region Initialization

		/// <summary> Initializes the world cell map, prepares the gem queue, and initiates the first drop. </summary>
		public void InitializeWorld()
		{
			if(IsOfficialSeed)
			{
				Random.InitState(OfficialSeed);
			}

			Columns.Clear();
			_topBoundaryCells.Clear();
			NextAvailableCells.Clear();
			Cells.Clear();
			Gem.LAST_GEM_UID = 0;

			// loop through all cells within the Cell Map
			foreach (var position in _cellMap.cellBounds.allPositionsWithin)
			{
				// if it doesn't have a tile, skip
				if (!_cellMap.HasTile(position))
					continue;

				// get centered world position of tile
				Vector2 worldPos = _cellMap.GetCellCenterWorld(position);
				// add cell at that position to zone cell dictionary
				Cells[worldPos] = new GemCell(worldPos, this);

				// subscribe the cell to world events it needs
				OnClearCells += Cells[worldPos].ClearCell;
				OnDestroyWorld += Cells[worldPos].RemoveFromWorld;
				// add the pos to columns
				if (!Columns.ContainsKey(worldPos.x))
				{
					Columns[worldPos.x] = new List<Vector2>();
				}
				Columns[worldPos.x].Add(worldPos);		
			}

			// find neighbors
			foreach (Vector2 pair in Cells.Keys)
			{
				// neighbor positions
				Vector2 northPos = new Vector2(pair.x, pair.y + 1);
				Vector2 southPos = new Vector2(pair.x, pair.y - 1);
				Vector2 westPos = new Vector2(pair.x - 1, pair.y);
				Vector2 eastPos = new Vector2(pair.x + 1, pair.y);

				// check positions
				if (Cells.ContainsKey(northPos))
					Cells[pair].SetNorthCell(northPos);
				if (Cells.ContainsKey(southPos))
					Cells[pair].SetSouthCell(southPos);
				if (Cells.ContainsKey(westPos))
					Cells[pair].SetWestCell(westPos);
				if (Cells.ContainsKey(eastPos))
					Cells[pair].SetEastCell(eastPos);
			}

			// reverse columns for easier settling
			foreach (KeyValuePair<float, List<Vector2>> pair in Columns)
			{
				Columns[pair.Key].Reverse();
				NextAvailableCells[pair.Value[0].x] = pair.Value[0];
				_topBoundaryCells.Add(pair.Value[0]);
			}

			// prepare the gem preparer and hatches
			GemPreparer.World = this;
			GemPreparer.GenerateNewGemQueue();
			GemPreparer.InitialPreparation();
			PrepareHatches();
		}

		/// <summary> Prepares the list of hatches and their positions. </summary>
		public void PrepareHatches()
		{
			_possibleHatches = new List<Vector2>();
			foreach (var position in _hatchMap.cellBounds.allPositionsWithin)
			{
				// if it doesn't have a tile, skip
				if (!_hatchMap.HasTile(position))
					continue;

				// get centered world position of tile
				Vector2 worldPos = _cellMap.GetCellCenterWorld(position);
				_possibleHatches.Add(worldPos);
			}
		}

		public void SetWorldTilemaps(Tilemap cellmap, Tilemap gemMap, Tilemap ghostGemMap, Tilemap shadowMap, Tilemap hatchMap)
		{
			_cellMap = cellmap;
			_gemMap = gemMap;
			_ghostGemMap = ghostGemMap;
			_shadowMap = shadowMap;
			_hatchMap = hatchMap;
		}

		#endregion

		#region Gem & Drop Methods

		/// <summary> Spawns a flying gem from the queue. </summary>
		/// <param name="pos"> The grid position to spawn the gem. </param>
		public void SpawnGemFromQueue(Vector2 pos)
		{
			// pop gem data from queue
			MetaGem metaGem = GemPreparer.PopGemFromQueue();
			
			// instantiate new gem object
			Gem newGem = Instantiate(FlyingGemPrefab, pos, Quaternion.identity).GetComponent<Gem>();

			// let gem listen to when state switches
			OnStateSwitched += newGem.SwitchedState;
			// listen to when the gem gets destroyed
			newGem.OnGemDestroy += RemoveFlyingGem;
			// listen to when the gem wants to place itself
			newGem.OnPlaceGem = PlaceGem;
			// listen to when the gem needs to check collision
			newGem.OnCheckColumnCollision = CheckColumnCollision;
			// listen to when the gem needs to play sfx
			newGem.OnPlaySFX = OnPlaySFX;
			// listen to when gem needs to be destroyed
			OnDestroyFlyingGems += newGem.DestroyGem;

			// initialize the gem
			newGem.InitializeGem(metaGem.sprite, metaGem.gemName, Columns[pos.x], _currentGemSpeed);
			// raise event to add an existing gem
			OnAddGemExisting(false);

			// if the next available cell at this position isnt free
			if (NextAvailableCells[pos.x] != null)
				PlaceGem(NextAvailableCells[pos.x], metaGem.gemName, withShadow: false, ghostTile: true);

			// get the next available cell
			Vector2 nextAvailable = NextAvailableCells[pos.x];
			
			if (Cells[nextAvailable].southCell.valid) // check if its south cell is valid
			{
				Vector2 southPos = Cells[nextAvailable].southCell.position; // get the south cells position
				NextAvailableCells[pos.x] = Cells[southPos].Position; // assign the next available cell as that south position
			}
		}

		public void SwapGem()
		{
			GemPreparer.SwapGem();
		}

		/// <summary> Spawn a flying gem by name. </summary>
		/// <param name="pos"> The grid position to spawn the gem. </param>
		/// <param name="gem"> The name of the gem to spawn. </param>
		public void SpawnGem(Vector2 pos, string gem)
		{	
			// get new gem data from name
			MetaGem metaGem = GetMetaGem(gem);	

			// instantiate new gem object
			Gem newGem = Instantiate(FlyingGemPrefab, pos, Quaternion.identity).GetComponent<Gem>();

			// let gem listen to when state switches
			OnStateSwitched += newGem.SwitchedState;
			// listen to when the gem gets destroyed
			newGem.OnGemDestroy += RemoveFlyingGem;
			// listen to when the gem wants to place itself
			newGem.OnPlaceGem = PlaceGem;
			// listen to when the gem needs to check collision
			newGem.OnCheckColumnCollision = CheckColumnCollision;
			// listen to when the gem needs to play sfx
			newGem.OnPlaySFX = OnPlaySFX;
			// listen to when gem needs to be destroyed
			OnDestroyFlyingGems += newGem.DestroyGem;

			// initialize gem
			newGem.InitializeGem(metaGem.sprite, metaGem.gemName, Columns[pos.x], _currentGemSpeed);
			// raise event to add an existing gem
			OnAddGemExisting(false);		
		}

		/// <summary> Swaps gem data between two cells. </summary>
		/// <param name="origPos"> Original grid position. </param>
		/// <param name="newPos"> The grid position to swap with. </param>
		public void GemSwapCopy(Vector2 origPos, Vector2 newPos)
		{
			// place a tile version of original gem in the new cell
			Cells[newPos].SetGemWithin(Cells[origPos].GemWithin);
			PlaceGem(Cells[newPos].Position, Cells[origPos].GemWithin);
			Cells[origPos].ClearCell(false);

			// update next available cells
			UpdateNextAvailableCell(newPos.x);
		}

		/// <summary> Places a gem on the gem tilemap. </summary>
		/// <param name="pos"> Grid position to place gem. </param>
		/// <param name="name"> The name of the gem to place. </param>
		/// <param name="withShadow"> Place a shadow with this gem. </param>
		/// <param name="ghostTile"> Place a ghost tile gem instead. </param>
		public void PlaceGem(Vector2 pos, string name, int uid = -1, bool withShadow = true, bool ghostTile = false)
		{
			if(_gemMap == null)
			{
				return;
			}

			// get the cell position from the world position
			Vector3Int tilePos = _gemMap.WorldToCell(pos);

			// create a meta gem with the name
			MetaGem metaGem = GetMetaGem(name);

			// place a shadow if no clear flag
			if (name != FLAG_CLEAR && withShadow)
				_shadowMap.SetTile(tilePos, GemShadowTilePrefab);

			// if no empty flag 
			if (metaGem.gemName != FLAG_EMPTY)
			{
				// if this isnt a ghost tile that is being placed
				if (!ghostTile)
				{
					// place a real gem at the map
					_gemMap.SetTile(tilePos, metaGem.tile);
					if(_activeGhostGems.ContainsKey(uid))
					{
						_ghostGemMap.SetTile(_activeGhostGems[uid], null);
						_activeGhostGems.Remove(uid);
					}
					OnSpawnAttachFX?.Invoke(name, pos);
				}
				else
				{
					// place a ghost tile at the map
					_ghostGemMap.SetTile(tilePos, metaGem.ghostTile);
					_activeGhostGems[Gem.LAST_GEM_UID] = tilePos;
				}
			}
			else if (name == FLAG_CLEAR)
			{
				// this is a clear flag placement, delete the tile at that position
				_shadowMap.SetTile(tilePos, null);
				_gemMap.SetTile(tilePos, null);
				return;
			}
		}

		/// <summary> Spawns a random gem drop. </summary>
		public void SpawnGemDrop()
		{
			// get new random gem and pos
			MetaGem newGem = GetRandomNonRepeatingGem(_previousDropGem);
			_previousDropGem = newGem.gemName;
			
			Vector2 newRandPos = _possibleHatches[Random.Range(0, _possibleHatches.Count)];

			// cache gems above, east and west
			string gemAbove = "", gemAboveEast = "", gemAboveWest = "";

			// make sure new random pos doesn't equal last pos
			while (newRandPos == _previousDropPos)
				newRandPos = _possibleHatches[Random.Range(0, _possibleHatches.Count)];
			
			// loop through column
			for (int i = Columns[newRandPos.x].Count - 1; i > -1; i--)
			{
				Vector2 cellPos = Columns[newRandPos.x][i];

				// if no gem, continue
				if (Cells[cellPos].GemWithin.Length <= 0)
				{
					continue;
				}

				// gem found
				gemAbove = Cells[cellPos].GemWithin;
				if (Cells[cellPos].southCell.valid)
				{
					CacheDropNeighbors(cellPos, ref gemAboveWest, ref gemAboveEast);
				}
			}
		
			// make sure the new drop gem is not the same as the cached above, east or west gems
			// reduces stack explosions from random luck
			while (newGem.gemName == gemAbove || newGem.gemName == gemAboveEast || newGem.gemName == gemAboveWest)
			{
				// make sure the gem is not the same as the last gem spawned
				newGem = GetRandomNonRepeatingGem(_previousDropGem);
				_previousDropGem = newGem.gemName;
			}
			
			// spawn the new gem
			_previousDropPos = newRandPos;
			SpawnGem(_previousDropPos, newGem.gemName);

			// end the single drop
			OnEndDropSingle?.Invoke();
		}

		public void RemoveFlyingGem(Gem g)
		{
			// remove from existing count of flying gems
			OnAddGemExisting?.Invoke(true);

			// remove all event listening for this gem as its about to be destroyed
			OnStateSwitched -= g.SwitchedState;
			g.OnGemDestroy -= RemoveFlyingGem;
			OnDestroyFlyingGems -= g.DestroyGem;
		}

		/// <summary> Generate new MetaGem data given a gem name. </summary> 
		/// <param name="name"> The name of the gem to generate. </param>
		/// <returns> new MetaGem with data of a Gem. </returns>
		public MetaGem GetMetaGem(string name)
		{
			MetaGem returnObject = new MetaGem("empty", null, null, null, null);
			foreach (MetaGem g in CurrentPossibleGems)
				if (g.gemName == name)
					returnObject = g;
			return returnObject;
		}

		/// <summary> Returns a different MetaGem from the given previous name. </summary>
		/// <param name="previous"> The previous gem to avoid generating again. </param>
		/// <returns> New random MetaGem that is different from previous. </returns>
		public MetaGem GetRandomNonRepeatingGem(string previous)
		{
			int newGemID = Random.Range(0, CurrentPossibleGems.Count);
			MetaGem newGem = CurrentPossibleGems[newGemID];

			// keep generating a random gem until it is different
			while (previous == newGem.gemName)
			{
				newGemID = Random.Range(0, CurrentPossibleGems.Count);
				newGem = CurrentPossibleGems[newGemID];
			}

			return newGem;
		}

		/// <summary> Updates a given columns cell availability. </summary>
		/// <param name="columnID"> The ID of the column to update. </param>
		public void UpdateNextAvailableCell(float columnID)
		{
			// reverse loop through column 
			for (int i = Columns[columnID].Count - 1; i >= 0; i--)
			{
				if (i >= Columns[columnID].Count)
					return;

				Vector2 pos = Columns[columnID][i];
				// if there is no gem there its the next available cell
				if (Cells[pos].GemWithin.Length == 0)
				{
					NextAvailableCells[pos.x] = pos;
					continue;
				}
				// column completely filled, gg
				else if(i == Columns[columnID].Count - 1)
				{
					OnBottomReached();
				}
			}
		}

		public void ClearAvailableCells(float columnID)
		{
			// reverse loop through column 
			for (int i = Columns[columnID].Count - 1; i >= 0; i--)
			{
				// clear gem in each cell
				Cells[Columns[columnID][i]].GemWithin = "";
			}

			// clear the column list
			Columns[columnID].Clear();
		}

		#endregion

		#region Standard Methods

		/// <summary> Checks if a sprite exists in a cell in the Gem Map. </summary>
		/// <param name="pos"> The position to check. </param>
		/// <returns> True if sprite exists in that cell position. </returns>
		public bool SpriteInCell(Vector2 pos)
		{
			Vector3Int tilePos = _gemMap.WorldToCell(pos);
			if (_gemMap.GetTile(tilePos) != null)
				return true;
			else
				return false;
		}

		#endregion

		#region Chain Methods

		/// <summary> 
		/// Checks each cell in each column in standard order.
		/// Due to the vast possibilities a player can encounter this is called each post chain check.
		/// </summary>
		public void StartCheckPostChain()
		{
			foreach (KeyValuePair<float, List<Vector2>> pair in Columns)
			{
				foreach(Vector2 pos in pair.Value)
				{
					if (Cells[pos].GemWithin.Length > 0)
						Cells[pos].StartCheckChain();
				}
			}
		}

		/// <summary> Starts the chain checking sequence with east west first. </summary>
		/// <param name="cell"> The cell to start at. </param>
		/// <param name="chainQueue"> The chain queue to pass on. </param>
		/// <param name="chainConnected"> The connected chain to pass on. </param>
		public void StartCheckChain(Vector2 cell, ref Queue<Vector2> chainQueue, ref List<Vector2> chainConnected)
		{
			CheckChainEastWest(cell, ref chainQueue, ref chainConnected);
		}

		/// <summary> Checks if the list of valid chained positions is valid for a stack clear. </summary>
		/// <param name="chainConnected"> The chain of connected cells to confirm. </param>
		/// <returns> True if the chain is valid for clearance. </returns>
		public bool ChainConfirm(ref List<Vector2> chainConnected)
		{
			bool didFind;
			if (chainConnected.Count >= 4)
				didFind = true;
			else
				didFind = false;
			if (!didFind)
				foreach (Vector2 cell in chainConnected)
					Cells[cell].ChainChecked = false;
			return didFind;
		}

		/// <summary> Given a cell position, adds any matching gems in E-W to the connected chain. Calls N-S checking if no valid chain. </summary>
		/// <param name="cell"> The cell that the chain starts from. This cells gem will be match checked. </param>
		/// <param name="chainQueue"> The ref chain queue. </param>
		/// <param name="chainConnected"> The ref connected chain. </param>
		private void CheckChainEastWest(Vector2 cell, ref Queue<Vector2> chainQueue, ref List<Vector2> chainConnected)
		{
			Vector2 currentCell = cell;
			chainConnected.Add(cell);
			int currentDir = 0;

			int checks = 0;
			while (checks < MAX_CHAIN_CHECKS)
			{
				// check west cells, add any matches to the connected chain. 
				// once there is no match, switch directions to east and perform the same logic.
				if (currentDir == 0)
				{		
					// next direction if no west cell
					if (!Cells[currentCell].westCell.valid)
					{
						currentCell = cell;
						currentDir = 1;
						continue;
					}
					
					Vector2 westCell = Cells[currentCell].westCell.position;
			
					// next direction if no gem in west cell or gem doesnt match, switch directions to east
					if (Cells[westCell].GemWithin.Length == 0 || Cells[westCell].GemWithin != Cells[cell].GemWithin)
					{
						currentCell = cell;
						currentDir = 1;
						continue;
					}

					// this cell has a matching gem
					currentCell = Cells[currentCell].westCell.position;
					chainConnected.Add(currentCell);
					continue;
				}

				// check east cells
				// finish if no east cell
				if (!Cells[currentCell].eastCell.valid)
					break;
				Vector2 eastCell = Cells[currentCell].eastCell.position;

				// finish if no gem in west cell or gem doesnt match
				if (Cells[eastCell].GemWithin.Length == 0 || Cells[eastCell].GemWithin != Cells[cell].GemWithin)
					break;

				// conditions are fine, this is a match
				currentCell = Cells[currentCell].eastCell.position;
				chainConnected.Add(currentCell);

				checks += 1;
			}
			
			// check if the connected chain has enough for an explosion.
			// if it does, call the recursive flood fill method which will catch any 
			// trailing gems of the same type as well as north or south chains too.
			if (ChainConfirm(ref chainConnected))
			{
				chainConnected.Clear();
				StartFloodFillChain(cell, ref chainQueue, ref chainConnected);
				return;
			}
			else // if it doesn't, clear the connected chain and then call to check the north and south chain the same way.
			{
				chainConnected.Clear();
				CheckChainNorthSouth(cell, ref chainQueue, ref chainConnected);
			}		
		}

		/// <summary>
		/// Given a cell position, adds any matching gems in E-W to the connected chain. 
		/// Calls recursive flood fill to explode all connected gems if a chain is found, otherwise clears the connected chain.
		/// </summary>
		/// <param name="cell"> The cell that the chain starts from. This cells gem will be match checked. </param>
		/// <param name="chainQueue"> The ref chain queue. </param>
		/// <param name="chainConnected"> The ref connected chain. </param>
		private void CheckChainNorthSouth(Vector2 cell, ref Queue<Vector2> chainQueue, ref List<Vector2> chainConnected)
		{
			Vector2 currentCell = cell;
			chainConnected.Add(cell);
			string currentDir = "north";

			int checks = 0;
			while (checks < MAX_CHAIN_CHECKS)
			{
				if (currentDir == "north")
				{
					// finish if no west cell
					if (!Cells[currentCell].northCell.valid)
					{
						currentCell = cell;
						currentDir = "south";
						continue;
					}
					Vector2 northCell = Cells[currentCell].northCell.position;
					// finish if no gem in west cell or gem doesnt match
					if (Cells[northCell].GemWithin.Length == 0 || Cells[northCell].GemWithin != Cells[cell].GemWithin)
					{
						currentCell = cell;
						currentDir = "south";
						continue;
					}

					// conditions are fine, this is a match
					currentCell = Cells[currentCell].northCell.position;
					chainConnected.Add(currentCell);
					continue;
				}

				// check south cells
				// finish if no south cell
				if (!Cells[currentCell].southCell.valid)
					break;
				Vector2 southCell = Cells[currentCell].southCell.position;
				// finish if no gem in south cell or gem doesnt match
				if (Cells[southCell].GemWithin.Length == 0 || Cells[southCell].GemWithin != Cells[cell].GemWithin)
					break;

				// conditions are fine, this is a match
				currentCell = Cells[currentCell].southCell.position;
				chainConnected.Add(currentCell);

				checks += 1;
			}

			if (ChainConfirm(ref chainConnected))
			{
				chainConnected.Clear();
				StartFloodFillChain(cell, ref chainQueue, ref chainConnected);
			}
			else
				chainConnected.Clear();
		}

		/// <summary> Starts the flood fill chain check for any trailing gems near the connected chain. </summary>
		/// <param name="cell"> The cell that the check originated from. </param>
		/// <param name="chainQueue"> The ref chain queue. </param>
		/// <param name="chainConnected"> The ref connected chain. </param>
		private void StartFloodFillChain(Vector2 cell, ref Queue<Vector2> chainQueue, ref List<Vector2> chainConnected)
		{
			// clear the chain queue
			chainQueue.Clear();
			// push the first cell
			chainQueue.Enqueue(cell);

			// while the chain queue has members, there is still more to check
			// it will quickly be empty, the dequeued position is the cell to check
			while (chainQueue.Count > 0)
				FloodFillChainCheck(Cells[cell].GemWithin, chainQueue.Dequeue(), ref chainQueue, ref chainConnected);

			// start to mark these cells to explode
			BufferExplodeCells(ref chainConnected);
		}

		/// <summary>
		/// Given a matching cell, enqueues any neighboring cells into the chain queue to also be checked by this same method.
		/// </summary>
		/// <param name="gemName"> The name of the gem to match. </param>
		/// <param name="cell"> The cell that is being filled. </param>
		/// <param name="chainQueue"> The ref chain queue. </param>
		/// <param name="chainConnected"> The ref connected chain. </param>
		private void FloodFillChainCheck(string gemName, Vector2 cell, ref Queue<Vector2> chainQueue, ref List<Vector2> chainConnected)
		{
			GemCell checkCell = Cells[cell];
			if (checkCell.ChainChecked || checkCell.GemWithin.Length == 0 || checkCell.GemWithin != gemName)
				return;

			chainConnected.Add(cell);
			checkCell.ChainChecked = true;

			// flood fill into neighbors
			if (checkCell.westCell.valid)
				chainQueue.Enqueue(checkCell.westCell.position);
			if (checkCell.eastCell.valid)
				chainQueue.Enqueue(checkCell.eastCell.position);
			if (checkCell.northCell.valid)
				chainQueue.Enqueue(checkCell.northCell.position);
			if (checkCell.southCell.valid)
				chainQueue.Enqueue(checkCell.southCell.position);
		}

		private void CacheDropNeighbors(Vector2 cellPos, ref string gemAboveWest, ref string gemAboveEast)
		{
			// cache the settling spot for west and east gems
			if (Cells[Cells[cellPos].southCell.position].westCell.valid)
				gemAboveWest = Cells[Cells[Cells[cellPos].southCell.position].westCell.position].GemWithin;
			if (Cells[Cells[cellPos].southCell.position].eastCell.valid)
				gemAboveEast = Cells[Cells[Cells[cellPos].southCell.position].eastCell.position].GemWithin;
		}

		#endregion

		#region Cell Methods

		public bool ExplodeNextCell()
		{
			if(_amountLastGemsDestroyed >= _currentChain.Length)
			{
				return false;
			}

			Vector2 cell = _currentChain[_amountLastGemsDestroyed];
			MetaGem metaGem = GetMetaGem(Cells[cell].GemWithin);

			OnRemoveGemsNeeded(1);

			// clear the cell
			Cells[cell].ClearCell(true);
			UpdateNextAvailableCell(cell.x);
			_amountLastGemsDestroyed += 1;
			return true;
		}

		public void FinishExplodingCells()
		{
			// clear the current chain
			_currentChain = new Vector2[0];
		}

		/// <summary> Clears all cells in the level. </summary>
		public void ClearCells()
		{
			_amountLastGemsDestroyed = 0;
			FinishExplodingCells();

			// notify flying gems to clear via event
			if (OnDestroyFlyingGems != null)
			{
				Debug.Log("Destroying Flying Gems");
				OnDestroyFlyingGems();
			}

			// notify cells to clear via event
			if (OnClearCells != null)
			{
				OnClearCells(true);
			}

			// reset next available cells
			for(int i = 0; i < _topBoundaryCells.Count; i++)
			{
				NextAvailableCells[_topBoundaryCells[i].x] = _topBoundaryCells[i];
				ClearAvailableCells(_topBoundaryCells[i].x);
			}
		}

		/// <summary> Clears a cell. </summary>
		/// <param name="pos"> The grid position to clear. </param>
		/// <param name="spawnExplosion"> Whether to spawn an explosion. </param>
		/// <param name="explodeFXName"> The name of the explosion fx. </param>
		public void ClearCell(Vector2 pos, bool spawnExplosion = false, string explodeFXName = "")
		{
			if(spawnExplosion)
				OnSpawnGemExplodeFX?.Invoke(explodeFXName, pos);
			PlaceGem(pos, "clear");
		}

		/// <summary> Checks collision between a flying gem and its current column bounds. </summary>
		/// <param name="gem"> The gem to check. </param>
		/// <param name="column"> The column to check with. </param>
		/// <param name="bounds"> The bounds of the gem. </param>
		public void CheckColumnCollision(Gem gem, List<Vector2> column, Bounds bounds)
		{
			// loop through column
			for (int i = 0; i < column.Count; i++)
			{
				Vector2 pos = column[i];
				// bounds intersection check
				if (!bounds.Intersects(Cells[pos].Bounds))
				{
					continue;
				}

				if (Cells[pos].GemWithin.Length == 0)
				{
					// if the cell has no north neighbor, its a top boundary
					if (!Cells[pos].northCell.valid)
					{
						Cells[pos].FillCell(gem);
						UpdateNextAvailableCell(pos.x);
						break;
					}
					continue; // continue as this cell is empty
				}

				// the cell has a gem inside
				if (Cells[pos].southCell.valid) // make sure the south cell exists
				{
					Cells[Cells[pos].southCell.position].FillCell(gem);
					UpdateNextAvailableCell(pos.x);
					break;
				}
				// reached the bottom, gg
				else
				{
					OnBottomReached();
					// game over
				}
			}
		}

		/// <summary> Prepares the list of connected cells to be exploded. </summary>
		/// <param name="chainConnected"> The ref connected chain. </param>
		private void BufferExplodeCells(ref List<Vector2> chainConnected)
		{
			// converts confirmed chain connected list to an array
			_currentChain = new Vector2[chainConnected.Count];
			chainConnected.CopyTo(_currentChain);
			chainConnected.Clear();

			// switch state to chain time
			OnSwitchState?.Invoke(GameStates.GameChainTime);
		}

		/// <summary> Settles all cells with a gem. </summary>
		public void SettleCells()
		{
			foreach (KeyValuePair<float, List<Vector2>> pair in Columns)
				foreach (Vector2 pos in Columns[pair.Key])
					if (Cells[pos].GemWithin.Length > 0)
						Cells[pos].Settle();
		}

		#endregion
	}
}

