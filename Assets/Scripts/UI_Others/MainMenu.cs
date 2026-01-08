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
<<<<<<< HEAD

    public void LoadGame ()
    {
        Debug.Log("START PLEASE!"); // throws the given text into the debugger if successful
        LevelLoader.Instance.LoadLevel(); // Loads the next scene, which should be the hub area.
    }

=======
>>>>>>> dfba167753e38eb7c1497ad38693f48e5ad64fe1
    public void QuitGame ()
    {
        Debug.Log("QUIT PLEASE!"); // throws the given text into the debugger if successful
        Application.Quit(); // closes the game
    }
}

