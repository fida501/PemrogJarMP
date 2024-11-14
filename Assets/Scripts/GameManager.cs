using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;

    [SyncVar] public string raceStatus = "start";
    [SyncVar] public float gameTimer = 0f;
    public List<GameObject> respawnPoints = new List<GameObject>();
    public GameObject globalCanvas;
    public TextMeshProUGUI globalTMP;
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
        // CmdInitializeRace();
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
    private void CmdInitializeRace()
    {
        isStarted = true;
        gameTimer = 0f;
        StartCoroutine(ChangeStatusAfterDelay("start"));
        // raceStatus = "start";
    }


    private void Update()
    {
        if (raceStatus == "start")
        {
            gameTimer += Time.deltaTime;
        }

        if (!isStarted)
        {
            CmdInitializeRace();
        }
    }

    private IEnumerator ChangeStatusAfterDelay(string newStatus)
    {
        yield return new WaitForSeconds(1.5f);
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

    private void DisableEssentials()
    {
        globalCanvas.SetActive(false);
    }

    // Update the SyncVar value on the server
    private void UpdateStatus(string newStatus)
    {
        raceStatus = newStatus; // This will automatically sync to all clients
    }

    private void UpdateGlobalTMPText(string countdownText)
    {
        globalTMP.text = countdownText;
    }
}