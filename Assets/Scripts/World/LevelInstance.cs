using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tilemap = UnityEngine.Tilemaps.Tilemap;

namespace Wozware.CrystalColumns
{
	public class LevelInstance : MonoBehaviour
	{
		public Tilemap CellMap;
		public Tilemap GemMap;
		public Tilemap ShadowMap;
		public Tilemap HatchMap;
		public Tilemap GhostMap;
	}
}

