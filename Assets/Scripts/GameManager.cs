using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;

    [SyncVar] public string raceStatus = "start";
    [SyncVar] public float gameTimer = 0f;
    public List<GameObject> respawnPoints = new List<GameObject>();
    public GameObject globalCanvas;
    [SyncVar] public GameObject globalTMP;
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

    // [Server]
    // private void InitializeRace()
    // {
    //     gameTimer = 0f;
    //     StartCoroutine(ChangeStatusAfterDelay("start"));
    //     // raceStatus = "start";
    // }
    // [Server]
    // private void CmdInitializeRace()
    // {
    //     gameTimer = 0f;
    //     StartCoroutine(ChangeStatusAfterDelay("start"));
    //     // raceStatus = "start";
    // }
    [ServerCallback]
    private void CmdInitializeRace()
    {
        isStarted = true;
        gameTimer = 0f;
        StartCoroutine(ChangeStatusAfterDelay("start"));
        // raceStatus = "start";
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
        globalCanvas.SetActive(true);
        globalTMP.SetActive(true);
        UpdateGlobalTMPText("3");
        yield return new WaitForSeconds(1f);
        UpdateGlobalTMPText("2");
        yield return new WaitForSeconds(1f);
        UpdateGlobalTMPText("1");
        yield return new WaitForSeconds(1f);
        UpdateGlobalTMPText("GO!");
        yield return new WaitForSeconds(1f);
        DisableEssentials();
        UpdateStatus(newStatus); // Update the SyncVar on the server
    }

    [ServerCallback]
    public void DisableEssentials()
    {
        globalCanvas.SetActive(false);
        globalTMP.SetActive(false);
    }

    // Update the SyncVar value on the server
    [ServerCallback]
    private void UpdateStatus(string newStatus)
    {
        raceStatus = newStatus; // This will automatically sync to all clients
    }

    [ServerCallback]
    private void UpdateGlobalTMPText(string countdownText)
    {
        globalTMP.GetComponent<TMPro.TextMeshProUGUI>().text = countdownText;
    }

    public bool AreAllPlayerConnected()
    {
        return NetworkServer.connections.Count >= 2;
    }
}