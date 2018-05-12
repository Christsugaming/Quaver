﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Quaver.API.Enums;
using Quaver.Config;
using Quaver.Helpers;
using Quaver.Input;
using Quaver.Main;
using Quaver.States.Gameplay.GameModes.Keys.Playfield;

namespace Quaver.States.Gameplay.GameModes.Keys.Input
{
    internal class KeysInputManager : IGameplayInputManager
    {
        /// <summary>
        ///     The list of button containers for these keys.
        /// </summary>
        private List<KeysInputBinding> BindingStore { get; }

        /// <summary>
        ///     Reference to the ruleset
        /// </summary>
        private GameModeKeys Ruleset { get;}

        /// <summary>
        ///     Ctor - 
        /// </summary>
        /// <param name="ruleset"></param>
        /// <param name="mode"></param>
        internal KeysInputManager(GameModeKeys ruleset, GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Keys4:
                    // Initialize 4K Input button container.
                    BindingStore = new List<KeysInputBinding>
                    {
                        new KeysInputBinding(ConfigManager.KeyMania4K1),
                        new KeysInputBinding(ConfigManager.KeyMania4K2),
                        new KeysInputBinding(ConfigManager.KeyMania4K3),
                        new KeysInputBinding(ConfigManager.KeyMania4K4)
                    };
                    break;
                case GameMode.Keys7:
                    // Initialize 7K input button container.
                    BindingStore = new List<KeysInputBinding>
                    {
                        new KeysInputBinding(ConfigManager.KeyMania7K1),
                        new KeysInputBinding(ConfigManager.KeyMania7K2),
                        new KeysInputBinding(ConfigManager.KeyMania7K3),
                        new KeysInputBinding(ConfigManager.KeyMania7K4),
                        new KeysInputBinding(ConfigManager.KeyMania7K5),
                        new KeysInputBinding(ConfigManager.KeyMania7K6),
                        new KeysInputBinding(ConfigManager.KeyMania7K7)
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }

            Ruleset = ruleset;
        }

         /// <inheritdoc />
         /// <summary>
         /// </summary>
        public void HandleInput(double dt)
        {
            for (var i = 0; i < BindingStore.Count; i++)
            {
                // Keeps track of if this key input is is important enough for us to want to 
                // update more things like animations, score, etc.
                var needsUpdating = false;
                
                // Key Pressed Uniquely
                if (InputHelper.IsUniqueKeyPress(BindingStore[i].Key.Value) && !BindingStore[i].Pressed)
                {
                    BindingStore[i].Pressed = true;
                    needsUpdating = true;
                }
                // Key Released Uniquely.
                else if (GameBase.KeyboardState.IsKeyUp(BindingStore[i].Key.Value) && BindingStore[i].Pressed)
                {
                    BindingStore[i].Pressed = false;
                    needsUpdating = true;                    
                }

                // Don't bother updating the game any further if this event isn't important.
                if (!needsUpdating)
                    continue;
                
                // Update the receptor of the playfield 
                var playfield = (KeysPlayfield) Ruleset.Playfield;             
                playfield.Stage.SetReceptorAndLightingActivity(i, BindingStore[i].Pressed);

                // Get the object manager itself.
                var manager = (KeysHitObjectManager) Ruleset.HitObjectManager;
                    
                // Find the object that is nearest in the lane that the user has pressed.
                var objectIndex = manager.GetIndexOfNearestLaneObject(i + 1, Ruleset.Screen.AudioTiming.CurrentTime);

                // Don't proceed if an object wasn't found.
                if (objectIndex == -1)
                    continue;
                
                var hitObject = (KeysHitObject) manager.ObjectPool[objectIndex];
                
                // If the key was pressed, 
                if (BindingStore[i].Pressed)
                {
                    // Play the HitSounds for this object.
                    manager.PlayObjectHitSounds(objectIndex);

                    // Check which hit window this object's timing is in
                    for (var j = 0; j < Ruleset.ScoreProcessor.JudgementWindow.Count; j++)
                    {
                        // Check if the user actually hit the object.
                        if (!(Math.Abs(hitObject.Info.StartTime - Ruleset.Screen.AudioTiming.CurrentTime) <= Ruleset.ScoreProcessor.JudgementWindow[(Judgement) j])) 
                            continue;
                        
                        var judgement = (Judgement) j;
                            
                        // Update the user's score
                        Ruleset.ScoreProcessor.CalculateScore(judgement);

                        // If the object is an LN, change the status to held.
                        if (hitObject.IsLongNote)
                            manager.ChangePoolObjectStatusToHeld(objectIndex);
                        // Otherwise, just recycle the object.
                        else
                            manager.RecyclePoolObject(objectIndex);
                        
                        break;
                    }
                }
                // If the key was released.
                else
                {                                   
                    var noteIndex = -1;
                    
                    // Get the most recent held long note in the current lane.
                    for (var j = 0; j < manager.HeldLongNotes.Count; j++)
                    {
                        if (manager.HeldLongNotes[j].Info.Lane != i + 1) 
                            continue;
                        
                        noteIndex = j;
                        break;
                    }

                    // If there is no object, then don't bother.
                    if (noteIndex == -1)
                        continue;
                    
                    // Check which window the object has 
                    var receivedJudgementIndex = -1;                   
                    for (var j = 0; j < Ruleset.ScoreProcessor.JudgementWindow.Count; j++)
                    {
                        // Get the release window of the current judgement.
                        var releaseWindow = Ruleset.ScoreProcessor.JudgementWindow[(Judgement) j] * Ruleset.ScoreProcessor.WindowReleaseMultiplier[(Judgement) j];

                        if (!(Math.Abs(manager.HeldLongNotes[noteIndex].Info.EndTime - Ruleset.Screen.AudioTiming.CurrentTime) < releaseWindow)) 
                            continue;
                        
                        receivedJudgementIndex = j;
                        break;
                    }
    
                    // If LN has been released during a window
                    if (receivedJudgementIndex != -1)
                    {
                        Ruleset.ScoreProcessor.CalculateScore((Judgement) receivedJudgementIndex);
                        manager.KillHoldPoolObject(noteIndex);
                    }
                    // If LN has been released early
                    else
                    {
                        // Count it as an okay if it was released early and kill the hold.
                        Ruleset.ScoreProcessor.CalculateScore(Judgement.Okay);
                        manager.KillHoldPoolObject(noteIndex);
                    }
                }
            }
        }
    }
}