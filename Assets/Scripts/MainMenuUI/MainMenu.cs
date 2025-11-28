using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public void PlayGame ()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildindex + 1); // Loads the next scene
    }

    public void QuitGame ()
    {
        application.Quit();
    }
}

