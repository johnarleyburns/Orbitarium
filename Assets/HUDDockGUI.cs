using UnityEngine;
using System.Collections;

public class HUDDockGUI : MonoBehaviour {

    private GUIContent content;
    private GUIStyle style = new GUIStyle();

    void Start()
    {
        content = new GUIContent();
    }

    void OnGUI()
    {
        GUI.Box(new Rect(Screen.width/2 - 100, Screen.height/2 - 100, Screen.width/2 + 100, Screen.height/2 + 100), content, style);
    }

}
