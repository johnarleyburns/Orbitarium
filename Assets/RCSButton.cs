using UnityEngine;
using UnityEngine.EventSystems;

public class RCSButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {

    public InputController inputController;
    private string nameStr;

	// Use this for initialization
	void Start () {
        nameStr = transform.name;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void OnPointerDown(PointerEventData data)
    {
        if (inputController != null && !string.IsNullOrEmpty(name))
        {
            inputController.PropertyChanged(nameStr + "_OnPointerDown", data);
        }
    }

    public void OnPointerUp(PointerEventData data)
    {
        if (inputController != null && !string.IsNullOrEmpty(name))
        {
            inputController.PropertyChanged(nameStr + "_OnPointerUp", data);
        }
    }
}
