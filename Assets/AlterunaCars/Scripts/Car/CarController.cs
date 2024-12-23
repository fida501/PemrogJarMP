﻿using System.Collections;
using Mirror;
using Alteruna;
using AlterunaCars;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

// namespace AlterunaCars
// {
[RequireComponent(typeof(InputSynchronizable), typeof(AudioSource))]
public class CarController : NetworkBehaviour
{
    private const float ENGINE_SMOOTHING = 1f;
    private const float WHEEL_TORQUE_SMOOTHING = 1f;
    private const float STEERING_SMOOTHING = 0.5f;

    [SerializeField] [HideInInspector] private InputSynchronizable _inputManager;

    [SerializeField] private WheelController[] wheels;
    [SerializeField] private AudioSource driftSource;
    [SerializeField] private AudioSource engineSource;
    [SerializeField] private Transform centerOfMass;
    public float _steering;

    private SyncedKey _handbrake;
    private bool _isMe = true;

    private Rigidbody _rb;
    private bool _stopped;
    private float _targetDriftPitch;
    private float _targetDriftVolume;
    private float _targetEngineVolume;
    private SyncedAxis _targetSteering;
    private SyncedAxis _targetTorque;

    private float _torque;

    [Header("Player Controller")] public GameObject playerModel;
    public GameObject playerCamera;
    public GameObject playerCanvas;
    public PlayerUIController playerUIController;
    [SerializeField] PlayerObjectController playerObjectController;
    private bool _isSpeedBoostActive = false;
        private bool _isAttacktActive = false;
    [SyncVar] public int lapIndex;
    [SerializeField] private AudioSource powerUpSource;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int bulletSpeed = 50;
    [SerializeField] private int bulletDelay = 1;


    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        playerCamera.SetActive(true);
    }


    private new void Reset()
    {
        // base.Reset();
        // _inputManager = GetComponent<InputSynchronizable>();
        //reset player position
        // change z rotation to 0
        transform.rotation = quaternion.Euler(transform.rotation.x, transform.rotation.y, 0f);
    }

    private void Start()
    {
        //Disable player model
        playerModel.SetActive(false);
        if (!isLocalPlayer)
        {
            playerCamera.SetActive(false);
        }

        if (_inputManager == null) _inputManager = GetComponent<InputSynchronizable>();

        _rb = GetComponent<Rigidbody>();
        if (centerOfMass != null) _rb.centerOfMass = centerOfMass.localPosition;

        // Setup inputs.
        _handbrake = new SyncedKey(_inputManager, KeyCode.Space);
        _targetSteering = new SyncedAxis(_inputManager, "Horizontal");
        _targetTorque = new SyncedAxis(_inputManager, "Vertical");

        // Set owner for wheels.
        foreach (var wheel in wheels) wheel.CarController = this;
    }

    private void FixedUpdate()
    {
        if (SceneManager.GetActiveScene().name == "Game")
        {
            if (playerModel.activeSelf == false)
            {
                SetPosition();
                playerModel.SetActive(true);
            }

            if (isOwned)
            {
                if (GameManager.instance.raceStatus == "finish")
                {
                    playerCanvas.SetActive(false);
                    return;
                }

                if (GameManager.instance.raceStatus != "start")
                {
                    SetPosition();
                    return;
                }

                //Check if canvas is not active
                if (!playerCanvas.activeSelf)
                {
                    EnablePlayerEssentials();
                }

                MovingCar();
                if (Input.GetKeyUp(KeyCode.R))
                {
                    Reset();
                }
                //input click kiri untuk menembak
                if (Input.GetMouseButtonDown(0))
                {
                    // Fire();
                    StartCoroutine(fireIEnumerator());
                }
            }
        }
    }

    public void EnablePlayerEssentials()
    {
        playerCanvas.SetActive(true);
    }

    // public override void Possessed(bool isMe, User user)
    // {
    // 	_isMe = isMe;
    // 	if (RacingUI.Instance != null) RacingUI.Instance.gameObject.SetActive(true);
    // }

    public void SetDrift(float drift)
    {
        _targetDriftVolume = Mathf.Max(_targetDriftVolume, drift);
        _targetDriftPitch = Mathf.Max(_targetDriftPitch, drift);
    }

    private void DeltaSmoothing(ref float value, float target, float smoothing) =>
        value = Mathf.LerpUnclamped(value, target, Mathf.Min(Time.fixedDeltaTime / smoothing, 1f));


    private void MovingCar()
    {
        DeltaSmoothing(ref _steering, _targetSteering.Value, STEERING_SMOOTHING);

        #region Drift

        //Check if driftsource null
        if (driftSource != null)
        {
            if (driftSource.isPlaying)
            {
                if (_targetDriftVolume < 0.01f)
                    driftSource.Stop();
                else
                    UpdateDrift();

                ResetDrift();
            }
            else if (_targetDriftVolume > 0.01f)
            {
                if (!_stopped)
                {
                    UpdateDrift();
                    driftSource.Play();
                    ResetDrift();
                }
            }
        }

        void ResetDrift()
        {
            _targetDriftVolume = 0;
            _targetDriftPitch = 0;
        }

        void UpdateDrift()
        {
            driftSource.pitch = Mathf.Lerp(driftSource.pitch, _targetDriftPitch, Time.fixedDeltaTime * 10);
            driftSource.volume = Mathf.Lerp(driftSource.volume, _targetDriftVolume, Time.fixedDeltaTime * 10);
        }

        #endregion

        #region Engine

        DeltaSmoothing(ref _torque, _targetTorque, WHEEL_TORQUE_SMOOTHING);

        var speed = Vector3.Dot(transform.forward, _rb.velocity);
        var speedAbs = Mathf.Abs(speed);

        var engineTarget = EnginePower(speed, _targetTorque);
        var torqueToWheel = _torque;

        // quick brake fix
        if (torqueToWheel > 0 && _targetTorque < 0)
        {
            torqueToWheel = _targetTorque;
        }
        // stop reverse when moving forward
        else if (torqueToWheel < 0 && _targetTorque > 0)
        {
            _torque = 0;
            torqueToWheel = 0;
        }


        static float EnginePower(float speed, float torque)
        {
            if (torque < 0)
            {
                if (speed > 0.01f) return 0;
            }
            else if (torque > 0)
            {
                if (speed < -0.01f) return 0;
            }

            return Mathf.Abs(torque);
        }

        DeltaSmoothing(ref _targetEngineVolume, engineTarget, ENGINE_SMOOTHING);

        if (_targetEngineVolume < 0.005f && speedAbs < 0.1f)
        {
            // stop engine and fade out sound
            if (engineSource != null)
            {
                if (engineSource.isPlaying)
                {
                    if (engineSource.volume < 0.01f)
                    {
                        engineSource.Stop();
                        _stopped = true;
                    }
                }
                else
                {
                    engineSource.volume = Mathf.Lerp(engineSource.volume, 0, Time.fixedDeltaTime * 10);
                }
            }
        }
        else
        {
            _targetEngineVolume = Mathf.Max(_targetEngineVolume, 0.005f);
            if (engineSource != null)
            {
                if (!engineSource.isPlaying)
                {
                    engineSource.Play();
                    _stopped = false;
                }
            }

            if (engineSource != null)
            {
                engineSource.volume = 0.8f + _targetEngineVolume * 0.05f;
                engineSource.pitch = _targetEngineVolume * 0.6f + Mathf.Lerp(0f, 0.5f, speed / 200) + 0.1f;
            }
        }

        #endregion

        #region Apply to wheels

        if (TrackController.IsStarted)
        {
            float speedMultiplier = _isSpeedBoostActive ? 2 : 1;
            foreach (var wheel in wheels)
                wheel.UpdateWheel(_steering, torqueToWheel * speedMultiplier, _handbrake);
        }
        else
            foreach (var wheel in wheels)
                wheel.UpdateWheel(_steering, 0, true);

        #endregion

        if (!_isMe) return;

        // if (RacingUI.Instance != null)
        // {
        //     RacingUI.Instance.SetSpeed(speed);
        // }

        playerUIController.UpdateSpeed(speed);
        playerUIController.UpdateTimer();
    }

    private void SetPosition()
    {
        int playerIndex = playerObjectController.PlayerIdNumber;
        GameObject spawnPoint = null;
        switch (playerIndex)
        {
            case 1:
                spawnPoint = GameManager.instance.respawnPoints[0];
                break;
            case 2:
                spawnPoint = GameManager.instance.respawnPoints[1];
                break;
            case 3:
                spawnPoint = GameManager.instance.respawnPoints[2];
                break;
            case 4:
                spawnPoint = GameManager.instance.respawnPoints[3];
                break;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point is null");
            return;
        }

        transform.position = spawnPoint.transform.position;
        transform.rotation = spawnPoint.transform.rotation;
        _rb.constraints = RigidbodyConstraints.None;
    }


    //fire mekanik
    // private void Fire()
    // {
    //     GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
    //     bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * bulletSpeed;
    //     NetworkServer.Spawn(bullet);
    //     //destroy after 2 second
    //     Destroy(bullet, 2);
    //     NetworkServer.Destroy(bullet);
    // }
    private IEnumerator fireIEnumerator()
    {
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * bulletSpeed;
        NetworkServer.Spawn(bullet);
        //destroy after 2 second
        yield return new WaitForSeconds(2);
        NetworkServer.Destroy(bullet);
    }

    public void ActiveSpeedPowerUp()
    {
        _isSpeedBoostActive = true;
        Debug.Log("SpeedBoost activated is " + _isSpeedBoostActive);
        StartCoroutine(DisableSpeedPowerUp());
    }

    // public void ActiveAttackPowerUp(){
    //     _isAttacktActive = true;
    //     Debug.Log("Attack activated is " + _isAttacktActive);
    //     StartCoroutine(DisableAttackPowerUp());
    // }

    private IEnumerator DisableSpeedPowerUp()
    {
        powerUpSource.enabled = true;
        yield return new WaitForSeconds(3);
        powerUpSource.enabled = false;
        _isSpeedBoostActive = false;
    }

    public void CheckLapIndex()
    {
        lapIndex++;
        if (lapIndex == 1)
        {
            GameManager.instance.FinishRace(gameObject);
        }
    }

    // private IEnumerator DisableAttackPowerUp(){
    //     Fire();
    //     yield return new WaitForSeconds(1f);
    //     Fire();
    //     yield return new WaitForSeconds(1f);
    //     Fire();
    //     yield return new WaitForSeconds(1f);
    //     powerUpSource.enabled = false;
    //     _isAttacktActive = false;
    // }
}