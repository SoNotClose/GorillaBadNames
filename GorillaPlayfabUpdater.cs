using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using Photon.Pun;
/// <summary>
/// 
/// GORILLA BAD NAMES
/// 
/// Github for cloudscript: https://github.com/SoNotClose/GorillaBadNames
/// .gg/zenunity
/// 
/// </summary>
public class GorillaPlayfabUpdater : MonoBehaviour
{
    public float sync = 1f; // best for uptdaing fast

    public string[] badNames;

    private string lastName;

    private string currentName;

    private float timer;

    private protected string titleId = "urid"; // deleted other playfab dnt worry

    private string defaultName = "gorilla";

    private void Start()
    {
        InitializeLogin();
    }

    private void InitializeLogin()
    {
        if (string.IsNullOrEmpty(PlayFabSettings.staticSettings.TitleId))
        {
            if (!string.IsNullOrEmpty(titleId))
                PlayFabSettings.staticSettings.TitleId = titleId;
            else
                return;
        }

        if (!PlayFabClientAPI.IsClientLoggedIn())
        {
            var loginRequest = new LoginWithCustomIDRequest
            {
                CustomId = SystemInfo.deviceUniqueIdentifier,
                CreateAccount = false
            };

            PlayFabClientAPI.LoginWithCustomID(loginRequest, result =>
            {
                TrackName();
                BadNames();
            },
            error => { });
        }
        else
        {
            TrackName();
            BadNames();
        }
    }

    private void TrackName()
    {
        currentName = PhotonNetwork.LocalPlayer.NickName;
        lastName = currentName;
        CFBN(currentName);
    }

    private void BadNames()
    {
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "GetBadNames",
            GeneratePlayStreamEvent = false
        };

        PlayFabClientAPI.ExecuteCloudScript(request, res =>
        {
            if (res.FunctionResult != null)
            {
                var json = res.FunctionResult.ToString();
                BadNamesWrapper wrapper = JsonUtility.FromJson<BadNamesWrapper>(json);
                if (wrapper != null && wrapper.badNames != null)
                {
                    badNames = wrapper.badNames;
                    Debug.Log("Loaded bad names: " + string.Join(", ", badNames));
                }
                else
                {
                    //
                }
            }
        },
        err =>
        {
            Debug.LogWarning("counft get bad name :<");
        });
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= sync)
        {
            timer = 0f;
            currentName = PhotonNetwork.NickName;

            if (currentName != lastName)
            {
                CFBN(currentName);
                lastName = currentName;
            }
        }
    }

    private void CFBN(string name)
    {
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "CheckForBadName",
            FunctionParameter = new
            {
                photonName = name,
                playFabName = name,
                currentPlayerId = PlayFabClientAPI.IsClientLoggedIn() ? PlayFabSettings.staticPlayer.PlayFabId : null
            },
            GeneratePlayStreamEvent = false
        };

        PlayFabClientAPI.ExecuteCloudScript(request, res =>
        {
            if (res.FunctionResult != null)
            {
                var json = JsonUtility.FromJson<BadNameResult>(res.FunctionResult.ToString());
                if (json != null && json.result == 2)
                {
                    ForceResetDisplayName();
                    Debug.Log("BANNED BOY");
                    PhotonNetwork.LocalPlayer.NickName = defaultName;
                }
                else
                {
                    SyncDisplayName(name); // not update the playfab if name is bad
                }
            }
            else
            {
                SyncDisplayName(name);
            }
        },
        err =>
        {
            SyncDisplayName(name);
        });
    }

    private void SyncDisplayName(string name)
    {
        var updateRequest = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = name
        };

        PlayFabClientAPI.UpdateUserTitleDisplayName(updateRequest, result =>
        {
            Debug.Log($"Updated PlayFab display name to: {name}");
        },
        error => { });
    }

    private void ForceResetDisplayName() // i kept forgettng to name this force reset so i struggle for an hour wondering why i kept gettng banned
    {
        var resetRequest = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = defaultName
        };

        PlayFabClientAPI.UpdateUserTitleDisplayName(resetRequest, result =>
        {
            PhotonNetwork.LocalPlayer.NickName = defaultName;
            Application.Quit();
            UnityEditor.EditorApplication.isPlaying = false;
        },
        error => { });
    }

    [System.Serializable]
    private class BadNameResult
    {
        public int result;
    }

    [System.Serializable]
    private class BadNamesWrapper
    {
        public string[] badNames;
    }
}
