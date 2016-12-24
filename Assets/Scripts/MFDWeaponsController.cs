using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MFDWeaponsController : IPropertyChangeObserver
{
    private GameObject panel;
    private InputController inputController;
    private GameController gameController;
    private bool isPrimaryPanel = false;

    private bool armed;

    private Text WeaponStatusText;
    private ToggleButton ArmButton;
    private Text ArmButtonText;
    private Dropdown WeaponTargetSelectorDropdown;
    private Text MissileCountText;
    private Button FireMissileButton;
    private Text GunsCountText;
    private Button FireGunsButton;
    private GameObject weaponTarget;
    private List<GameObject> weaponTargets = new List<GameObject>();

    public void Connect(GameObject weaponsPanel, InputController inputControl, GameController gameControl, bool panelIsPrimaryPanel)
    {
        panel = weaponsPanel;
        inputController = inputControl;
        gameController = gameControl;
        isPrimaryPanel = panelIsPrimaryPanel;

        WeaponStatusText = panel.transform.Search("WeaponStatusText").GetComponent<Text>();
        ArmButton = panel.transform.Search("ArmButton").GetComponent<ToggleButton>();
        ArmButtonText = panel.transform.Search("ArmButtonText").GetComponent<Text>();
        WeaponTargetSelectorDropdown = panel.transform.Search("WeaponTargetSelectorDropdown").GetComponent<Dropdown>();
        MissileCountText = panel.transform.Search("MissileCountText").GetComponent<Text>();
        FireMissileButton = panel.transform.Search("FireMissileButton").GetComponent<Button>();
        GunsCountText = panel.transform.Search("GunsCountText").GetComponent<Text>();
        FireGunsButton = panel.transform.Search("FireGunsButton").GetComponent<Button>();

        inputController.AddObserver("TargetList", this);
        inputController.AddObserver("SelectWeaponsTarget", this);
        inputController.AddObserver("MissileCountText", this);

        ArmButton.onClick.AddListener(delegate { ArmDisarm(); });
        WeaponTargetSelectorDropdown.onValueChanged.AddListener(delegate { WeaponTargetSelectorDropdownOnValueChanged(); });
        WeaponTargetSelectorDropdown.value = 0;
        FireMissileButton.onClick.AddListener(delegate { FireMissile(); });

        SyncWeaponTarget();
        Disarm();
    }

    public void PropertyChanged(string name, object value)
    {
        switch (name)
        {
            case "TargetList":
                GameObject oldTarget = weaponTarget;
                List<GameObject> targets = value as List<GameObject>;
                GameObject closestEnemy = gameController.NextClosestTarget(gameController.GetPlayer(), TargetDB.TargetType.ENEMY);
                if (targets != null)
                {
                    List<string> names = new List<string>();
                    names.Add("No Target");
                    weaponTargets.Clear();
                    int closestEnemyIndex = 0;
                    int i = 0;
                    foreach (GameObject g in targets)
                    {
                        if (gameController.TargetData().GetTargetType(g) == TargetDB.TargetType.ENEMY)
                        {
                            names.Add(g.name);
                            weaponTargets.Add(g);
                            if (g == closestEnemy)
                            {
                                closestEnemyIndex = i + 1;
                                weaponTarget = closestEnemy;
                            }
                            i++;
                        }
                    }
                    WeaponTargetSelectorDropdown.ClearOptions();
                    WeaponTargetSelectorDropdown.AddOptions(names);
                    WeaponTargetSelectorDropdown.value = closestEnemyIndex;
                }
                break;
            case "SelectWeaponTarget":
                weaponTarget = value as GameObject;
                for (int i = 0; i < weaponTargets.Count; i++)
                {
                    GameObject g = weaponTargets[i];
                    if (g == weaponTarget)
                    {
                        WeaponTargetSelectorDropdown.value = i + 1;
                    }
                }
                break;
            case "MissileCountText":
                MissileCountText.text = value as string;
                break;
        }
    }

    private void ArmDisarm()
    {
        if (ArmButton.isToggled)
        {
            Arm();
        }
        else
        {
            Disarm();
        }
    }

    private void Arm()
    {
        ArmButtonText.text = "ARMED";
        WeaponStatusText.text = "ONLINE";
//        FireMissileButton.enabled = true;
//        FireGunsButton.enabled = true;
        armed = true;
    }

    private void Disarm()
    {
        ArmButtonText.text = "ARM";
        WeaponStatusText.text = "OFF";
//        FireMissileButton.enabled = false;
//        FireGunsButton.enabled = false;
        armed = false;
    }

    private void SyncWeaponTarget()
    {
        int tgtVal = WeaponTargetSelectorDropdown.value - 1;
        if (tgtVal >= 0 && tgtVal < weaponTargets.Count)
        {
            weaponTarget = weaponTargets[tgtVal];
        }
        else
        {
            weaponTarget = null;
        }
    }

    private void WeaponTargetSelectorDropdownOnValueChanged()
    {
        SyncWeaponTarget();
    }

    private void FireMissile()
    {
        if (armed && weaponTarget != null)
        {
            gameController.GetPlayerShip().FireFirstAvailableMissile(weaponTarget);
        }
    }

    private void FireGuns()
    {
        if (armed)
        {
//            gameController.GetPlayerShip().FireFirstAvailableMissile(weaponTarget);
        }
    }
}
