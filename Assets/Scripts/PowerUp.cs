using System;
using System.Collections;
using System.Collections.Generic;
using AlterunaCars;
using Mirror;
using UnityEngine;


public class PowerUp : NetworkBehaviour
{
    public enum PowerUpType
    {
        SpeedBost,
        Attack
    }

    public PowerUpType powerUpType;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            CarController carController = other.GetComponent<CarController>();
            if (carController != null)
            {
                ApplyPowerUp(other.GetComponent<CarController>());
                NetworkServer.Destroy(gameObject);
            }
        }
    }

    private void ApplyPowerUp(CarController carController)
    {
        switch (powerUpType)
        {
            case PowerUpType.SpeedBost:
                Debug.Log("SpeedBoost");
                carController.ActiveSpeedPowerUp();
                break;
            case PowerUpType.Attack:
                Debug.Log("Attack");
                break;
        }
    }
}