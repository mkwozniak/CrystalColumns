using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wozware.CrystalColumns
{
	[CreateAssetMenu(fileName = "Stage", menuName = "ScriptableObjects/Stage", order = 2)]

	public class Stage : ScriptableObject
	{
		public string LevelNumber;
		public string LevelName;
		public bool SpawnNewStage;
		public GameObject StageObject;
		public int InitializeDropAmount;
		public int InitialGemsNeeded;
		public float InitialTime;
		public float BaseTimeGain;
		public int BaseScoreGain;
		public List<SubStage> SubStages;
		public string MusicID;
		public Color BGColor1;
		public Color BGColor2;
		public float GemSpeed;
		public float AutoShootTime;
	}
}

