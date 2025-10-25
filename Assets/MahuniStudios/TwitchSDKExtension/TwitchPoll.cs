// © Copyright 2025 Mahuni Game Studios

using System.Collections.Generic;
using UnityEngine;
using TwitchSDK;
using TwitchSDK.Interop;
using System.Linq;
using System.Collections;
using System;
using static Twitch;

namespace Mahuni.Twitch.Extension
{
    /// <summary>
    /// Create, stop or delete a Twitch poll. This is an example implementation to test the authentication.
    /// This guide was used while coding: https://dev.twitch.tv/docs/game-engine-plugins/unity-guide/#polls
    /// </summary>
    public static class TwitchPoll
    {
        private static GameTask<Poll> activePoll;
        private static Action<List<PollChoiceInfo>> onPollUpdated;
        private static Action<TwitchPollResult> onPollEnded;

        #region Start Poll

        /// <summary>
        /// Start a new poll
        /// </summary>
        /// <param name="pollTitle">The title of the poll</param>
        /// <param name="pollChoices">A string array of the poll choices</param>
        /// <param name="resultCallback">Listen to this callback to get notified on the poll result</param>
        /// <param name="voteUpdateCallback">Listen to this callback to get notified on vote updates</param>
        /// <param name="pollDuration">The duration of the poll in seconds. Min duration is 15 seconds</param>
        /// <returns>null</returns>
        public static IEnumerator StartPoll(string pollTitle, string[] pollChoices, Action<TwitchPollResult> resultCallback, Action<List<PollChoiceInfo>> voteUpdateCallback, int pollDuration = 15)
        {
            onPollEnded = resultCallback;
            onPollUpdated = voteUpdateCallback;

            if (!ValidatePoll()) return null;
            
            // Poll needs to be at least 15 seconds, otherwise creation will fail
            if (pollDuration < 15)
            {
                Debug.LogWarning($"Cannot create a poll with duration of {pollDuration} seconds because it is less than the required minimum. Overwrote duration to 15 seconds.");
                pollDuration = 15;
            }

            Debug.Log($"Starting a new poll.\nTitle: '{pollTitle}'\nChoices: '{string.Join("', '", pollChoices)}'\nDuration: {pollDuration} seconds");
            activePoll = API.NewPoll(new PollDefinition
            {
                Title = pollTitle,
                Choices = pollChoices,
                Duration = pollDuration, // in seconds
            });

            return MonitorPoll();
        }

        /// <summary>
        /// Validate if a new poll can be created
        /// </summary>
        /// <returns>True if a new poll can be created, false otherwise</returns>
        private static bool ValidatePoll()
        {
            if (!TwitchAuthentication.IsAuthenticated())
            {
                onPollEnded?.Invoke(new TwitchPollResult(false, "Twitch SDK is not authenticated.", null));
                Debug.LogError("Cannot create a poll when Twitch SDK is not authenticated.");
                return false;
            }

            // If we don't have the "Manage Polls" permission, we also give up.
            if (!TwitchAuthentication.ContainsScope(TwitchOAuthScope.Channel.ManagePolls.Scope))
            {
                onPollEnded?.Invoke(new TwitchPollResult(false, "Twitch SDK authentication scopes miss poll permission.", null));
                Debug.LogError($"Cannot create a poll without permissions to do so. Add '{TwitchOAuthScope.Channel.ManagePolls.Scope}' to the scopes in TwitchAuthentication script.");
                return false;
            }

            // We already polled something, so we don't do anything.
            if (activePoll != null)
            {
                Debug.LogWarning("Cannot create a poll while there is already a poll ongoing. The other poll needs to finish first.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Coroutine loop to monitor poll updates and result
        /// </summary>
        /// <returns>null</returns>
        private static IEnumerator MonitorPoll()
        {
            Poll poll;
            bool pollInitialized = false;
            long currentVotes = 0;

            while (true)
            {
                // We ran in trouble grabbing the poll, so we abort
                if (!TryGetPoll(out poll))
                {
                    DeletePoll();
                    yield break;
                }

                if (poll == null)
                {
                    // Wait for poll to be active
                }
                else if (poll.Info.Status == PollStatus.Active)
                {
                    long updatedVotes = GetTotalVotes(poll.Info.Choices.ToList());
                    if (!pollInitialized || updatedVotes > currentVotes)
                    {
                        onPollUpdated?.Invoke(poll.Info.Choices.ToList());
                        pollInitialized = true;
                    }

                    currentVotes = updatedVotes;
                }
                // Poll has ended
                else break;

                yield return null;
            }

            if (poll.Info.Status == PollStatus.Completed)
            {
                onPollEnded?.Invoke(new TwitchPollResult(poll.Info.Choices.ToList()));
            }
            else
            {
                onPollEnded?.Invoke(new TwitchPollResult(false, "Poll was cancelled with status " + poll.Info.Status, poll.Info.Choices.ToList()));
            }

            activePoll = null;
        }

        #endregion

        #region Stop / Delete Poll

        /// <summary>
        /// Stop the active poll. It will be closed and results will be shown in chat.
        /// </summary>
        public static void StopPoll()
        {
            if (activePoll == null)
            {
                Debug.LogWarning("There is no active poll that can be stopped.");
                return;
            }
            
            activePoll.MaybeResult.FinishPoll();
            onPollEnded?.Invoke(new TwitchPollResult(activePoll.MaybeResult.Info.Choices.ToList()));
            Debug.Log($"Active poll with title {activePoll.MaybeResult.Info.Title} is stopped.");
            
            activePoll = null;
        }

        /// <summary>
        /// Delete the active poll. It will be aborted and no results will be shown in chat.
        /// </summary>
        public static void DeletePoll()
        {
            if (activePoll == null)
            {
                Debug.LogWarning("There is no active poll that can be deleted.");
                return;
            }

            activePoll.MaybeResult.DeletePoll();
            onPollEnded?.Invoke(new TwitchPollResult(false, "Poll was deleted"));
            Debug.Log($"Active poll with title {activePoll.MaybeResult.Info.Title} is deleted.");

            activePoll = null;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Get the active poll
        /// </summary>
        /// <param name="poll">The active poll</param>
        /// <returns>True if getting the poll was successful. If false, poll will be null.</returns>
        private static bool TryGetPoll(out Poll poll)
        {
            poll = null;
            try
            {
                poll = activePoll?.MaybeResult;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("Something went wrong when trying to get the active poll state: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Get if the list of poll choices contains exactly one choice with the most votes.
        /// </summary>
        /// <param name="choices">The choices list to retrieve the information from</param>
        /// <param name="winner">The winner information if method returns true, else it is null</param>
        /// <returns>True if there is a winner in the choices list</returns>
        public static bool HasWinner(this List<PollChoiceInfo> choices, out PollChoiceInfo winner)
        {
            winner = null;
            
            if (choices == null || !choices.Any())
            {
                return false;
            }
            
            if (choices.GetAllWinners().Count == 1)
            {
                winner = choices.GetAllWinners().First();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get if the list of poll choices contains more than one choice with the most votes.
        /// </summary>
        /// <param name="choices">The choices list to retrieve the information from</param>
        /// <param name="winners">The winners information as list if method returns true, else it is null</param>
        /// <returns>True if there is a winner in the choices list</returns>
        public static bool IsDraw(this List<PollChoiceInfo> choices, out List<PollChoiceInfo> winners)
        {
            winners = choices.GetAllWinners();

            if (choices.HasWinner(out _))
            {
                return false;
            }

            return winners.Count > 1;
        }

        /// <summary>
        /// Get the list of poll choices containing the choices with the most votes
        /// </summary>
        /// <param name="choices">The choices list to retrieve the information from</param>
        /// <returns>The list of poll choices containing the choices with the most votes</returns>
        public static List<PollChoiceInfo> GetAllWinners(this List<PollChoiceInfo> choices)
        {
            long winningVote = choices.Max(option => option.Votes);
            return choices.Where(option => option.Votes == winningVote).ToList();
        }

        /// <summary>
        /// Get the total amount of votes from the list of poll choices
        /// </summary>
        /// <param name="choices">The choices list to retrieve the information from</param>
        /// <returns>The total amount of votes from the list of poll choices</returns>
        public static long GetTotalVotes(this List<PollChoiceInfo> choices)
        {
            if (choices == null || !choices.Any())
            {
                Debug.LogWarning("Trying to get votes from an empty list!");
                return 0;
            }

            return choices.Sum(choice => choice.Votes);
        }
        
        /// <summary>
        /// A helper struct to gather poll result information
        /// </summary>
        public struct TwitchPollResult
        {
            public readonly bool success;
            public readonly string error;
            public readonly List<PollChoiceInfo> choiceInformation;

            public TwitchPollResult(bool success, string error, List<PollChoiceInfo> choiceInformation)
            {
                this.success = success;
                this.error = error;
                this.choiceInformation = choiceInformation;
            }

            public TwitchPollResult(bool success, string error)
            {
                this.success = success;
                this.error = error;
                choiceInformation = new List<PollChoiceInfo>();
            }

            public TwitchPollResult(List<PollChoiceInfo> choiceInformation)
            {
                success = true;
                error = string.Empty;
                this.choiceInformation = choiceInformation;
            }
        }

        #endregion
    }
}