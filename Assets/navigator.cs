using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Give_introduction : MonoBehaviour
{
    public IO_Setup io_setup;


    public void give_introduction()
    {
        Debug.Log("Called Navigator.");
        io_setup.PlayTextToSpeech("Hello, I am your navigator. You will have two modes to use. The first mode is to describe your sourrounding. By saying Hey Baymax, you can figure out what is inf");
        io_setup.PlayTextToSpeech("If yo.");
    }
}
