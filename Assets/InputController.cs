using UnityEngine;
using UnityEngine.UI;

public class InputController : MonoBehaviour
{
    public ToggleButton RotateButton;
    public ToggleButton TranslateButton;
    public ToggleButton StopThrustButton;
    public ToggleButton GoThrustButton;
    public Slider FuelSlider;
    public Text FuelRemainingText;
    public ToggleButton DumpFuelButton;
    public GameObject HUDLogic;
    public GameObject RelativeVelocityDirectionIndicator;
    public GameObject RelativeVelocityAntiDirectionIndicator;
}
