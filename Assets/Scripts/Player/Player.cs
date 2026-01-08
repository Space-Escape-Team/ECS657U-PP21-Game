using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public PlayerPuzzleInteractor playerPuzzleInteractor;
    // a public variable for the position of the player also?
    public int times_completed;
    public void SavePlayer()
    {
        SaveSystem.SavePlayer(this);
    }

    public void LoadPlayer()
    {
        
    }
}
