using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Disable : MonoBehaviour
{

    public GameObject objectDisable;


    void OnTriggerEnter()
    {
        objectDisable.SetActive(false);
    }
}