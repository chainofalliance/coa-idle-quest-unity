using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEntry 
{
    string EntryName { get; set; }
    long Price { get; set; }

    void Destroy();
    void Deselect();
}
