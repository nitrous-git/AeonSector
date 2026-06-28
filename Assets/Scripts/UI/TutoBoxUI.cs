using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutoBoxUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button closeTutoBox;

    public void CloseTutoBox()
    {
        Debug.Log("CloseTutoBox");
        this.gameObject.active = false;
    }

}
