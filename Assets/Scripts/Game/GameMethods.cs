using UnityEngine.InputSystem;
using Text = UnityEngine.UI.Text;
using Random = UnityEngine.Random;
using UnityEngine.UI;
using UnityEngine;

namespace Wozware.CrystalColumns
{
	public sealed partial class Game
	{
		#region Initialization

		/// <summary> First automatic initialization </summary>
		private void Awake()
		{
			// initialize fonts
			_customFontBlue.Initialize();
			_customFontGreen.Initialize();
			_customFontPurple.Initialize();
			_customFontRed.Initialize();
			_customFontYellow.Initialize();
			_customFontNumbers.Initialize();

			// subscribe the manager to world events
			_world.OnSwitchState += SwitchState;
			_world.OnStateSwitched += OnStateSwitched;
			_world.OnRemoveGemsNeeded += RemoveGemsNeeded;
			_world.OnAddGemExisting += AddGemExisting;
			_world.OnPlaySFX += _audio.SpawnSFX;
			_world.OnSpawnAttachFX += SpawnAttachFX;
			_world.OnSpawnGemExplodeFX += SpawnGemExplodeFX;
			_world.OnEndDropSingle += EndDropSingle;
			_world.OnBottomReached += TriggerGameOver;

			_player.PlaySFX = PlaySFX;
			_player.OnTriggeredPause = TriggerGamePause;

			// each game state has its own update delegate
			_stateUpdates[GameStates.Menu] = UpdateNoBehavior; // menu has no update behavior
			_stateUpdates[GameStates.GameOver] = UpdateNoBehavior; // game over has no update behavior
			_stateUpdates[GameStates.GamePrestage] = Update_Prestage;
			_stateUpdates[GameStates.GamePregame] = Update_Pregame;
			_stateUpdates[GameStates.Gameplay] = Update_Gameplay;
			_stateUpdates[GameStates.GameDrop] = Update_Drop;
			_stateUpdates[GameStates.GameChainTime] = Update_ChainTime;
			_stateUpdates[GameStates.GameSettle] = Update_Settle;
			_stateUpdates[GameStates.GameWin] = Update_Win;
			_stateUpdates[GameStates.Paused] = UpdateNoBehavior;

			// each game state has their own UI object in the canvas view
			foreach (UIState view in _uiStatesList)
				_uiStates[view.State] = view;

			_uiAnimator.OnStateEnter += UIStateEnter;
			_uiAnimator.OnStateExit += UIStateExit;
			_uiAnimator.OnStateIdleReady += UIStateIdleReady;
			_uiAnimator.OnSwitchState += SwitchUIState;
			_uiAnimator.OnButtonHovered += _audio.ButtonHoverSFX;
			_uiAnimator.OnStateFadeOut += UIStateFadeOut;

			_uiAnimator.SetSpeed(_uiAnimationSpeed);
			_worldAnimator.SetSpeed(_worldAnimationSpeed);

			SwitchState(GameStates.Menu);
			SwitchUIState(UIStates.MainMenu);
			Screen.SetResolution(1920, 1080, false);
		}

		/// <summary> Second automatic initialization </summary>
		private void Start()
		{
			// load assets to their external dictionaries to be accessed
			Assets.LoadAssetsToDictionaries();

			// set the initial pregame time
			_preGameTimer = _pregameTime;

			// set the main menu theme music
			_audio.SwitchMusic("theme");

			// TODO: Settings Save File
			// update settings to default
			_ui.SFXVolumeSlider.value = _settings.SFXVolume;
			_ui.MusicVolumeSlider.value = _settings.MusicVolume;
			GenerateBlitzPracticeButtons("easy");
		}

		private void InitializeWorld()
		{
			if (_currLevel == null)
				return;

			Debug.Log("Initializing World");
			_player.SetCellMap(_currLevel.CellMap);
			_mainCamera.backgroundColor = _currLvlStage.BGColor1;
			_bgTilemap.color = _currLvlStage.BGColor2;
			// assign the world tilemaps
			_world.SetWorldTilemaps(_currLevel.CellMap, _currLevel.GemMap, _currLevel.GhostMap, _currLevel.ShadowMap, _currLevel.HatchMap);
			if(!_randomSeedMode)
			{
				_world.IsOfficialSeed = true;
			}

			_world.InitializeWorld(); // initialize the world grid
		}

		#endregion

		#region Public Methods

		public void QuitApplication()
		{
			Application.Quit();
		}

		public void ToggleOfficialSeedMode()
		{
			if(_ui.Toggle_OfficialSeed.isOn)
			{
				_ui.Toggle_RandomSeed.isOn = false;
				_randomSeedMode = false;
				_world.IsOfficialSeed = true;
			}
		}

		public void ToggleRandomSeedMode()
		{
			if (_ui.Toggle_RandomSeed.isOn)
			{
				_ui.Toggle_OfficialSeed.isOn = false;
				_randomSeedMode = true;
				_world.IsOfficialSeed = false;
			}
		}

		public void SelectBlitzDifficulty(string difficulty)
		{
			_currDifficulty = difficulty;
			_ui.Lbl_PracticeSelectedDifficulty.SetText(_currDifficulty.ToUpper());
			_ui.Lbl_PrestageDifficulty.SetText(_currDifficulty.ToUpper());
		}

		/// <summary> Initiates the Blitz game mode. </summary>
		/// <param name="newGame">Whether or not to reset the current game progress.</param>
		public void StartGameBlitz(bool newGame = true)
		{
			HideTimeGainPanel();
			HideScoreGainPanel();

			if (newGame)
			{
				// new game, reset the stats
				_currStage = 0;
				_didShowPrestageMenu = false;
				SwitchState(GameStates.GamePrestage); // switch state
				UpdatePrestageLabels();
				LoadCurrentBlitzWorld(true); // create the world and reload
				return;
			}

			// destroy the level and reload if the stage needs to.
			if (Assets.BlitzLevels[_currDifficulty].Stages[_currStage].SpawnNewStage)
			{
				DestroyCurrentLevel(); // destroy the current level if there is one.
				_didShowPrestageMenu = false;
				SwitchState(GameStates.GamePrestage); // switch to loading state
				LoadCurrentBlitzWorld(true); // create the world and reload
				return;
			}

			LoadCurrentBlitzWorld();
		}

		public void StartGameBlitzPractice(int stageID)
		{
			HideTimeGainPanel();
			HideScoreGainPanel();

			// new game, reset the stats
			_isPracticeMode = true;
			_currStage = stageID;
			Debug.Log($"Starting Blitz Practice Mode at StageID: {stageID}");
			_didShowPrestageMenu = false;
			SwitchState(GameStates.GamePrestage); // switch state
			_uiAnimator.SwitchState(UIStates.Prestage);
			UpdatePrestageLabels();
			LoadCurrentBlitzWorld(true); // create the world and reload
		}

		public void QuitActiveGame()
		{
			TriggerGameOver();
		}

		public void TriggerGamePause()
		{
			if (_gamePaused)
			{
				SwitchState(_stateBeforePause);
				_uiAnimator.SwitchState(UIStates.Game);
				_gamePaused = false;
				_player.SetUnpaused();
				return;
			}

			_stateBeforePause = _gameState;
			SwitchState(GameStates.Paused);
			_uiAnimator.SwitchState(UIStates.PauseMenu);
			_gamePaused = true;
		}

		public void GameOverClean()
		{
			_audio.StopMusic(false);
			_audio.SpawnSFX("gameover");
			_audio.SwitchMusic("game-over");

			_totalElapsedTimekeep = GetTimeFromSeconds(_currTimeElapsed);

			string min_m = GetLeadingStringTime(_totalElapsedTimekeep.minutes);
			string min_s = GetLeadingStringTime(_totalElapsedTimekeep.seconds);
			string min_ms = GetLeadingStringTime(_totalElapsedTimekeep.ms);

			_ui.Lbl_GameOverElapsedMin.SetText(min_m);
			_ui.Lbl_GameOverElapsedSec.SetText(min_s);
			_ui.Lbl_GameOverElapsedMS.SetText(min_ms);

			_ui.Lbl_GameOverTotalScore.SetText(GetLeadingStringScore(_currScore));

			_ui.Lbl_GameOverGame.SetText(_currGameMode);
			_ui.Lbl_GameOverDifficulty.SetText(_currDifficulty.ToUpper());

			string stageName = Assets.BlitzLevels[_currDifficulty].Stages[_currStage].LevelNumber;
			_ui.Lbl_GameOverStage.SetText(stageName);

			_currTimeElapsed = 0;
			_currStageTimeElapsed = 0;
			_currScore = 0;
			_currStageScore = 0;

			_worldAnimator.SwitchState(2);

			_world.ClearCells();
			_world.GemPreparer.ClearQueue();
			_world.GemPreparer.ClearSwapGem();

			_player.SetAimerVisible(false);
			_player.gameObject.SetActive(false); // activate the player

			DestroyCurrentLevel();

			if (_gamePaused)
			{
				_gamePaused = false;
				_player.SetUnpaused();
			}
		}

		/// <summary> Switches to the main menu state.</summary>
		public void ReturnToMenuState()
		{
			_player.SetAimerVisible(false);
			SwitchState(GameStates.Menu);
			SwitchUIState(UIStates.MainMenu);
			// set the main menu theme music
			_audio.SwitchMusic("theme");
		}

		/// <summary>
		/// Subscribes PregameEnter to the world animator idle ready event.
		/// </summary>
		public void ListenPregameEnter()
		{
			_audio.StopMusic(false);
			_countDownSecTimer = 1f;
			_worldAnimator.OnStateIdleReady += PregameEnter;
		}

		/// <summary> Reloads the level by calling StartGameBlitz with no new game </summary>
		public void ContinueNextBlitzStage()
		{
			if (_isPracticeMode)
			{
				_uiAnimator.SwitchState(UIStates.PracticeMenu);
				SwitchState(GameStates.Menu);
				_audio.SwitchMusic("theme");
				_isPracticeMode = false;
				return;
			}

			_advancingStage = false;
			_stageAdvanceReady = false;
			_uiAnimator.SwitchState(UIStates.Prestage);
			_currStageTimeElapsed = 0;
			_currStageScore = 0;
			StartGameBlitz(false); // start the game again
		}

		/// <summary> Prepares the pre-stage state before switching. </summary>
		public void PreparePrestage()
		{
			// flag bools for prestage state
			_didShowPrestageMenu = false;
			_audio.SwitchMusic("between_stages");
			_ui.BlitzStageAdvancePanel.SetActive(true);
			_ui.BlitzStageClearPanel.SetActive(false);
		}

		public void EnterTutorialView()
		{
			_currTutorialView = 0;
			HideTutorialViewStates();
			_ui.TutorialViewObjects[_currTutorialView].SetActive(true);
			_uiAnimator.SwitchState(UIStates.TutorialMenu);
		}

		public void PreviousTutorialView()
		{
			_currTutorialView--;
			if (_currTutorialView < 0)
			{
				HideTutorialViewStates();
				_uiAnimator.SwitchState(UIStates.BlitzMenu);
				return;
			}

			HideTutorialViewStates();
			_ui.TutorialViewObjects[_currTutorialView].SetActive(true);
		}

		public void ContinueNextTutorialView()
		{
			_currTutorialView++;
			if(_currTutorialView >= _ui.TutorialViewObjects.Count)
			{
				HideTutorialViewStates();
				_uiAnimator.SwitchState(UIStates.BlitzMenu);
				return;
			}

			HideTutorialViewStates();
			_ui.TutorialViewObjects[_currTutorialView].SetActive(true);
		}

		#endregion

		#region Private Methods

		private void TriggerGameOver()
		{
			SwitchState(GameStates.GameOver);
			_uiAnimator.SwitchState(UIStates.GameOver);
			GameOverClean();
		}

		private void ShowTimeGainPanel(float timeGained)
		{
			_ui.TimeGainPanel.SetActive(true);
			_ui.Lbl_TimeGain.SetText(((int)timeGained).ToString());
			_isShowingTimeGainPanel = true;
			_showTimeGainPanelTimer = 0f;
		}

		private void HideTimeGainPanel()
		{
			_ui.TimeGainPanel.SetActive(false);
			_isShowingTimeGainPanel = false;
			_showTimeGainPanelTimer = 0f;
		}

		private void ShowScoreGainPanel(int scoreGained)
		{
			_ui.ScoreGainPanel.SetActive(true);
			_ui.Lbl_ScoreGain.SetText((scoreGained).ToString());
			_isShowingScoreGainPanel = true;
			_showScoreGainPanelTimer = 0f;
		}

		private void HideScoreGainPanel()
		{
			_isShowingScoreGainPanel = false;
			_showScoreGainPanelTimer = 0f;
			_ui.ScoreGainPanel.SetActive(false);
		}

		private void HideTutorialViewStates()
		{
			for(int i = 0; i < _ui.TutorialViewObjects.Count; i++)
			{
				_ui.TutorialViewObjects[i].SetActive(false);
			}
		}

		private void UpdatePrestageLabels()
		{
			_ui.Lbl_PrestageGame.SetText(_currGameMode);
			if(_isPracticeMode)
			{
				_ui.Lbl_PrestageDifficulty.SetText(_selectedPracticeDifficulty.ToUpper());
			}
			string randomMode = _randomSeedMode ? "RANDOM SEED" : "OFFICIAL SEED";
			_ui.Lbl_PrestageSeedMode.SetText(randomMode);
		}

		/// <summary> Loads the necessary world objects and settings for the current stage of the Blitz game mode. </summary>
		private void LoadCurrentBlitzWorld(bool reloadLevel = false)
		{
			Debug.Log($"Loading Blitz World - [reloadLevel]: {reloadLevel}");

			_currBlitzLvlData = Assets.BlitzLevels[_currDifficulty];
			_currLvlStage = _currBlitzLvlData.Stages[_currStage];

			_dueForDropTimer = _currLvlStage.SubStages[_currSubStage].nextDropTime;
			_currDropAmount = _currLvlStage.InitializeDropAmount;
			_currTimeLeft = _currLvlStage.InitialTime;
			_currGemsNeeded = _currLvlStage.InitialGemsNeeded;

			_world.CurrentPossibleGems = _currBlitzLvlData.possibleGems;
			_player.AutoShootTime = _currLvlStage.AutoShootTime;

			// post chain delay is the chain delay + the buffer time for  possible combo explosions
			_postChainDelay = _chainDelay + _postChainBufferTime;

			// load a new level if needed
			if (reloadLevel)
			{
				// load the level
				_currLevel = Instantiate(_currLvlStage.StageObject, _stageParent).GetComponent<LevelInstance>();
				_currLevel.transform.position += new Vector3(_levelXOffset, 0, 0);
			}
			else
			{
				_didShowPrestageMenu = false;
				SwitchState(GameStates.GamePrestage); // no loading was done, switch to prestage state for new stage
			}

			_currLevel.gameObject.SetActive(true); // activate the current level
			_player.gameObject.SetActive(true); // activate the player

			_world.GemSpeed = _currLvlStage.GemSpeed;

			if (!DebugMode)
			{
				_initialDrop = true;
				QueueDrop();
			}
		}

		/// <summary> Switch the game state. </summary>
		/// <param name="state"> The state to switch to. </param>
		private void SwitchState(GameStates state)
		{
			_gameState = state;

			// state was switched
			// invoke the state switched event to any listeners
			if (OnStateSwitched != null)
			{
				OnStateSwitched(state);
			}
		}

		/// <summary> Switches the UI animator state. </summary>
		/// <param name="state"></param>
		private void SwitchUIState(UIStates state)
		{
			if (!_uiStates.ContainsKey(state))
				return;

			_lastUIState = _uiState;
			_uiState = state;
			_uiAnimator.SetAnimatorState(_uiStates[state].AnimatorID);
			_eventSystem.SetActive(false);
		}

		private void UIStateEnter()
		{
			EnableUIStateObjects(true, _uiState);
		}

		private void UIStateExit()
		{
			EnableUIStateObjects(false, _lastUIState);
		}

		private void UIStateFadeOut()
		{
			Debug.Log($"UI Fade Out: {_uiState}");
			if(_uiState == UIStates.GameOver)
			{
				SetDefaultBackgroundColors();
				return;
			}
		}

		private void UIStateIdleReady()
		{
			_audio.SpawnSFX("ui-drop");
			_eventSystem.SetActive(true);
		}

		private void EnableUIStateObjects(bool val, UIStates state)
		{
			Debug.Log($"Enabling UI State: {state} : {val}");

			for (int i = 0; i < _uiStates[state].CanvasObjects.Count; i++)
			{
				_uiStates[state].CanvasObjects[i].gameObject.SetActive(val);
			}
		}

		/// <summary> Activates the pregame state when in limbo before starting a level. </summary>
		private void PregameEnter()
		{
			InitializeWorld();

			if (!_currLevel.gameObject.activeSelf)
			{
				_currLevel.gameObject.SetActive(true);
			}

			SwitchState(GameStates.GamePregame);
			_worldAnimator.FallFX.Play();
			_worldAnimator.OnStateIdleReady -= PregameEnter;
		}

		/// <summary> Returns a TimeKeep struct composed of min/sec/ms given a time in seconds. </summary>
		/// <param name="time"> The total time in seconds. </param>
		/// <returns> TimeKeep struct composed of minutes, seconds and milliseconds. </returns>
		private TimeKeep GetTimeFromSeconds(float time)
		{
			int minutes = (int)time / 60;
			int seconds = (int)time - 60 * minutes;
			int milliseconds = (int)(100 * (time - minutes * 60 - seconds));
			return new TimeKeep(minutes, seconds, milliseconds);
		}

		/// <summary>
		/// Returns an string time from a float formatted with a leading zero.
		/// </summary>
		/// <param name="time">The float time to convert.</param>
		/// <returns>A string formatted with a leading zero.</returns>
		private string GetLeadingStringTime(float time)
		{
			return string.Format("{0:00}", (int)time);
		}

		/// <summary>
		/// Returns a string score from a base integer score formatted with 6 leading zeroes.
		/// </summary>
		/// <param name="score">The base integer score.</param>
		/// <returns>A string formatted with 6 leading zeroes.</returns>
		private string GetLeadingStringScore(int score)
		{
			return string.Format("{0:000000}", score);
		}

		/// <summary> Destroys the current level if it exists. </summary>
		private void DestroyCurrentLevel()
		{
			if (_currLevel != null)
			{
				Destroy(_currLevel.gameObject);
				_currLevel = null;
			}
		}

		/// <summary> Add or subtract a gem to the existing number of flying gems. </summary>
		/// <param name="negative"> Whether to subtract instead of add. </param>
		private void AddGemExisting(bool negative)
		{
			_ = negative ? _currFlyingGems -= 1 : _currFlyingGems += 1;
		}

		/// <summary> Adds to the current blitz game mode time. </summary>
		/// <param name="t"> The amount of time to add. </param>
		private void AddToBlitzTime(float t)
		{
			_currTimeLeft += t;
		}

		/// <summary> Adds to the current score. </summary>
		/// <param name="score"> The amount of score to add. </param>
		private void AddToScore(int score)
		{
			_currScore += score;
			_currStageScore += score;
			_ui.Lbl_ScoreValue.SetText(GetLeadingStringScore(_currScore));
		}

		/// <summary> Remove from the current amount of gems needed.</summary>
		/// <param name="amount"> The amount of gems to remove. </param>
		private void RemoveGemsNeeded(int amount)
		{
			if (_stageAdvanceReady)
			{
				return;
			}

			_currGemsNeeded -= amount;
			_ui.Lbl_GemsNeeded.SetText(_currGemsNeeded.ToString());
			if (_currGemsNeeded <= 0)
			{
				_stageAdvanceReady = true;
				return;
			}

			_ui.Lbl_GemsNeeded.SetText(_currGemsNeeded.ToString());
		}

		/// <summary> Advance a stage. </summary>
		private void AdvanceStage()
		{
			_advancingStage = true;
			_stageAdvanceReady = false;

			if (_currStage + 1 < Assets.BlitzLevels[_currDifficulty].Stages.Count)
			{
				// flag bools/timer for game win state
				_didClearAfterWin = false;
				_dropsFinished = false;
				_currStage += 1;

				// deal with sfx and music then switch to game win state
				_audio.SwitchMusic("");
				_audio.SpawnSFX("stage_complete");
				SwitchState(GameStates.GameWin);
				return;
			}

			// deal with sfx and music then switch to game win state
			_audio.SpawnSFX("stage_complete");
			_audio.SwitchMusic("stage-win");
			SwitchState(GameStates.Menu);
			SwitchUIState(UIStates.FinalGameWin);
			_worldAnimator.SwitchState(2);

			ClearWorldAfterWin();
		}

		/// <summary> Advance a sub-stage. </summary>
		private void AdvanceSubStage()
		{
			if (_dropsFinished)
				return;
			else
				EndDropFinal(false);

			if (_currSubStage + 1 < _currLvlStage.SubStages.Count)
			{
				_currSubStage += 1;
				_dueForDropTimer = _currLvlStage.SubStages[_currSubStage].nextDropTime;
				_currDropAmount = _currLvlStage.SubStages[_currSubStage].dropAmountMax;
			}
			else
			{
				EndDropFinal(false);
				_currSubStage = 0;
				_dueForDropTimer = _currLvlStage.SubStages[_currSubStage].nextDropTime;
				_currDropAmount = _currLvlStage.SubStages[_currSubStage].dropAmountMax;
				// _dropsFinished = true;
			}
		}

		/// <summary> Ends a scheduled active drop completely. </summary>
		/// <param name="advance"> Whether or not to advance a stage. </param>
		private void EndDropFinal(bool advance = true)
		{
			SwitchState(GameStates.Gameplay);
			_dropBetweenTimer = 0.0f;
			_ui.DropUIPanel.SetActive(false);
			_ui.Lbl_DropHappening.gameObject.SetActive(false);
			_world.SpriteDropCover.SetActive(false);
			_dropped = 0;
			_dueForDrop = false;
			if (advance)
				AdvanceSubStage();
			else
				_currDropAmount = _currLvlStage.SubStages[_currSubStage].dropAmountMax;
		}

		/// <summary> Ends a single drop of a scheduled active drop. </summary>
		private void EndDropSingle()
		{
			_dropBetweenTimer = 0.0f;
			_dropped += 1;
		}

		/// <summary> Queues the next active drop to be activated next frame. </summary>
		private void QueueDrop()
		{
			_dropped = 0;
			_dueForDrop = true;
			_ui.DropUIPanel.SetActive(true);
			_ui.Lbl_DropHappening.gameObject.SetActive(true);
			_world.SpriteDropCover.SetActive(true);
			Debug.Log("Queuing Drop");
		}

		private void SpawnAttachFX(string gemName, Vector2 pos)
		{
			if (Assets.GemFX.ContainsKey(gemName))
			{
				ParticleSystem fx = Instantiate(Assets.GemFX[gemName].gemAttachFX, pos, Quaternion.identity).GetComponent<ParticleSystem>();
				ParticleSystem.ColorOverLifetimeModule fx_color = fx.colorOverLifetime;
				fx_color.color = Assets.GemFX[gemName].fxGradient_0;
				Destroy(fx.gameObject, _attachFXLifetime);
				if (_attachSFXTimer > _attachSFXDelay)
				{
					_audio.SpawnSFX(Assets.GemFX[gemName].gemHitSFX);
					_attachSFXTimer = 0f;
				}
			}
		}

		private void PlaySFX(string name)
		{
			_audio.SpawnSFX(name);
		}

		private void SpawnGemExplodeFX(string gemName, Vector2 pos)
		{
			if (Assets.GemFX.ContainsKey(gemName))
			{
				ParticleSystem fx = Instantiate(Assets.GemFX[gemName].gemExplodeFX, pos, Quaternion.identity).GetComponent<ParticleSystem>();
				ParticleSystem.ColorOverLifetimeModule fx_color = fx.colorOverLifetime;
				fx_color.color = Assets.GemFX[gemName].fxGradient_1;
				Destroy(fx.gameObject, _explodeFXLifetime);
				if (_explodeSFXTimer > _explodeSFXDelay)
				{
					_audio.SpawnSFX(Assets.GemFX[gemName].gemExplodeSFX);
					_explodeSFXTimer = 0f;
				}
			}
		}

		private void CheckDropDue()
		{
			if (_currFlyingGems > 0 || _explodingCells || _gameState == GameStates.GameSettle)
			{
				return;
			}

			// due for drop and drops are left
			if (_dueForDrop)
			{
				_currFlyingGems = 0;
				SwitchState(GameStates.GameDrop);
				_dueForDrop = false;
				Debug.Log("Triggering Gem Drop");
				return;
			}

			// drop timer down
			_ui.Lbl_SecTillDrop.SetText(((int)_dueForDropTimer).ToString());
			_dueForDropTimer -= Time.deltaTime;

			if (_dueForDropTimer <= 0 && !_dueForDrop)
			{
				QueueDrop();
			}
		}

		private void GenerateBlitzPracticeButtons(string difficulty)
		{
			int stageCount = Assets.BlitzLevels[difficulty].Stages.Count;

			int i = 0;
			int rowCount = _ui.BlitzPracticeMenuParent.childCount;

			while (i < rowCount)
			{
				Destroy(_ui.BlitzPracticeMenuParent.GetChild(i));
				i++;
			}

			Instantiate(_ui.BlitzPracticeStageRowPrefab, _ui.BlitzPracticeMenuParent);

			int currParentIndex = 0;
			for (i = 0; i < stageCount; i++)
			{
				string lvlName = Assets.BlitzLevels[_currDifficulty].Stages[i].LevelNumber;
				int stageID = Assets.BlitzLevels[_currDifficulty].Stages.IndexOf(Assets.BlitzLevels[_currDifficulty].Stages[i]);
				GameObject newBtn = Instantiate(_ui.BlitzPracticeButtonPrefabParent, _ui.BlitzPracticeMenuParent.GetChild(currParentIndex));
				Button btn = newBtn.GetComponent<Button>();
				ButtonVisualModifier btn_mod = newBtn.GetComponent<ButtonVisualModifier>();
				btn_mod.OnHoverEvent.AddListener(() => {
					_uiAnimator.ButtonHover();
				});

				btn.onClick.AddListener(() => {
					StartGameBlitzPractice(stageID);
					_audio.SpawnSFX("button_click0");
				});

				//Debug.Log(newBtn.transform.GetChild(1).GetComponent<CustomFontRenderer>());
				newBtn.transform.GetChild(1).GetComponent<CustomFontRenderer>().SetText(lvlName);

				if (_ui.BlitzPracticeMenuParent.GetChild(currParentIndex).childCount >= _ui.BlitzPracticeStageRowMaximum)
				{
					Instantiate(_ui.BlitzPracticeStageRowPrefab, _ui.BlitzPracticeMenuParent);
					currParentIndex++;
				}
			}
		}

		private void SetDefaultBackgroundColors()
		{
			_mainCamera.backgroundColor = _defaultCameraBG;
			_bgTilemap.color = _defaultTilemapBG;
		}

		private void ClearWorldAfterWin()
		{
			_world.ClearCells();
			_world.GemPreparer.ClearQueue();
			_world.GemPreparer.ClearSwapGem();
			_player.SetAimerVisible(false);
			_afterWinTimer = 0f;
			_winDelayFinished = false;
			_didClearAfterWin = true;
		}

		#endregion
	}
}

