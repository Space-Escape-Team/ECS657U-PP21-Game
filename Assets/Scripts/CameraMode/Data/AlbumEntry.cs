using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable] // For JsonUtility
public class AlbumEntry
{
    public string photoBase64;
    public string timestamp;
    public bool containsEnemy;
    public string caption;

    // Constructor for easy creation
    public AlbumEntry(string base64, string time, bool enemy)
    {
        photoBase64 = base64;
        timestamp = time;
        containsEnemy = enemy;
        caption = "";
    }
}
