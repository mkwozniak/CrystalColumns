using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Wozware.CrystalColumns
{

	#region Delegates

	public delegate void StateSwitching(GameStates state);
	public delegate void GemPlacing(Vector2 pos, string name, int uid = -1, bool withShadow = true, bool ghostTile = false);

	#endregion

	#region Constants

	public static class Constants
	{
		public static string InitialDropMessage = "INITIAL_\nDROP";
		public static string ScheduledDropMessage = "SCHEDULED_\nDROP";
	}

	#endregion

	#region Structs

	/// <summary> Holds the total data of a Blitz mode level. </summary>
	[System.Serializable]
	public struct BlitzLevelData
	{
		public string name;
		public List<Stage> Stages;
		public List<MetaGem> possibleGems;
	}

	/// <summary> Holds the data of a single level within a mode. </summary>
	[System.Serializable]
	public struct LevelStage
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

	/// <summary> Holds the scheduled drop data in a level. </summary>
	[System.Serializable]
	public struct SubStage
	{
		public float nextDropTime;
		public int dropAmountMax;
	}

	/// <summary> Holds a Gems fundamental data. </summary>
	[System.Serializable]
	public struct MetaGem
	{
		public string gemName;
		public Sprite sprite;
		public Sprite ui_sprite;
		public Sprite aim_sprite;
		public TileBase tile;
		public TileBase ghostTile;

		public MetaGem(string gemName, Sprite sprite, Sprite aimSprite, TileBase tile, TileBase ghostTile)
		{
			ui_sprite = null;
			aim_sprite = aimSprite;
			this.gemName = gemName;
			this.sprite = sprite;
			this.tile = tile;
			this.ghostTile = ghostTile;
		}
	}

	/// <summary> Holds the data of a cell neighbor. </summary>
	public struct Neighbor
	{
		public bool valid;
		public Vector2 position;
		public Neighbor(bool invalidate = true)
		{
			valid = false;
			position = Vector2.zero;
		}

		public Neighbor(Vector2 position)
		{
			valid = true;
			this.position = position;
		}
	}

	/// <summary> Data for any FX object. </summary>
	[System.Serializable]
	public struct FXContainer
	{
		public string name;
		public GameObject FX;
	}

	/// <summary> Data for particle fx and color gradients for gems. </summary>
	[System.Serializable]
	public struct GemFX
	{
		public string gemName;
		public string gemHitSFX;
		public string gemExplodeSFX;
		public GameObject gemAttachFX;
		public GameObject gemExplodeFX;
		public Gradient fxGradient_0;
		public Gradient fxGradient_1;
	}

	/// <summary> Data describing a sound. </summary>
	[System.Serializable]
	public struct SoundContainer
	{
		public string name;
		public AudioClip clip;
	}

	/// <summary> Data for an animator state associated with a game state. </summary>
	[System.Serializable]
	public struct UIState
	{
		public UIStates State;
		/// <summary> The animator ID for the state entry/exit. </summary>
		public int AnimatorID;
		public List<Transform> CanvasObjects;
	}

	/// <summary> Data for minutes, seconds, and ms. </summary>
	public struct TimeKeep
	{
		public float minutes;
		public float seconds;
		public float ms;
		public TimeKeep(float minutes, float seconds, float ms)
		{
			this.minutes = minutes;
			this.seconds = seconds;
			this.ms = ms;
		}
	}

	/// <summary> Holds the necessary data for the games user interface. </summary>
	[System.Serializable]
	public struct UI
	{
		public int BlitzPracticeStageRowMaximum;

		// UI Object References
		public GameObject DropUIPanel;
		public GameObject BlitzTimeGainPanel;
		public GameObject BlitzStageAdvancePanel;
		public GameObject BlitzPrestageMenuPanel;
		public GameObject BlitzStageClearPanel;
		public GameObject TimeGainPanel;
		public GameObject ScoreGainPanel;

		// UI Text References
		public CustomFontRenderer Lbl_DropHappening;
		public CustomFontRenderer Lbl_PregameCounter;
		public CustomFontRenderer Lbl_DropValue;
		public CustomFontRenderer Lbl_SecondsLeft;
		public CustomFontRenderer Lbl_PrestageNumber;
		public CustomFontRenderer Lbl_TimeGain;
		public CustomFontRenderer Lbl_ScoreGain;
		public CustomFontRenderer Lbl_ScoreValue;
		public CustomFontRenderer Lbl_SFXVolume;
		public CustomFontRenderer Lbl_MusicVolume;
		public CustomFontRenderer Lbl_SecTillDrop;
		public CustomFontRenderer Lbl_ElapsedMin;
		public CustomFontRenderer Lbl_ElapsedSec;
		public CustomFontRenderer Lbl_ElapsedMS;
		public CustomFontRenderer Lbl_WinElapsedMin;
		public CustomFontRenderer Lbl_WinElapsedSec;
		public CustomFontRenderer Lbl_WinElapsedMS;
		public CustomFontRenderer Lbl_WinElapsedStageMin;
		public CustomFontRenderer Lbl_WinElapsedStageSec;
		public CustomFontRenderer Lbl_WinElapsedStageMS;
		public CustomFontRenderer Lbl_WinTotalScore;
		public CustomFontRenderer Lbl_WinStageScore;
		public CustomFontRenderer Lbl_WinGame;
		public CustomFontRenderer Lbl_WinDifficulty;
		public CustomFontRenderer Lbl_WinStage;
		public CustomFontRenderer Lbl_PrestageGame;
		public CustomFontRenderer Lbl_PrestageDifficulty;
		public CustomFontRenderer Lbl_GameOverElapsedMin;
		public CustomFontRenderer Lbl_GameOverElapsedSec;
		public CustomFontRenderer Lbl_GameOverElapsedMS;
		public CustomFontRenderer Lbl_GameOverTotalScore;
		public CustomFontRenderer Lbl_GameOverGame;
		public CustomFontRenderer Lbl_GameOverDifficulty;
		public CustomFontRenderer Lbl_GameOverStage;
		public CustomFontRenderer Lbl_GemsNeeded;
		public CustomFontRenderer Lbl_PrestageSeedMode;
		public CustomFontRenderer Lbl_PracticeSelectedDifficulty;

		public Toggle Toggle_OfficialSeed;
		public Toggle Toggle_RandomSeed;

		public RectTransform BlitzPracticeMenuParent;
		public GameObject BlitzPracticeStageRowPrefab;
		public GameObject BlitzPracticeButtonPrefabParent;

		public List<GameObject> TutorialViewObjects;

		public Scrollbar SFXVolumeSlider;
		public Scrollbar MusicVolumeSlider;

		public Texture2D BaseCursor;
		public Texture2D PlayerCursor;

		public Vector2 BaseCursorHotspot;
		public Vector2 PlayerCursorHotspot;
	}

	#endregion

	#region Enums

	/// <summary> Game state enum. </summary>
	public enum GameStates
	{
		Menu,
		GamePrestage,
		GamePregame,
		Gameplay,
		GameDrop,
		GameChainTime,
		GameSettle,
		GameOver,
		GameWin,
		Paused,
	}

	public enum UIStates
	{
		Loading,
		MainMenu,
		BlitzMenu,
		Prestage,
		Pregame,
		Game,
		GameOver,
		GameWin,
		Settings,
		AudioSettings,
		VideoSettings,
		GameSettings,
		PracticeMenu,
		PauseMenu,
		TutorialMenu,
		FinalGameWin,
	}

	/// <summary> Game difficulty enum. </summary>
	public enum GameDifficulties
	{
		Easy,
		Normal,
		Hard,
		Expert,
	}

	public enum PlayerControlModes
	{
		Mouse,
		LeftRightKeys,
	}
	#endregion
}

