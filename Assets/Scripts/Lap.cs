using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Lap : NetworkBehaviour
{
    public bool isFinishTrigger = false;
    public GameObject finishLine;
    public GameObject finishMarker;
    private void Start()
    {
        finishLine.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            if (!isFinishTrigger)
            {
                finishLine.SetActive(true);
                return;
            }

            other.GetComponent<CarController>().CheckLapIndex();
            gameObject.SetActive(false);
            finishMarker.SetActive(true);
        }
    }
}