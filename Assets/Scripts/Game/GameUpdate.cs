using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Wozware.CrystalColumns
{
	public partial class Game
	{
		/// <summary> The base Unity Update method. </summary>
		public void Update()
		{
			_stateUpdates[_gameState](); // call the states appropriate delegate update
			Update_Constant(); // call the constant update					
		}

		/// <summary> A behaviorless update state. </summary>
		private void UpdateNoBehavior() { return; }

		/// <summary> The constant update state that will be executed during all states. </summary>
		private void Update_Constant()
		{
			// game logic which runs between all gameplay states (gameplay, explosion, settle)
			UpdateGlobalGameLogic();

			_audio.Update(Time.deltaTime);
			UpdateAudioSettings();
			UpdateCursor();
		}

		private void UpdateAudioSettings()
		{
			if(_uiState != UIStates.AudioSettings)
			{
				return;
			}

			_settings.SFXVolume = _ui.SFXVolumeSlider.value;
			_settings.MusicVolume = _ui.MusicVolumeSlider.value;
			_ui.Lbl_SFXVolume.SetText(((int)(_settings.SFXVolume * 100f)).ToString());
			_ui.Lbl_MusicVolume.SetText(((int)(_settings.MusicVolume * 100f)).ToString());

			_audio.MusicSource.volume = _settings.MusicVolume;
			_audio.SetMusicVolume(_settings.MusicVolume);
			_audio.SetSFXVolume(_settings.SFXVolume);
		}

		private void UpdateCursor()
		{
			if (_uiState == UIStates.Game)
			{
				if (_cursorID != 1)
				{
					Cursor.SetCursor(_ui.PlayerCursor, _ui.PlayerCursorHotspot, CursorMode.Auto);
					_cursorID = 1;
				}

				return;
			}

			if (_cursorID != 0)
			{
				Cursor.SetCursor(_ui.BaseCursor, _ui.BaseCursorHotspot, CursorMode.Auto);
				_cursorID = 0;
			}
		}

		private void UpdateGlobalGameLogic()
		{
			if(_gameState == GameStates.Menu)
			{
				return;
			}

			_postChainTimer += Time.deltaTime;

			// when chain timer threshold reached, switch to normal state
			// but do not interrupt drop state/explosion if it is active
			if (_postChainTimer > _postChainDelay && _gameState != GameStates.GameDrop && !_explodingCells)
			{
				_postChainTimer = 0.0f;
				_chainTimer = 0.0f;
				_settleTimer = 0.0f;
				_didTriggerCellExplosion = false;
				if (_gameState == GameStates.GameChainTime || _gameState == GameStates.GameSettle)
					SwitchState(GameStates.Gameplay); // no chains found after delay, return to normal state			
			}

			if(_gameState != GameStates.Menu 
				&& _gameState != GameStates.GamePrestage 
				&& _gameState != GameStates.GamePregame 
				&& _gameState != GameStates.GameOver 
				&& _gameState != GameStates.GameWin)
			{
				_player.CheckForPause();
			}

			_attachSFXTimer += Time.deltaTime;
			_explodeSFXTimer += Time.deltaTime;

			if (_isShowingTimeGainPanel)
			{
				_showTimeGainPanelTimer += Time.deltaTime;
				if (_showTimeGainPanelTimer >= _timeGainPanelShowTime)
				{
					HideTimeGainPanel();
				}
			}

			if (_isShowingScoreGainPanel)
			{
				_showScoreGainPanelTimer += Time.deltaTime;
				if (_showScoreGainPanelTimer >= _scoreGainPanelShowTime)
				{
					HideScoreGainPanel();
				}
			}

			if (_stageAdvanceReady)
			{
				if (!_explodingCells && !_didTriggerCellExplosion && !_advancingStage)
				{
					AdvanceStage();
				}
			}
		}

		/// <summary> The ingame prestage update state. Handles the idled prestage panel before pressing start button. </summary>
		private void Update_Prestage()
		{
			if (!_didShowPrestageMenu)
			{
				Debug.Log("Showing Prestage Menu");
				_audio.SwitchMusic("between_stages");
				_ui.BlitzPrestageMenuPanel.SetActive(true);
				_ui.Lbl_PrestageNumber.SetText(_currLvlStage.LevelNumber);
				_didShowPrestageMenu = true;

				// flag bools for pregame state
				_didPlayStageMusic = false;
			}
		}

		/// <summary> The ingame pregame update state. Handles the countdown timer before gameplay. </summary>
		private void Update_Pregame()
		{
			// start stage music
			if (!_didPlayStageMusic)
			{
				// switch to the appropriate music
				_audio.SwitchMusic(Assets.BlitzLevels[_currDifficulty].Stages[_currStage].MusicID); 
				_didPlayStageMusic = true;
			}

			if (_preGameTimer >= 0)
			{
				_preGameTimer -= Time.deltaTime;
				_ui.Lbl_PregameCounter.SetText(((int)_preGameTimer).ToString());
				UpdateTimerTickSound(ref _countDownSecTimer);
				return;
			}

			SwitchState(GameStates.GameDrop); // once finished pregame, initial drop
			SwitchUIState(UIStates.Game);
			_preGameTimer = _pregameTime;
			_lowTimeTimer = 0f;
		}

		/// <summary> The ingame normal update state. Handles logic for normal gameplay. </summary>
		private void Update_Gameplay()
		{
			_player.UpdateLogic();

			if (!_player.IsAimerVisible())
			{
				if (!_world.SpriteInCell(_player.GetAimerPosition()))
					_player.SetAimerVisible(true);
				else
					_player.SetAimerVisible(false);
			}

			if (!_dropsFinished)
			{
				CheckDropDue();
			}

			_ui.Lbl_SecondsLeft.SetText(((int)(_currTimeLeft)).ToString());

			_currTimeElapsed += Time.deltaTime;
			_currStageTimeElapsed += Time.deltaTime;

			_elapsedTimekeep = GetTimeFromSeconds(_currStageTimeElapsed);

			string min_m = GetLeadingStringTime(_elapsedTimekeep.Minutes);
			string min_s = GetLeadingStringTime(_elapsedTimekeep.Seconds);
			string min_ms = GetLeadingStringTime(_elapsedTimekeep.Milliseconds);

			_ui.Lbl_ElapsedMin.SetText(min_m);
			_ui.Lbl_ElapsedSec.SetText(min_s);
			// _ui.Lbl_ElapsedMS.SetText(min_ms);

			_currTimeLeft -= Time.deltaTime;

			if(_currTimeLeft <= 7)
			{
				UpdateTimerTickSound(ref _lowTimeTimer);
			}

			if (_currTimeLeft <= 0)
			{
				TriggerGameOver();
			}
		}

		/// <summary> The ingame drop update state. Handles logic for drops in progress. </summary>
		private void Update_Drop()
		{
			if (_player.IsAimerVisible())
				_player.SetAimerVisible(false);

			_dropBetweenTimer += Time.deltaTime;

			if (_dropBetweenTimer > _dropBetweenDelay)
			{
				_world.SpawnGemDrop();
				int dropLeft = _currDropAmount - _dropped;
				if(dropLeft >= 0)
				{
					_ui.Lbl_DropValue.SetText((_currDropAmount - _dropped).ToString());
				}
			}

			if (_dropped > _currDropAmount) // drop max, end the drop
			{
				if (_initialDrop)
				{
					EndDropFinal(false);
					AddToScore(0);
					_ui.Lbl_GemsNeeded.SetText(_currGemsNeeded.ToString());
					_initialDrop = false;
				}
				else
				{
					EndDropFinal();
				}
			}
		}

		/// <summary> The ingame chain time update state. Handles logic between cell explosions and settling cells. </summary>
		private void Update_ChainTime()
		{
			if (_player.IsAimerVisible())
				_player.SetAimerVisible(false);

			if (!_didTriggerCellExplosion)
			{
				_world.LastGemAmountDestroyed = 0;
				_postChainTimer = 0f;
				_chainTimer = 0f;
				_didTriggerCellExplosion = true;
				_explodingCells = true;
			}

			// reset post chain timer during chain time
			_postChainTimer = 0.0f; 

			if (_didTriggerCellExplosion && _explodingCells)
			{
				_explodeTimer += Time.deltaTime;

				float timeGain = _currLvlStage.BaseTimeGain;
				int scoreGain = _currLvlStage.BaseScoreGain;
				float timeRatio = _currTimeLeft / _currLvlStage.InitialTime;
				float totalTimeGain = 0;
				int totalScoreGain = 0;

				if (_explodeTimer > 0.05f)
				{
					bool exploded = _world.ExplodeNextCell();
					int gemsDestroyed = _world.LastGemAmountDestroyed;
					totalTimeGain += timeGain * gemsDestroyed;
					totalScoreGain += (scoreGain * gemsDestroyed) + (int)(scoreGain * timeRatio);

					ShowTimeGainPanel(totalTimeGain);
					ShowScoreGainPanel(totalScoreGain);

					if (exploded)
					{
						_explodeTimer = 0f;
					}
					else
					{
						_world.FinishExplodingCells();
						_postChainTimer = 0f;
						_chainTimer = 0f;
						_explodingCells = false;
						AddToScore(totalScoreGain);
						AddToBlitzTime(totalTimeGain);
					}
				}

				return;
			}

			_chainTimer += Time.deltaTime;
			if (_chainTimer > _chainDelay) // time to try settling cells
			{
				_chainTimer = 0f;
				SwitchState(GameStates.GameSettle);
			}
		}

		/// <summary> The ingame settle update state. Handles logic when settling cells. </summary>
		private void Update_Settle()
		{
			if (_player.IsAimerVisible())
				_player.SetAimerVisible(false);

			// settle time
			_settleTimer += Time.deltaTime;
			if (_settleTimer > _settleDelay)
			{
				// settle cells
				_world.SettleCells();
				// call another post chain check after settling, as there may be new chains
				_settleTimer = 0.0f;
				_didTriggerCellExplosion = false;
				_world.StartCheckPostChain();
			}
		}

		/// <summary> The ingame win state. Handles logic when advancing between stages. </summary>
		private void Update_Win()
		{
			if (!_didClearAfterWin)
			{
				_audio.SwitchMusic("stage-win");

				_totalElapsedTimekeep = GetTimeFromSeconds(_currTimeElapsed);

				string min_m = GetLeadingStringTime(_totalElapsedTimekeep.Minutes);
				string min_s = GetLeadingStringTime(_totalElapsedTimekeep.Seconds);
				string min_ms = GetLeadingStringTime(_totalElapsedTimekeep.Milliseconds);

				string min_sm = GetLeadingStringTime(_elapsedTimekeep.Minutes);
				string min_ss = GetLeadingStringTime(_elapsedTimekeep.Seconds);
				string min_sms = GetLeadingStringTime(_elapsedTimekeep.Milliseconds);

				_ui.Lbl_WinElapsedMin.SetText(min_m);
				_ui.Lbl_WinElapsedSec.SetText(min_s);
				_ui.Lbl_WinElapsedMS.SetText(min_ms);

				_ui.Lbl_WinElapsedStageMin.SetText(min_sm);
				_ui.Lbl_WinElapsedStageSec.SetText(min_ss);
				_ui.Lbl_WinElapsedStageMS.SetText(min_sms);

				_ui.Lbl_WinTotalScore.SetText(GetLeadingStringScore(_currScore));
				_ui.Lbl_WinStageScore.SetText(GetLeadingStringScore(_currStageScore));

				_ui.Lbl_WinGame.SetText("BLITZ MODE");
				_ui.Lbl_WinDifficulty.SetText(_currDifficulty.ToUpper());
				string stageName = Assets.BlitzLevels[_currDifficulty].Stages[_currStage - 1].LevelNumber;
				_ui.Lbl_WinStage.SetText(stageName);

				ClearWorldAfterWin();
				return;
			}

			if(!_winDelayFinished)
			{
				_afterWinTimer += Time.deltaTime;

				if (_afterWinTimer >= _afterWinDelay)
				{
					_uiAnimator.SwitchState(UIStates.GameWin);
					_worldAnimator.SwitchState(2);
					_afterWinTimer = 0f;
					_winDelayFinished = true;
				}
			}
		}

		private void UpdateTimerTickSound(ref float timer)
		{
			timer += Time.deltaTime;
			if (timer >= 1)
			{
				_audio.SpawnSFX("time-low");
				timer = 0f;
			}
		}
	}
}

