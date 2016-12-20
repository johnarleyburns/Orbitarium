using UnityEngine;
using System.Collections;

public class MFDWeaponsController : IPropertyChangeObserver
{
    private GameObject panel;
    private bool isPrimaryPanel = false;
    //        private Text targetText;

    public void Connect(GameObject weaponsPanel, InputController inputController, bool panelIsPrimaryPanel)
    {
        panel = weaponsPanel;
        isPrimaryPanel = panelIsPrimaryPanel;
        //            targetText = panel.transform.Search("TargetText").GetComponent<Text>();
        //            inputController.AddObserver("TargetText", this);
    }

    public void PropertyChanged(string name, object value)
    {
        switch (name)
        {
            case "TargetText":
                //                    targetText.text = value as string;
                break;
        }
    }

}
