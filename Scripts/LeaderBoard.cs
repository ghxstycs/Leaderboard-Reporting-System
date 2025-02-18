using UnityEngine;
using UnityEngine.Networking;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Photon.Voice.PUN;
using Photon.VR.Player;
using System.Text;
using System.Collections;
using PlayFab;
using PlayFab.ClientModels;
using MonkeyJumpersButtons; // Remove unless you're using my report button.

[RequireComponent(typeof(PhotonView))]
public class LeaderBoard : MonoBehaviour
{
    [Header("Fully re-scripted by ghxsty.")]
    [SerializeField] public TMP_Text[] displaySpot;
    [SerializeField] public Renderer[] ColorSpot;
    [SerializeField] public string WebHookURL;
    [SerializeField] public string BanWebHookURL;
    [SerializeField] public string RequiredItemID;
    [SerializeField] public Playfablogin playfablogin;
    [SerializeField] public string BanDelayString;
    private bool hashed;
    private bool Kicked = false;
    private bool BanReported = false;

    private void Start()
    {
        if (GetComponent<PhotonView>().OwnershipTransfer != OwnershipOption.Takeover)
        {
            GetComponent<PhotonView>().OwnershipTransfer = OwnershipOption.Takeover;
        }
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        RefreshLeaderBoard();
    }

    private void RefreshLeaderBoard()
    {
        for (int i = 0; i < displaySpot.Length; i++)
        {
            displaySpot[i].text = "";
            ColorSpot[i].material.color = Color.white;
        }

        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            displaySpot[i].text = PhotonNetwork.PlayerList[i].NickName;

            foreach (PhotonVRPlayer PVRP in FindObjectsOfType<PhotonVRPlayer>())
            {
                if (PVRP.gameObject.GetComponent<PhotonView>().Owner == PhotonNetwork.PlayerList[i])
                {
                    ColorSpot[i].material.color = JsonUtility.FromJson<Color>(
                        (string)PVRP.gameObject.GetComponent<PhotonView>().Owner.CustomProperties["Colour"]);
                }
            }
        }
    }

    private void Update()
    {
        if (Kicked || BanReported)
        {
            string message = Kicked ? "You have been kicked from the server." : "You have been banned, a staff member reported you.";

            displaySpot[0].color = Color.red;
            displaySpot[0].text = message;

            PhotonNetwork.Disconnect();
            return; 
        }

        RefreshLeaderBoard();
    }

    public void MutePress(int ButtonNumber)
    {
        if (PhotonNetwork.PlayerList.Length >= ButtonNumber - 1)
        {
            Photon.Realtime.Player targetPlayer = PhotonNetwork.PlayerList[ButtonNumber - 1];

            if (targetPlayer != PhotonNetwork.LocalPlayer)
            {
                foreach (PhotonVRPlayer PVRP in FindObjectsOfType<PhotonVRPlayer>())
                {
                    if (PVRP.gameObject.GetComponent<PhotonView>().Owner == targetPlayer)
                    {
                        AudioSource audioSource = PVRP.gameObject.GetComponent<PhotonVoiceView>().SpeakerInUse.gameObject.GetComponent<AudioSource>();
                        audioSource.mute = !audioSource.mute;
                        break;
                    }
                }
            }
        }
    }

    public void KickPress(int ButtonNumber)
    {
        if (PhotonNetwork.PlayerList.Length >= ButtonNumber - 1)
        {
            Photon.Realtime.Player targetPlayer = PhotonNetwork.PlayerList[ButtonNumber - 1];

            if (targetPlayer != PhotonNetwork.LocalPlayer)
            {
                foreach (PhotonVRPlayer PVRP in FindObjectsOfType<PhotonVRPlayer>())
                {
                    if (PVRP.gameObject.GetComponent<PhotonView>().Owner == targetPlayer)
                    {
                        GetComponent<PhotonView>().RequestOwnership();
                        GetComponent<PhotonView>().RPC("KickPlayer", PVRP.gameObject.GetComponent<PhotonView>().Owner);
                    }
                }
            }
        }
    }

    [PunRPC]
    void KickPlayer()
    {
        Kicked = true;
    }

    [PunRPC]
    void BanReportRPC()
    {
        BanReported = true;
    }

    public void Report(int ButtonNumber, string reportReason)
    {
        if (PhotonNetwork.PlayerList.Length >= ButtonNumber - 1)
        {
            Photon.Realtime.Player targetPlayer = PhotonNetwork.PlayerList[ButtonNumber - 1];
            if (targetPlayer != PhotonNetwork.LocalPlayer)
            {
                CheckForBanPermission(targetPlayer, reportReason); // pass dis bish to the ban check fr
            }
            else
            {
                Debug.Log("You cannot report yourself.");
            }
        }
    }

    private void CheckForBanPermission(Photon.Realtime.Player targetPlayer, string reportReason)
    {
        string targetPlayfabID = targetPlayer.CustomProperties.ContainsKey("PlayfabID") 
            ? (string)targetPlayer.CustomProperties["PlayfabID"] 
            : "Unknown ID";

        Debug.Log($"Reporting Player: {PhotonNetwork.LocalPlayer.NickName}, Target Player: {targetPlayer.NickName}, Target PlayFab ID: {targetPlayfabID}, Reason: {reportReason}");

        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), result =>
        {
            bool hasBanItem = result.Inventory.Exists(item => item.ItemId == RequiredItemID);
            
            if (hasBanItem)
            {
                StartCoroutine(BanAfterDelay(targetPlayer, targetPlayfabID, reportReason));
            }
            else
            {
                SendToWebhook($"{targetPlayer.NickName} (PlayFab ID: {targetPlayfabID}) was reported by {PhotonNetwork.LocalPlayer.NickName} (PlayFab ID: {playfablogin.MyPlayFabID}) for {reportReason}.", WebHookURL);
            }
        },
        error => Debug.LogError("Error checking inventory: " + error.ErrorMessage));
    }

    private IEnumerator BanAfterDelay(Photon.Realtime.Player targetPlayer, string targetPlayfabID, string reportReason)
    {
        if (!float.TryParse(BanDelayString, out float banDelay))
        {
            Debug.LogError("Invalid BanDelayString value. Using default 1 second.");
            banDelay = 0.1f; 
        }

        Debug.Log($"Waiting {banDelay} seconds before banning {targetPlayer.NickName}");
        yield return new WaitForSeconds(banDelay);

        foreach (PhotonVRPlayer PVRP in FindObjectsOfType<PhotonVRPlayer>())
        {
            if (PVRP.gameObject.GetComponent<PhotonView>().Owner == targetPlayer)
            {
                PhotonView view = GetComponent<PhotonView>();
                view.RequestOwnership();

                float waitTime = 0f;
                while (view.Owner != PhotonNetwork.LocalPlayer && waitTime < 1f) // 1s max wait time
                {
                    yield return new WaitForSeconds(0.1f);
                    waitTime += 0.1f;
                }

                if (view.Owner == PhotonNetwork.LocalPlayer)
                {
                    view.RPC("BanReportRPC", PVRP.gameObject.GetComponent<PhotonView>().Owner);
                    BanPlayer(targetPlayer, reportReason);
                    yield return new WaitForSeconds(0.1f);
                }
                else
                {
                    Debug.LogError("Failed to gain ownership before calling RPC.");
                }

                break;
            }
        }
    }

    private void BanPlayer(Photon.Realtime.Player targetPlayer, string reportReason)
    {
        string targetPlayfabID = targetPlayer.CustomProperties.ContainsKey("PlayfabID") 
            ? (string)targetPlayer.CustomProperties["PlayfabID"] 
            : "Unknown ID";

        GetComponent<PhotonView>().RPC("BanReportRPC", targetPlayer);

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "banPlayer",
            FunctionParameter = new { targetPlayfabID, duration = 48, reason = reportReason }
        };

        PlayFabClientAPI.ExecuteCloudScript(request, 
            result => 
            {
                SendToWebhook($"{targetPlayer.NickName} (PlayFab ID: {targetPlayfabID}) was reported by {PhotonNetwork.LocalPlayer.NickName} (Staff) for {reportReason} and was banned for 48 hours.", BanWebHookURL);
            },
            error => Debug.LogError("Ban request failed: " + error.ErrorMessage));
    }

    private void SendToWebhook(string message, string webhookUrl)
    {
        StartCoroutine(PostToDiscord(message, webhookUrl));
    }

    IEnumerator PostToDiscord(string message, string webhookUrl)
    {
        string jsonPayload = "{\"content\": \"" + message + "\"}";

        UnityWebRequest www = new UnityWebRequest(webhookUrl, "POST");
        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonPayload);
        www.uploadHandler = new UploadHandlerRaw(jsonToSend);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Webhook Error: " + www.error);
        }
    }
}
