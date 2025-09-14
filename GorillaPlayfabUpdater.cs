using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using Photon.Pun;
using GorillaNetworking;
/// <summary>
/// 
/// GORILLA BAD NAMES
/// by sonotclose
/// 
/// </summary>
public class GorillaPlayfabUpdater : MonoBehaviour
{
    public float sync = 1f; // best for uptdaing fast

    public string[] badNames;

    private string lastName;

    private string currentName;

    private float timer;

    private protected string titleId = "URPLAYFABID";

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
        currentName = PlayerPrefs.GetString("playerName", "gorilla");
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

        PlayFabClientAPI.ExecuteCloudScript(request, async res =>
        {
            if (res.FunctionResult != null)
            {
                var json = JsonUtility.FromJson<BadNameResult>(res.FunctionResult.ToString());
                if (json != null && json.result == 2)
                {
                    PlayerPrefs.SetString("playerName", defaultName); // i forgot to do this and it took me 2 weeks to relize
                    PlayerPrefs.Save();
                    PhotonNetwork.LocalPlayer.NickName = defaultName;
                    GorillaComputer.instance.currentName = defaultName;
                    GorillaComputer.instance.savedName = defaultName;
                    GorillaComputer.instance.offlineVRRigNametagText.text = defaultName;
                    StartCoroutine(BadName());
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

    private IEnumerator BadName()
    {
        Debug.Log("BANNED BOY KICKING NOW");
        yield return new WaitForSeconds(1.2597f); // give time for the name to update
        Application.Quit();
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#endif
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

