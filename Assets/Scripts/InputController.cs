using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputController : MonoBehaviour
{

    public ToggleButton POSToggleButton;
    public ToggleButton NEGToggleButton;
    public ToggleButton NMLPOSToggleButton;
    public ToggleButton NMLNEGToggleButton;
    public ToggleButton KILLToggleButton;
    public ToggleButton TargetToggleButton;
    public GameObject HUDLogic;
    public GameObject RelativeVelocityDirectionIndicator;
    public GameObject RelativeVelocityAntiDirectionIndicator;
    public GameObject RelativeVelocityNormalPlusDirectionIndicator;
    public GameObject RelativeVelocityNormalMinusDirectionIndicator;
    public GameObject TargetDirectionIndicator;
    public bool ControlsEnabled = true;

    private Dictionary<string, HashSet<IPropertyChangeObserver>> propertyChangeObservers = new Dictionary<string, HashSet<IPropertyChangeObserver>>();

    public void AddObserver(string name, IPropertyChangeObserver observer)
    {
        HashSet<IPropertyChangeObserver> observers;
        if (propertyChangeObservers.TryGetValue(name, out observers))
        {
            observers.Add(observer);
        }
        else
        {
            observers = new HashSet<IPropertyChangeObserver>() { observer };
            propertyChangeObservers.Add(name, observers);
        }
    }

    public void RemoveObserver(string name, IPropertyChangeObserver observer)
    {
        HashSet<IPropertyChangeObserver> observers;
        if (propertyChangeObservers.TryGetValue(name, out observers))
        {
            observers.Remove(observer);
        }
    }

    public void PropertyChanged(string name, object value)
    {
        HashSet<IPropertyChangeObserver> observers;
        if (propertyChangeObservers.TryGetValue(name, out observers))
        {
            foreach (IPropertyChangeObserver observer in observers)
            {
                observer.PropertyChanged(name, value);
            }
        }
    }

}
