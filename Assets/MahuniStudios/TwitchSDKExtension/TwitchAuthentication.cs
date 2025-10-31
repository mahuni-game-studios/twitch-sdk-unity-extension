// © Copyright 2025 Mahuni Game Studios

using System;
using System.Collections;
using System.Linq;
using TwitchSDK;
using TwitchSDK.Interop;
using UnityEngine;
using static Twitch;

namespace Mahuni.Twitch.Extension
{
    /// <summary>
    /// Authenticate your application to Twitch and give it permission to interact with the Twitch SDK
    /// </summary>
    public static class TwitchAuthentication
    {
        public static event Action<AuthenticationStatus> OnTwitchSdkAuthenticationStatusChanged;
        public static event Action OnTwitchSdkAuthenticated;
        public static event Action OnTwitchSdkReadyForAuthentication;
        private static AuthenticationStatus authenticationStatus;
        private static GameTask<AuthenticationInfo> authInfoTask;
        private static string authenticationUrl;
        
        public enum AuthenticationStatus
        {
            Unknown,
            Error,
            Loading,
            Waiting,
            LoggedOut,
            Authenticated
        }
        
        /// <summary>
        /// Get if the application is authenticated and Twitch SDK methods can thereby be used
        /// </summary>
        /// <returns>True if the application is authenticated, false if authentication is not completed</returns>
        public static bool IsAuthenticated()
        {
            return authenticationStatus == AuthenticationStatus.Authenticated;
        }
        
        /// <summary>
        /// Reset the authentication status by logging out
        /// </summary>
        public static void Reset()
        {
            API.LogOut();
            authenticationStatus = AuthenticationStatus.LoggedOut;
        }

        /// <summary>
        /// Start the validation process to retrieve authentication information
        /// </summary>
        /// <param name="monoBehaviour">The MonoBehaviour to attach the coroutine onto</param>
        /// <param name="autoOpenBrowser">True to automatically open the URL in the browser when ready.
        /// False to wait for an external trigger to call <see cref="OpenAuthenticationURL"/> method.</param>
        public static void StartAuthenticationValidation(MonoBehaviour monoBehaviour, bool autoOpenBrowser)
        {
            monoBehaviour.StartCoroutine(UpdateAuthenticationStatus(autoOpenBrowser));
        }

        /// <summary>
        /// Update the authentication state and wait for the user to confirm in the browser
        /// </summary>
        /// <param name="autoOpenBrowser">True to open the browser as soon as the information is ready.
        /// False to wait for an external trigger to call <see cref="OpenAuthenticationURL"/> method.</param>
        /// <returns>null</returns>
        private static IEnumerator UpdateAuthenticationStatus(bool autoOpenBrowser)
        {
            AuthenticationInfo userAuthInfo = null;
            authenticationUrl = string.Empty;
            authenticationStatus = AuthenticationStatus.Unknown;

            while (userAuthInfo == null)
            {
                // If we detect an exception, it can mean that the Client ID is not set correctly, so we stop right here
                if (API.GetAuthenticationInfo(GetScopes()) != null && API.GetAuthenticationInfo(GetScopes()).Exception != null)
                {
                    authenticationStatus = AuthenticationStatus.Error;
                    OnTwitchSdkAuthenticationStatusChanged?.Invoke(authenticationStatus);
                    Debug.LogWarning($"Exception caught: {API.GetAuthenticationInfo(GetScopes()).Exception}");
                    yield break;
                }
                
                UpdateAuthenticationStatus();

                // If we are already authenticated, we can stop right here
                if (authenticationStatus == AuthenticationStatus.Authenticated)
                {
                    OnTwitchSdkAuthenticated?.Invoke();
                    yield break;
                }

                if (authenticationStatus == AuthenticationStatus.LoggedOut)
                {
                    authInfoTask ??= API.GetAuthenticationInfo(GetScopes());
                }
                else if (authenticationStatus == AuthenticationStatus.Waiting)
                {
                    userAuthInfo = API.GetAuthenticationInfo(GetScopes()).MaybeResult;
                }
                
                yield return null;
            }

            // We have reached the state where we can ask the user to authenticate in the browser
            authenticationUrl = userAuthInfo.Uri;
            if (autoOpenBrowser) OpenAuthenticationURL();
            OnTwitchSdkReadyForAuthentication?.Invoke();

            // Wait for authentication to be confirmed
            while (authenticationStatus != AuthenticationStatus.Authenticated)
            {
                UpdateAuthenticationStatus();
                yield return null;
            }

            OnTwitchSdkAuthenticated?.Invoke();
        }

        /// <summary>
        /// Open the URL in your default browser to complete authentication
        /// </summary>
        public static void OpenAuthenticationURL()
        {
            if (string.IsNullOrEmpty(authenticationUrl))
            {
                Debug.LogWarning("Trying to open a browser URL which is empty!");
                return;
            }

            Application.OpenURL(authenticationUrl);
        }
        
        /// <summary>
        /// Get if the passed scope string is contained in the current scopes
        /// </summary>
        /// <param name="scope">The scope string to look for in the current scopes</param>
        /// <returns>True if the passed scope string is contained in the current scopes</returns>
        public static bool ContainsScope(string scope)
        {
            return API.GetAuthState().MaybeResult.Scopes.Contains(scope);
        }

        /// <summary>
        /// Get the scopes you want your application to get permission for
        /// </summary>
        /// <returns>An array of scopes to get permission for</returns>
        private static TwitchOAuthScope[] GetScopes()
        {
            TwitchOAuthScope[] scopes =
            {
                // This method uses all provided scopes contained in the TwitchSDK plugin, you should only request the scopes you actively need!
                TwitchOAuthScope.Bits.Read,
                TwitchOAuthScope.Channel.ManageBroadcast,
                TwitchOAuthScope.Channel.ManagePolls,
                TwitchOAuthScope.Channel.ManagePredictions,
                TwitchOAuthScope.Channel.ManageRedemptions,
                TwitchOAuthScope.Channel.ReadHypeTrain,
                TwitchOAuthScope.Clips.Edit,
                TwitchOAuthScope.User.ReadSubscriptions,

                // There are more scopes available, find more examples here: https://dev.twitch.tv/docs/authentication/scopes/
                new("moderator:read:followers"),
                new("channel:read:subscriptions"),
                new("user:read:chat")
            };

            return scopes;
        }
        
        /// <summary>
        /// Update the authentication state and invoke the <see cref="OnTwitchSdkAuthenticationStatusChanged"/> event if it has changed 
        /// </summary>
        private static void UpdateAuthenticationStatus()
        {
            AuthStatus newState = API.GetAuthState().MaybeResult.Status;
            AuthenticationStatus translatedStatus;
            switch (newState)
            {
                case AuthStatus.LoggedOut:
                    translatedStatus = AuthenticationStatus.LoggedOut;
                    break;
                case AuthStatus.Loading:
                    translatedStatus = AuthenticationStatus.Loading;
                    break;
                case AuthStatus.WaitingForCode:
                    translatedStatus = AuthenticationStatus.Waiting;
                    break;
                case AuthStatus.LoggedIn:
                    translatedStatus = AuthenticationStatus.Authenticated;
                    break;
                default:
                    translatedStatus = AuthenticationStatus.Unknown;
                    break;
            }
            
            bool hasStateChanged = translatedStatus != authenticationStatus;
            authenticationStatus = translatedStatus;
            if (hasStateChanged) OnTwitchSdkAuthenticationStatusChanged?.Invoke(authenticationStatus);
        }
    }
}