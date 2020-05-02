using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

public class PlayerPrefsDeleteEditor : Editor
{
    [MenuItem("Shortcuts/ClearCache")]
    static void ClearData()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("Data Cleared");
    }

    [MenuItem("Shortcuts/ToogleObject #a")]
    static void DeactivateObject()
    {
        GameObject obj = Selection.activeGameObject;
        if(obj.activeSelf)
        {
            obj.SetActive(false);
        }
        else
        {
            obj.SetActive(true);
        }
    }

}
