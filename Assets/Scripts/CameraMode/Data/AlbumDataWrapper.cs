using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AlbumDataWrapper
{
    public List<AlbumEntry> entries = new List<AlbumEntry>();

    // Empty constructor for JSON loading
    public AlbumDataWrapper() { }

    // Constructor with data
    public AlbumDataWrapper(List<AlbumEntry> albumEntries)
    {
        entries = albumEntries;
    }
}
