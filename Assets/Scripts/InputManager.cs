using System;
using UnityEngine;
using UnityEngine.PlayerLoop;


public class InputManager : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown("escape")) Application.Quit();
        }
    }