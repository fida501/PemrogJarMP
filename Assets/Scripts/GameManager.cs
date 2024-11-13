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
    public TextMeshProUGUI globalTMP; // Reference to the TextMeshPro component
    [SyncVar] public bool isCanvasVisible = false; // Sync the visibility of the canvas
    [SyncVar] public string countdownText = "3"; // Sync the countdown text
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
        // Update countdown and canvas visibility
        SetCanvasVisibility(true); // Show the canvas
        UpdateGlobalTMPText("3");
        yield return new WaitForSeconds(1f);
        UpdateGlobalTMPText("2");
        yield return new WaitForSeconds(1f);
        UpdateGlobalTMPText("1");
        yield return new WaitForSeconds(1f);
        UpdateGlobalTMPText("GO!");
        yield return new WaitForSeconds(1f);
        DisableEssentials();
        UpdateStatus(newStatus);
    }

    [ServerCallback]
    private void DisableEssentials()
    {
        SetCanvasVisibility(false); // Hide the canvas
    }

    // Update the SyncVar value on the server
    [ServerCallback]
    private void UpdateStatus(string newStatus)
    {
        raceStatus = newStatus;  // This will automatically sync to all clients
    }

    // Update the countdown text
    [ServerCallback]
    private void UpdateGlobalTMPText(string countdownText)
    {
        this.countdownText = countdownText; // Sync the countdown text value
    }

    // Set the canvas visibility and sync it across clients
    [Server]
    private void SetCanvasVisibility(bool isVisible)
    {
        isCanvasVisible = isVisible;  // Sync the visibility across clients
    }

    // Sync the race status across clients (using SyncVar hook)
    private void OnRaceStatusChanged(string oldStatus, string newStatus)
    {
        // Handle any behavior that should occur when the race status changes
        // For example, update UI or trigger other events based on race status
    }

    // Handle updating the canvas visibility and text updates on the client side
    [ClientRpc]
    private void RpcUpdateCanvasVisibilityAndText(bool isVisible, string countdownText)
    {
        globalCanvas.SetActive(isVisible);  // Set the canvas active state
        globalTMP.text = countdownText;  // Update the countdown text
    }

    // Handle client-side updates for the canvas visibility
    [ServerCallback]
    public void UpdateCanvasVisibilityOnClients()
    {
        RpcUpdateCanvasVisibilityAndText(isCanvasVisible, countdownText); // Sync with all clients
    }

    public bool AreAllPlayerConnected()
    {
        return NetworkServer.connections.Count >= 2;  // Minimum number of players to start the race
    }
}
