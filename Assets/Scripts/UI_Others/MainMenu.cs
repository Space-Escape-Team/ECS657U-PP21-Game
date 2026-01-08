using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour // the methods for the main menu
{
    public void PlayGame ()
    {
        Debug.Log("START PLEASE!"); // throws the given text into the debugger if successful
        LevelLoader.Instance.LoadNextLevel(); // Loads the next scene, which should be the hub area.
    }
    public void QuitGame ()
    {
        Debug.Log("QUIT PLEASE!"); // throws the given text into the debugger if successful
        Application.Quit(); // closes the game
    }
}

