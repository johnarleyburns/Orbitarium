using UnityEngine;
using UnityEngine.EventSystems;

public class RCSButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {

    public InputController inputController;

    public void OnPointerDown(PointerEventData data)
    {
        if (inputController != null && inputController.ControlsEnabled)
        {
            inputController.PropertyChanged(transform.name + "_OnPointerDown", data);
        }
    }

    public void OnPointerUp(PointerEventData data)
    {
        if (inputController != null && inputController.ControlsEnabled)
        {
            inputController.PropertyChanged(transform.name + "_OnPointerUp", data);
        }
    }
}
