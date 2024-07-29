using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Video;

namespace Wozware.CrystalColumns
{
	public sealed partial class Game : MonoBehaviour
	{
		#region Events

		public event Action<GameStates> OnStateSwitched;

		#endregion

		#region Public Members

		public bool DebugMode = true;

		/// <summary> The primary asset library to use. </summary>
		public AssetLibrary Assets;

		[Header("Settings")]

		/// <summary> Speed multiplier for UI animation. </summary>
		[SerializeField] private float _uiAnimationSpeed = 2.0f;

		/// <summary> Speed multiplier for world animation. </summary>
		[SerializeField] private float _worldAnimationSpeed = 2.0f;

		/// <summary> The delay between chain detection then explosion </summary>
		[SerializeField] private float _chainDelay = 0.2f;

		/// <summary> The delay for columns to settle after an explosion </summary>
		[SerializeField] private float _settleDelay = 0.2f;

		/// <summary> The delay between each dropped gem </summary>
		[SerializeField] private float _dropBetweenDelay = 0.15f;

		/// <summary> The time before starting the game and each stage. </summary>
		[SerializeField] private float _pregameTime = 5.0f;

		/// <summary> The amount of time the time gain panel is shown. </summary>
		[SerializeField] private float _timeGainPanelShowTime = 3.0f;

		/// <summary> The amount of time the score gain panel is shown. </summary>
		[SerializeField] private float _scoreGainPanelShowTime = 3.0f;

		/// <summary> The amount of time to wait after winning a stage. </summary>
		[SerializeField] private float _afterWinDelay = 2.0f;

		[SerializeField] private float _attachFXLifetime = 1.0f;
		[SerializeField] private float _attachSFXDelay = 0.1f;

		[SerializeField] private float _explodeFXLifetime = 1.0f;
		[SerializeField] private float _explodeSFXDelay = 0.1f;

		[SerializeField] private float _levelXOffset = -0.5f;

		/// <summary> The current difficulty selected. </summary>
		[SerializeField] private string _currDifficulty = "easy";

		#endregion

		#region Private Members

		[Header("References")]

		/// <summary> World reference. </summary>
		[SerializeField] private World _world;

		/// <summary> Player reference. </summary>
		[SerializeField] private Player _player;

		/// <summary> The UI Animator reference. </summary>
		[SerializeField] private UIAnimator _uiAnimator;

		/// <summary> The World Animator reference. </summary>
		[SerializeField] private WorldAnimator _worldAnimator;

		/// <summary> The Camera reference </summary>
		[SerializeField] private Camera _mainCamera;

		/// <summary> The Background Tilemap reference </summary>
		[SerializeField] private Tilemap _bgTilemap;

		/// <summary> The world stage parent for loaded stages. </summary>
		[SerializeField] private Transform _stageParent;

		/// <summary> The Unity UI Event System game object. </summary>
		[SerializeField] private GameObject _eventSystem;

		/// <summary> The parent of all tilemaps in the game. </summary>
		[SerializeField] private GameObject _levelMapParent;

		/// <summary> The custom fonts. </summary>
		[SerializeField] private CustomFont _customFontBlue;
		[SerializeField] private CustomFont _customFontGreen;
		[SerializeField] private CustomFont _customFontPurple;
		[SerializeField] private CustomFont _customFontRed;
		[SerializeField] private CustomFont _customFontYellow;
		[SerializeField] private CustomFont _customFontNumbers;

		[SerializeField] private List<VideoPlayer> _tutorialVideos = new();

		[SerializeField] private Color _defaultCameraBG;
		[SerializeField] private Color _defaultTilemapBG;

		[Header("Audio")]

		[SerializeField] private Audio _audio;

		[Header("Settings")]

		[SerializeField] private Settings _settings;

		[Header("UI")]

		/// <summary> The UI shown for each state. </summary>
		[SerializeField] private List<UIState> _uiStatesList;
		[SerializeField] private UI _ui;

		/// <summary> The current UI state. </summary>
		[SerializeField] private UIStates _uiState;
		private UIStates _lastUIState;

		/// <summary> The current amount of flying gems existing. </summary>
		private int _currFlyingGems = 0;

		/// <summary> The current stage. </summary>
		private int _currStage = 0;

		/// <summary> The current sub-stage. </summary>
		private int _currSubStage = 0;

		/// <summary> The current gems needed to advance to next stage. </summary>
		private int _currGemsNeeded = 0;

		/// <summary> The current amount to drop. </summary>
		private int _currDropAmount = 0;

		/// <summary> The current amount already dropped. </summary>
		private int _dropped = 0;

		/// <summary> The current score. </summary>
		private int _currScore = 0;

		/// <summary> The current visible cursor id </summary>
		private int _cursorID = 0;

		/// <summary> The current score in the stage </summary>
		private int _currStageScore = 0;

		/// <summary> The current page in the tutorial view </summary>
		private int _currTutorialView = 0;

		/// <summary> The total current time left in seconds. </summary>
		private float _currTimeLeft;

		/// <summary> The total current time elapsed in seconds </summary>
		private float _currTimeElapsed;

		/// <summary> The stages current time elapsed in seconds </summary>
		private float _currStageTimeElapsed;

		/// <summary> The current post chain delay time. </summary>
		private float _postChainDelay = 0;

		/// <summary> The buffer time waited after chain explosion. </summary>
		private float _postChainBufferTime = 0.1f;

		/// <summary> Timer which keeps track of the next chain of stacks to check. </summary>
		private float _chainTimer = 0.0f;

		/// <summary> Timer controlling the delay after each chain check. </summary>
		private float _postChainTimer = 0.0f;

		/// <summary> Timer controlling the delay between each cell explosion. </summary>
		private float _explodeTimer = 0.0f;

		/// <summary> Timer controlling time between each gem drop in scheduled drop. </summary>
		private float _dropBetweenTimer = 0.0f;

		/// <summary> Timer controlling time between scheduled drops. </summary>
		private float _dueForDropTimer = 0.0f;

		/// <summary> Timer controlling the delay after settling cells. </summary>
		private float _settleTimer = 0.0f;

		/// <summary> Timer controlling delay in pre-game state. </summary>
		private float _preGameTimer = 0.0f;

		/// <summary> Timer controlling delay after winning stage. </summary>
		private float _afterWinTimer = 0.0f;

		/// <summary> If due for drop and should switch to drop state. </summary>
		private bool _dueForDrop = false;

		/// <summary> If all scheduled drops in the stage are finished. </summary>
		private bool _dropsFinished = false; 

		/// <summary> Did the initial drop happen? </summary>
		private bool _initialDrop = false;

		/// <summary> If the level was cleared after win. </summary>
		private bool _didClearAfterWin = false;

		/// <summary> If the win delay was finished after win. </summary>
		private bool _winDelayFinished = false;

		/// <summary> If the prestate menu was shown already. </summary>
		private bool _didShowPrestageMenu = false;

		/// <summary> If the stage music was already played. </summary>
		private bool _didPlayStageMusic = false;

		/// <summary> If the in time gain panels are visible </summary>
		private bool _isShowingTimeGainPanel = false;

		/// <summary> If the in time gain panels are visible </summary>
		private bool _isShowingScoreGainPanel = false;

		/// <summary> If any relevant cells have exploded. </summary>
		private bool _didTriggerCellExplosion = false;

		/// <summary> If any cells are currently being exploded. </summary>
		private bool _explodingCells = false;

		/// <summary> If the game is currently advancing a stage. </summary>
		private bool _advancingStage = false;

		/// <summary> If the game is ready to advance a stage. </summary>
		private bool _stageAdvanceReady = false;

		/// <summary> If the game is currently paused. </summary>
		private bool _gamePaused = false;

		/// <summary> If the game is currently in practice mode. </summary>
		private bool _isPracticeMode = false;

		/// <summary> If the game is currently in random seed mode. </summary>
		[SerializeField] private bool _randomSeedMode = false;

		/// <summary> Time controlling delay between attach sfx. </summary>
		private float _attachSFXTimer = 0f;

		/// <summary> Time controlling delay between explode sfx. </summary>
		private float _explodeSFXTimer = 0f;

		/// <summary> When running out of time, tracks seconds. </summary>
		private float _lowTimeTimer = 0f;

		/// <summary> When counting down, tracks seconds. </summary>
		private float _countDownSecTimer = 0f;

		/// <summary> Time controlling the time gain panel visibility. </summary>
		private float _showTimeGainPanelTimer = 0f;

		/// <summary> Time controlling the score gain panel visibility. </summary>
		private float _showScoreGainPanelTimer = 0f;

		/// <summary> The current game mode label name. </summary>
		private string _currGameMode = "BLITZ MODE";

		/// <summary> Timekeep controlling total elapsed time. </summary>
		private TimeKeep _elapsedTimekeep;

		/// <summary> Timekeep controlling stage elapsed time. </summary>
		private TimeKeep _totalElapsedTimekeep;

		/// <summary> The current level instance. </summary>
		private LevelInstance _currLevel;

		/// <summary> The current blitz level instance data. </summary>
		private BlitzLevelData _currBlitzLvlData;

		/// <summary> The current stage data. </summary>
		private Stage _currLvlStage;

		/// <summary> The current game state. </summary>
		[SerializeField] private GameStates _gameState;
		private GameStates _stateBeforePause;

		/// <summary> The dictionary that stores each game state update method. </summary>
		private Dictionary<GameStates, Action> _stateUpdates = new();

		/// <summary> The dictionary of UI views. </summary>
		private Dictionary<UIStates, UIState> _uiStates = new();

		#endregion
	}
}


