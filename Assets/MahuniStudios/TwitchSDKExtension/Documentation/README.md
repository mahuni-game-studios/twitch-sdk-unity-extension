# Unity Twitch SDK Extension by Mahuni Game Studios

[![Downloads](https://img.shields.io/github/downloads/mahuni-game-studios/twitch-sdk-unity-extension/total.svg)](https://github.com/danqzq/unity-twitch-chat-interactions/releases/) [![Latest Version](https://img.shields.io/github/v/release/mahuni-game-studios/twitch-sdk-unity-extension)](https://github.com/danqzq/unity-twitch-chat-interactions/releases/tag/v1.31)

An extension to the Unity package available from Twitch that will allow you to easily implement Twitch API calls into your game!

## Code Snippet Examples

The simplest implementation to give your game permission to access the Twitch SDK!

### Authentication

```cs
public class YourUnityClass : MonoBehaviour
{
    private void Start()
    {
        // Register to authentication finished event
        TwitchAuthentication.OnTwitchSdkAuthenticated += OnTwitchSdkAuthenticated;
           
        // Start authentication
        TwitchAuthentication.StartAuthenticationValidation(this, false);
    }
    
    private void OnTwitchSdkAuthenticated()
    {
        // TODO: Start using the Twitch SDK API from here!
    }
}
```

### Start a Poll

```cs
public class YourUnityClass : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(TwitchPoll.StartPoll("Cats or Dogs?", new[] { "Cats!", "Dogs!" }, OnPollResultCallback, OnPollVoteUpdateCallback));
    }
      
    private void OnPollResultCallback(TwitchPoll.TwitchPollResult result)
    {
        if (result.success)
        {
            List<PollChoiceInfo> choices = result.choiceInformation;
            if (choices.GetTotalVotes() == 0)
            {
                // no votes
            }
            else if (choices.HasWinner(out PollChoiceInfo winner))
            {
                // do something with the winner!
                Debug.Log($"Winner is: '{winner.Title}'");
            }
            else if (choices.IsDraw(out List<PollChoiceInfo> winners))
            {
                // it was a draw!
                Debug.Log($"Winners are: '{string.Join("', '", winners)}'");
            }
        }
        else
        {
            // handle poll failures
            Debug.LogWarning(result.error);
        }
            
    }
}
```

## Installation Guide

### Prerequisites

To start developing with the Twitch SDK and using this extension, some things need to be set up first.

#### Twitch Client ID

To be able to interact with the Twitch SDK, you need to register your Twitch application. You can follow how to do that with this [Guide from Twitch](https://dev.twitch.tv/docs/authentication/register-app/). In short:

1. Sign up to Twitch if you don't have already
2. Navigate to the [Twitch Developer Console](https://dev.twitch.tv/console/apps)
3. Create a new application and select an appropriate category, e.g. as Game Integration
4. Click on *Manage* on your application entry and you will be presented a `Client ID`. This ID will be used by the plugin to interact with Twitch.

<font color="red">The `Client ID` should stay secret, do not share or show it!</font>

#### Twitch SDK Unity package

Twitch provides plugins for different game engines including Unity. Download the latest Twitch SDK *.unitypackage* provided by Twitch from here: [Twitch Game Engine Plugins](https://dev.twitch.tv/docs/game-engine-plugins).

*In this extension, the version from June 2024 was used.*

#### Demo scene

To use the provided `TwitchSDKExtension_Demo` scene, the `TextMeshPro` package is required. If you do not have it yet imported into your project, simply opening the `TwitchSDKExtension_Demo.scene` will ask if you want to import it. Select the `Import TMP Essentials` option, close the `TMP Importer` and you are good to go.

### Setup project
1. Either open this project or your own project in the Unity Editor
2. Drag and drop the downloaded Twitch SDK *.unitypackage* into your project. By default, it will place itself under '*/[..]/Assets/Plugins/*'. If the folder doesn't exist yet, Unity will create it for you.
   - <img src="Documentation/twitch-package-import.png" alt="Twitch Package Import Screenshot" width="300" title="Twitch Package Import Screenshot"/>
   - <font color="orange">For Windows, be sure to just check **either** *x86* **or** *x86_64* folder only and not import both, else you will get exceptions due to duplicate *R66_core.dll!*</font>
3. Find and select the `TwitchSDKSettings` asset under '*/[..]/Assets/Plugins/Resources/TwitchSDKSettings.asset*' and copy your `Client ID` into the `Twitch Client ID` input field shown in the inspector
   - <img src="Documentation/twitch-sdk-settings.png" alt="Twitch SDK Settings Asset Screenshot" width="600"/>
4. Start using the `Twitch Authentication` and / or the `TwitchPoll` scripts right away, or take a look into the `Demo` scene