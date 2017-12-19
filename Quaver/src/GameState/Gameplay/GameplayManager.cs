﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Quaver.GameState.States;
using Quaver.Logging;
using Quaver.Audio;
using Quaver.GameState.Gameplay.PlayScreen;
using Quaver.Graphics.Sprite;
using Quaver.Graphics.Text;
using Quaver.Graphics.Button;
using Quaver.Graphics;
using Quaver.Input;
using Quaver.Replays;
using Quaver.Config;
using Quaver.QuaFile;

namespace Quaver.GameState.Gameplay
{
    /// <summary>
    /// This class handles the interaction between note and input.
    /// </summary>
    class GameplayManager : IHelper
    {
        private AccuracyBoxUI AccuracyBoxUI { get; set; }

        private NoteManager NoteManager { get; set; }

        private Playfield Playfield { get; set; }

        private Timing Timing { get; set; }

        private ScoreManager ScoreManager { get; set; }

        private PlayfieldUI PlayfieldUI { get; set; }

        /// <summary>
        ///     The MD5 Hash of the played beatmap.
        /// </summary>
        private string BeatmapMd5 { get; set; }

        /// <summary>
        ///     The current Qua file that's being red
        /// </summary>
        private Qua CurrentQua { get; set; }

        /// <summary>
        ///     The input manager for this game state.
        /// </summary>
        private GameplayInputManager InputManager { get; set; }

        /// <summary>
        ///     Holds the list of replay frames for this state.
        /// </summary>
        private List<ReplayFrame> ReplayFrames { get; set; }

        /// <summary>
        ///     Keeps track of whether or not the song intro is current skippable.
        /// </summary>
        private bool IntroSkippable { get; set; }

        //todo: remove. TEST.
        private Sprite TextUnder { get; set; }
        private TextBoxSprite SVText { get; set; }
        private Button TestButton { get; set; }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="qua"></param>
        public GameplayManager(Qua qua, string beatmapMd5)
        {
            // Pass Parameters
            BeatmapMd5 = beatmapMd5;
            CurrentQua = qua;

            // Create Class Components
            AccuracyBoxUI = new AccuracyBoxUI();
            NoteManager = new NoteManager(qua);
            Playfield = new Playfield();
            PlayfieldUI = new PlayfieldUI();
            Timing = new Timing(qua);
            ScoreManager = new ScoreManager();
            InputManager = new GameplayInputManager();
            ReplayFrames = new List<ReplayFrame>();

            // Initialize Gameplay
            InitializeGameplay(null, qua);

            // Hook InputManager
            InputManager.ManiaKeyPress += ManiaKeyDown;
            InputManager.ManiaKeyRelease += ManiaKeyUp;

            // Hook Missed Note Events
            NoteManager.PressMissed += PressMissed;
            NoteManager.ReleaseSkipped += ReleaseSkipped;
            NoteManager.ReleaseMissed += ReleaseMissed;
        }

        public void Initialize(IGameState playScreen)
        {
            //Todo: Remove. TEST.
            TestButton = new TextButton(new Vector2(200, 30), "BACK")
            {
                Image = GameBase.LoadedSkin.ColumnTimingBar,
                Alignment = Alignment.TopCenter
            };
            TestButton.Clicked += BackButtonClick;

            TextUnder = new Sprite()
            {
                Image = GameBase.UI.HollowBox,
                Tint = Color.Blue,
                Size = new Vector2(250, 200),
                Alignment = Alignment.TopRight
            };
        }

        public void UnloadContent()
        {
            //NoteManager.UnloadContent();
            Timing.UnloadContent();
            Playfield.UnloadContent();
            PlayfieldUI.UnloadContent();
            AccuracyBoxUI.UnloadContent();
            NoteManager.UnloadContent();

            //todo: remove this later
            TestButton.Clicked -= BackButtonClick;
            TextUnder.Destroy();
        }

        public void Update(double dt)
        {
            TestButton.Update(dt);
            TextUnder.Update(dt);
            
            // Set the current song time.
            Timing.Update(dt);
            GameplayReferences.CurrentSongTime = Timing.CurrentSongTime;

            // Check if the song is currently skippable.
            IntroSkippable = (GameBase.SelectedBeatmap.Qua.HitObjects[0].StartTime - Timing.CurrentSongTime >= 5000);

            // Update Helper Classes
            Playfield.Update(dt);
            NoteManager.Update(dt);
            AccuracyBoxUI.Update(dt);
            PlayfieldUI.Update(dt);

            // Check the input for this particular game state.
            InputManager.CheckInput(IntroSkippable, ReplayFrames);

            // Update Loggers. todo: remove
            Logger.Update("KeyCount", $"Key Count: {GameBase.SelectedBeatmap.Qua.KeyCount}");
            Logger.Update("SongPos", "Current Track Position: " + NoteManager.TrackPosition);
            Logger.Update("Skippable", $"Intro Skippable: {IntroSkippable}");

            //Todo: remove. TEST.
            TextUnder.Update(dt);

            if (Timing.PlayingIsDone)
                GameBase.GameStateManager.ChangeState(new ScoreScreenState(BeatmapMd5, ScoreManager, GameBase.SelectedBeatmap.Artist, GameBase.SelectedBeatmap.Title, GameBase.SelectedBeatmap.DifficultyName, ReplayFrames));  
        }

        public void Draw()
        {
            TestButton.Draw();
            TextUnder.Draw();
            Playfield.Draw();
            NoteManager.Draw();
            PlayfieldUI.Draw();
            AccuracyBoxUI.Draw();
            TestButton.Draw();
        }

        /// <summary>
        ///     Solely responsible for intializing gameplay aspects
        /// </summary>
        private void InitializeGameplay(IGameState state, Qua qua)
        {
            //Initialize Score Manager
            //todo: temp
            
            var count = 0;
            var total = GameBase.SelectedBeatmap.Qua.HitObjects.Count;

            foreach (var ho in GameBase.SelectedBeatmap.Qua.HitObjects)
            {
                if (ho.EndTime > ho.StartTime) count++;
            }

            ScoreManager = new ScoreManager();
            ScoreManager.Initialize(total + count, GameBase.SelectedBeatmap.Qua.Judge); //TODO: ADD RELEASE COUNTS AS WELL

            //Initialize class components
            Playfield.Initialize(state);
            Timing.Initialize(state);
            NoteManager.Initialize(state);
            AccuracyBoxUI.Initialize(state);
            PlayfieldUI.Initialize(state);

            //todo: remove this. used for logging.
            // Create loggers
            Logger.Add("KeyCount", "", Color.Pink);
            Logger.Add("SongPos", "", Color.White);
            Logger.Add("Skippable", "", CustomColors.NameTagAdmin);
            Logger.Add("JudgeDifficulty", "", CustomColors.NameTagModerator);

            // Update hit window logger
            var loggertext = "Hitwindow: Judge: " + ScoreManager.JudgeDifficulty + "   Press: ";
            foreach (var a in ScoreManager.HitWindowPress) loggertext += Math.Floor(a) + "ms, ";
            loggertext += "   Release: ";
            foreach (var a in ScoreManager.HitWindowRelease) loggertext += Math.Floor(a) + "ms, ";

            // Logger.Update("JudgeDifficulty", loggertext);
        }

        /// <summary>
        ///     Temporary method for back button click handling todo: remove
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void BackButtonClick(object sender, EventArgs e)
        {
            GameBase.GameStateManager.ChangeState(new SongSelectState());
        }

        /// <summary>
        ///     Everytime a mania key gets pressed, this method will look for the closest note and judge it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="keyLane"></param>
        public void ManiaKeyDown(object sender, ManiaKey keyLane)
        {
            // Play Audio
            GameBase.LoadedSkin.Hit.Play((float)Configuration.VolumeGlobal / 100 * Configuration.VolumeEffect / 100, 0, 0);

            //Check for Note press/LN press
            //Reference Variables
            int noteIndex = -1;
            int i;

            // Update Receptor in Playfield
            Playfield.UpdateReceptor(keyLane.GetKey(), true);

            //Search for closest HitObject that is inside the HitTiming Window
            for (i = 0; i < NoteManager.HitObjectPoolSize && i < NoteManager.HitObjectPool.Count; i++)
            {
                if (NoteManager.HitObjectPool[i].KeyLane == keyLane.GetKey() + 1 && NoteManager.HitObjectPool[i].StartTime - Timing.CurrentSongTime > -ScoreManager.HitWindowPress[4])
                {
                    noteIndex = i;
                    break;
                }
            }

            //If such HitObject exists, it will do key-press stuff to it
            if (noteIndex > -1)
            {
                //Check which HitWindow this object's timing is in
                for (i = 0; i < 5; i++)
                {
                    if (Math.Abs(NoteManager.HitObjectPool[noteIndex].StartTime - Timing.CurrentSongTime) <= ScoreManager.HitWindowPress[i])
                    {
                        //Score manager stuff
                        ScoreManager.Count(i, false, NoteManager.HitObjectPool[noteIndex].StartTime - Timing.CurrentSongTime, Timing.CurrentSongTime / SongManager.Length);
                        AccuracyBoxUI.UpdateAccuracyBox(i, ScoreManager.JudgePressSpread[i], ScoreManager.JudgeReleaseSpread[i], ScoreManager.JudgeCount);
                        PlayfieldUI.UpdateJudge(i, false, NoteManager.HitObjectPool[noteIndex].StartTime - Timing.CurrentSongTime);

                        // If the player is spamming
                        if (i >= 3)
                            NoteManager.KillNote(noteIndex);
                        else
                        {
                            //If the object is an LN, hold it at the receptors
                            if (NoteManager.HitObjectPool[noteIndex].IsLongNote) NoteManager.HoldNote(noteIndex);

                            //If the object is not an LN, recycle it.
                            else NoteManager.RecycleNote(noteIndex);
                        }

                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     Everytime a mania key gets released, this method will look for the closest note and judge it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="keyLane"></param>
        public void ManiaKeyUp(object sender, ManiaKey keyLane)
        {
            //Reference Variables
            int noteIndex = -1;
            int i;

            // Update Receptor in Playfield
            Playfield.UpdateReceptor(keyLane.GetKey(), false);

            //Search for closest HitObject that is inside the HitTiming Window
            for (i = 0; i < NoteManager.HitObjectHold.Count; i++)
            {
                if (NoteManager.HitObjectHold[i].KeyLane == keyLane.GetKey() + 1)
                {
                    noteIndex = i;
                    break;
                }
            }

            //If such HitObject exists, it will do key-press stuff to it
            if (noteIndex > -1)
            {
                //Check which HitWindow this object's timing is in.
                //Since it's an LN, the hit window is increased by 1.25x.
                //Only checks MARV/PERF/GREAT/GOOD
                int releaseTiming = -1;
                for (i = 0; i < 4; i++)
                {
                    if (Math.Abs(NoteManager.HitObjectHold[noteIndex].EndTime - Timing.CurrentSongTime) <= ScoreManager.HitWindowRelease[i])
                    {
                        releaseTiming = i;
                        break;
                    }
                }

                //If LN has been released during a HitWindow
                if (releaseTiming > -1)
                {
                    ScoreManager.Count(i, true);
                    AccuracyBoxUI.UpdateAccuracyBox(i, ScoreManager.JudgePressSpread[i], ScoreManager.JudgeReleaseSpread[i], ScoreManager.JudgeCount);
                    PlayfieldUI.UpdateJudge(i, true);
                    NoteManager.KillHold(noteIndex, true);
                }
                //If LN has been pressed early
                else
                {
                    ScoreManager.Count(5, true);
                    AccuracyBoxUI.UpdateAccuracyBox(5, ScoreManager.JudgePressSpread[i], ScoreManager.JudgeReleaseSpread[i], ScoreManager.JudgeCount);
                    PlayfieldUI.UpdateJudge(5, true);
                    NoteManager.KillHold(noteIndex);
                }
            }
        }

        public void PressMissed(object sender, EventArgs e)
        {
            ScoreManager.Count(5, false, 0, GameplayReferences.CurrentSongTime/ SongManager.Length);
            AccuracyBoxUI.UpdateAccuracyBox(5, ScoreManager.JudgePressSpread[5], ScoreManager.JudgeReleaseSpread[5], ScoreManager.JudgeCount);
            PlayfieldUI.UpdateJudge(5);
        }

        public void ReleaseSkipped(object sender, EventArgs e)
        {
            ScoreManager.Count(5, true);
            AccuracyBoxUI.UpdateAccuracyBox(5, ScoreManager.JudgePressSpread[5], ScoreManager.JudgeReleaseSpread[5], ScoreManager.JudgeCount);
            PlayfieldUI.UpdateJudge(5);
        }

        public void ReleaseMissed(object sender, EventArgs e)
        {
            ScoreManager.Count(4,true);
            AccuracyBoxUI.UpdateAccuracyBox(4, ScoreManager.JudgePressSpread[4], ScoreManager.JudgeReleaseSpread[4], ScoreManager.JudgeCount);
            PlayfieldUI.UpdateJudge(4);
        }
    }
}