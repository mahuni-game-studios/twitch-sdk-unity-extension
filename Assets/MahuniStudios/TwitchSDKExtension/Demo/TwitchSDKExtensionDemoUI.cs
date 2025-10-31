// © Copyright 2025 Mahuni Game Studios

using System;
using System.Collections.Generic;
using Mahuni.Twitch.Extension;
using TMPro;
using TwitchSDK.Interop;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A class to demonstrate and test the authentication together with the Twitch SDK polls and logging out again.
/// Use it together with the TwitchSDKExtension_Demo scene.
/// </summary>
public class TwitchSDKExtensionDemoUI : MonoBehaviour
{
    [Header("Authentication")]
    public TextMeshProUGUI authenticationDescriptionText;
    public TextMeshProUGUI authenticationStatusText;
    public Button authenticateButton;
    public GameObject authenticationBlocker, authenticationErrorBanner;

    [Header("Poll")]
    public TMP_InputField pollTitle;
    public TMP_InputField pollOptionA, pollOptionB;
    public Button startPollButton, stopPollButton, deletePollButton;
    public TextMeshProUGUI pollResultText;
    public GameObject pollBlocker;

    [Header("Logout")]
    public Button logoutButton;
    public GameObject logoutBlocker;

    /// <summary>
    /// Start is called on the frame when a script is enabled just before any of the Update methods are called for the first time.
    /// </summary>
    private void Start()
    {
        // Initialize authentication
        TwitchAuthentication.OnTwitchSdkAuthenticationStatusChanged += OnTwitchSdkAuthenticationStatusChanged;
        TwitchAuthentication.OnTwitchSdkAuthenticated += OnTwitchSdkAuthenticated;
        TwitchAuthentication.OnTwitchSdkReadyForAuthentication += OnTwitchSdkReadyForAuthentication;
        authenticateButton.onClick.AddListener(OnAuthenticateButtonClicked);

        // Initialize poll
        startPollButton.onClick.AddListener(OnStartPollButtonClicked);
        stopPollButton.onClick.AddListener(OnStopPollButtonClicked);
        deletePollButton.onClick.AddListener(OnDeletePollButtonClicked);

        // Initialize logout
        logoutButton.onClick.AddListener(OnLogoutButtonClicked);

        // Start authentication check
        StartAuthenticationValidation();
    }

    #region Authentication

    /// <summary>
    /// Start a fresh authentication validation
    /// </summary>
    private void StartAuthenticationValidation()
    {
        ResetUI();
        TwitchAuthentication.StartAuthenticationValidation(this, false);
    }

    /// <summary>
    /// Callback when the authentication information is ready to be confirmed by the user.
    /// We enable the button to open the browser.
    /// </summary>
    private void OnTwitchSdkReadyForAuthentication()
    {
        authenticationDescriptionText.text = "<color=\"green\">Ready to authenticate, click on 'Authenticate' button to open URL in browser!";
        Debug.Log(authenticationDescriptionText.text, this);
        authenticateButton.interactable = true;
    }

    /// <summary>
    /// The authentication button was clicked by the user, so we open the browser
    /// </summary>
    private void OnAuthenticateButtonClicked()
    {
        authenticationDescriptionText.text = "<color=\"orange\">Confirm authentication in your browser!";
        Debug.Log(authenticationDescriptionText.text, this);
        authenticateButton.interactable = false;
        TwitchAuthentication.OpenAuthenticationURL();
    }

    /// <summary>
    /// Callback when the authentication is completed
    /// </summary>
    private void OnTwitchSdkAuthenticated()
    {
        authenticationDescriptionText.text = "<color=\"green\">Authentication finished successfully, you are ready to go :)";
        Debug.Log(authenticationDescriptionText.text, this);

        authenticateButton.interactable = false;
        authenticationBlocker.SetActive(true);

        UpdatePollsElements(false);

        logoutButton.interactable = true;
        logoutBlocker.SetActive(false);
    }

    /// <summary>
    /// Callback when the authentication state has changed in the backend
    /// </summary>
    /// <param name="authenticationStatus">The status the authentication changed into</param>
    private void OnTwitchSdkAuthenticationStatusChanged(TwitchAuthentication.AuthenticationStatus authenticationStatus)
    {
        string state = "Authentication state: ";
        switch (authenticationStatus)
        {
            case TwitchAuthentication.AuthenticationStatus.Unknown:
            case TwitchAuthentication.AuthenticationStatus.Loading:
            case TwitchAuthentication.AuthenticationStatus.Waiting:
            default:
                state += "<color=\"orange\">";
                break;
            case TwitchAuthentication.AuthenticationStatus.LoggedOut:
                state += "<color=\"red\">";
                break;
            case TwitchAuthentication.AuthenticationStatus.Authenticated:
                state += "<color=\"green\">";
                break;
            case TwitchAuthentication.AuthenticationStatus.Error:
                authenticationErrorBanner.SetActive(true);
                return;
        }
        
        authenticationStatusText.text = state + authenticationStatus;
        Debug.Log(authenticationStatusText.text, this);
    }

    #endregion

    #region Poll Test

    /// <summary>
    /// The user clicked on the start poll button
    /// </summary>
    private void OnStartPollButtonClicked()
    {
        try
        {
            StartCoroutine(TwitchPoll.StartPoll(pollTitle.text, new[] { pollOptionA.text, pollOptionB.text }, OnPollResultCallback, OnPollVoteUpdateCallback));
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not start poll coroutine! " + e);
        }

        UpdatePollsElements(true);
        pollResultText.text = "<color=\"green\">Poll is running...";
    }

    /// <summary>
    /// Callback when the active poll received an update in the vote count
    /// </summary>
    /// <param name="info">The updated poll information</param>
    private void OnPollVoteUpdateCallback(List<PollChoiceInfo> info)
    {
        long count = info.GetTotalVotes();
        if (count > 0) pollResultText.text = $"<color=\"green\">Poll updated, total votes are now {count}.";
        Debug.Log(pollResultText.text, this);
    }

    /// <summary>
    /// Callback when the poll finished with a result that now needs to be checked
    /// </summary>
    /// <param name="result">The result of the poll</param>
    private void OnPollResultCallback(TwitchPoll.TwitchPollResult result)
    {
        if (result.success)
        {
            List<PollChoiceInfo> choices = result.choiceInformation;
            
            if (choices.GetTotalVotes() == 0)
            {
                pollResultText.text = $"<color=\"orange\">Poll finished without votes.";
            }
            else if (choices.HasWinner(out PollChoiceInfo winner))
            {
                pollResultText.text = $"<color=\"green\">Poll finished, winner is '{winner.Title}'!";
            }
            else if (choices.IsDraw(out List<PollChoiceInfo> winners))
            {
                pollResultText.text = $"<color=\"green\">Poll finished with a draw. Winners are: '{string.Join("', '", winners)}'!";
            }
            else
            {
                pollResultText.text = $"<color=\"orange\">Poll finished without clear winner.";
            }

            Debug.Log(pollResultText.text, this);
        }
        else
        {
            pollResultText.text = $"<color=\"red\">Poll finished with error: '{result.error}'";
            Debug.LogWarning(pollResultText.text, this);
        }

        UpdatePollsElements(false);
    }

    /// <summary>
    /// The user clicked on the stop poll button
    /// </summary>
    private void OnStopPollButtonClicked()
    {
        TwitchPoll.StopPoll();
        UpdatePollsElements(false);
    }

    /// <summary>
    /// The user clicked on the delete poll button
    /// </summary>
    private void OnDeletePollButtonClicked()
    {
        TwitchPoll.DeletePoll();
        UpdatePollsElements(false);
    }

    /// <summary>
    /// Update the poll UI elements to be active and / or interactable or not
    /// </summary>
    /// <param name="isPollRunning">True if there is a poll currently running, false if there is no active poll</param>
    private void UpdatePollsElements(bool isPollRunning)
    {
        pollBlocker.SetActive(false);
        startPollButton.interactable = !isPollRunning;
        stopPollButton.interactable = isPollRunning;
        deletePollButton.interactable = isPollRunning;
        pollTitle.interactable = !isPollRunning;
        pollOptionA.interactable = !isPollRunning;
        pollOptionB.interactable = !isPollRunning;
    }

    #endregion

    #region Logout

    /// <summary>
    /// The user clicked on the logout button
    /// </summary>
    private void OnLogoutButtonClicked()
    {
        TwitchAuthentication.Reset();
        StartAuthenticationValidation();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Reset the UI elements 
    /// </summary>
    private void ResetUI()
    {
        authenticationDescriptionText.text = "Waiting for initialization...";
        authenticationStatusText.text = "Authentication state: Unknown";
        authenticateButton.interactable = false;
        authenticationBlocker.SetActive(false);
        authenticationErrorBanner.SetActive(false);

        startPollButton.interactable = false;
        stopPollButton.interactable = false;
        deletePollButton.interactable = false;
        pollTitle.interactable = false;
        pollOptionA.interactable = false;
        pollOptionB.interactable = false;
        pollResultText.text = "No poll running.";
        pollBlocker.SetActive(true);

        logoutButton.interactable = false;
        logoutBlocker.SetActive(true);
    }

    #endregion
}