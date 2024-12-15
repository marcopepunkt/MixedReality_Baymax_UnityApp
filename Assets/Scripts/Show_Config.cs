using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Show_Config : MonoBehaviour
{
    public GameObject targetObject; // Reference to the GameObject you want to toggle

    // Call this method to toggle the active state
    public void ToggleActiveState()
    {
        if (targetObject != null)
        {
            bool isActive = targetObject.activeSelf; // Check current active state
            targetObject.SetActive(!isActive); // Toggle the state
            Debug.Log("Toggled object to: " + !isActive);
        }
        else
        {
            Debug.LogWarning("Target object is not assigned!");
        }
    }

    public void Hide()
    {
        targetObject.SetActive(false);
    }

    public void Show()
    {
        targetObject.SetActive(true);
    }
}
