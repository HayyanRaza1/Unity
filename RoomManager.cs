using UnityEngine;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab;
    public Transform spawnPoint;
    public GameObject connectionPanel;
    public TMP_Text connectionText;
    public TMP_Text playerJoinText;
    public TMP_Text playerLeaveText;

    private const float messageDisplayDuration = 2f;
    private const int maxRetryCount = 3;
    private int retryCount = 0;

    private void Start()
    {
        ConnectToPhoton();
    }

    private void ConnectToPhoton()
    {
        Debug.Log("Connecting...");
        if (connectionPanel != null)
        {
            connectionPanel.SetActive(true);
            connectionText.text = "Connecting...";
        }

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Server");
        connectionText.text = "Connected to Server";
        retryCount = 0; // Reset retry count on successful connection

        PhotonNetwork.JoinLobby();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError("Disconnected from server: " + cause);

        if (connectionPanel != null)
        {
            connectionPanel.SetActive(true);
            connectionText.text = "Disconnected: " + cause;
        }

        if (retryCount < maxRetryCount)
        {
            retryCount++;
            Debug.Log("Retrying connection... Attempt: " + retryCount);
            ConnectToPhoton();
        }
        else
        {
            connectionText.text = "Failed to connect after " + maxRetryCount + " attempts.";
        }
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");
        connectionText.text = "Joined Lobby";

        RoomOptions roomOptions = new RoomOptions { MaxPlayers = 20 };
        PhotonNetwork.JoinOrCreateRoom("test", roomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room");
        connectionPanel.SetActive(false);

        if (playerPrefab != null && spawnPoint != null)
        {
            GameObject playerObject = PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, Quaternion.identity);
            PhotonNetwork.LocalPlayer.TagObject = playerObject;
            Debug.Log("Player instantiated: " + PhotonNetwork.LocalPlayer.NickName);
        }
        else
        {
            Debug.LogError("playerPrefab or spawnPoint is not assigned.");
        }

        playerJoinText.text = PhotonNetwork.LocalPlayer.NickName + " joined the game";
        playerJoinText.gameObject.SetActive(true);
        StartCoroutine(HideJoinMessage());
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log(newPlayer.NickName + " joined the game");
        playerJoinText.text = newPlayer.NickName + " joined the game";
        playerJoinText.gameObject.SetActive(true);
        StartCoroutine(HideJoinMessage());
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log(otherPlayer.NickName + " left the game");
        playerLeaveText.text = otherPlayer.NickName + " left the game";
        playerLeaveText.gameObject.SetActive(true);
        StartCoroutine(HideLeaveMessage());
    }

    private IEnumerator HideJoinMessage()
    {
        yield return new WaitForSeconds(messageDisplayDuration);
        playerJoinText.gameObject.SetActive(false);
    }

    private IEnumerator HideLeaveMessage()
    {
        yield return new WaitForSeconds(messageDisplayDuration);
        playerLeaveText.gameObject.SetActive(false);
    }
}
