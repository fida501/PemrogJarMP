using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;

    [SyncVar(hook = nameof(OnRaceStatusChanged))] public string raceStatus = "start";
    [SyncVar] public float gameTimer = 0f;
    public List<GameObject> respawnPoints = new List<GameObject>();
    public GameObject globalCanvas;
    public TextMeshProUGUI globalTMP; // Instead of SyncVar for GameObject, use TextMeshProUGUI directly
    [SyncVar] public List<GameObject> players = new List<GameObject>();
    public bool isStarted = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        CmdInitializeRace();
    }

    [ServerCallback]
    private void CmdInitializeRace()
    {
        isStarted = true;
        gameTimer = 0f;
        StartCoroutine(ChangeStatusAfterDelay("start"));
    }

    [ServerCallback]
    private void Update()
    {
        if (raceStatus == "start")
        {
            gameTimer += Time.deltaTime;
        }

        if (isStarted)
        {
            return;
        }

        if (AreAllPlayerConnected() == false)
        {
            return;
        }

        CmdInitializeRace();
    }

    private IEnumerator ChangeStatusAfterDelay(string newStatus)
    {
        globalCanvas.SetActive(true);  // Display canvas and countdown
        UpdateGlobalTMPText("3");
        yield return new WaitForSeconds(1f);
        UpdateGlobalTMPText("2");
        yield return new WaitForSeconds(1f);
        UpdateGlobalTMPText("1");
        yield return new WaitForSeconds(1f);
        UpdateGlobalTMPText("GO!");
        yield return new WaitForSeconds(1f);
        DisableEssentials();
        UpdateStatus(newStatus);  // Update the SyncVar on the server
    }

    [ServerCallback]
    public void DisableEssentials()
    {
        globalCanvas.SetActive(false);
    }

    // Update the SyncVar value on the server
    [ServerCallback]
    private void UpdateStatus(string newStatus)
    {
        raceStatus = newStatus;  // This will automatically sync to all clients
    }

    [ServerCallback]
    private void UpdateGlobalTMPText(string countdownText)
    {
        globalTMP.text = countdownText;  // Update the TMP text directly
    }

    // Sync the race status across clients (using SyncVar hook)
    private void OnRaceStatusChanged(string oldStatus, string newStatus)
    {
        // Handle any behavior that should occur when the race status changes
        // For example, update UI or trigger other events based on race status
    }

    public bool AreAllPlayerConnected()
    {
        return NetworkServer.connections.Count >= 2;  // Minimum number of players to start the race
    }
}
