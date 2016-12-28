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
    private Text AmmoCountText;
    private Button FireGunsButton;
    private GameObject weaponTarget;
    private List<GameObject> weaponTargets = new List<GameObject>();
    private int missileCount = 0;
    private int ammoCount = -1;

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
        AmmoCountText = panel.transform.Search("GunsCountText").GetComponent<Text>();
        FireGunsButton = panel.transform.Search("FireGunsButton").GetComponent<Button>();

        inputController.AddObserver("TargetList", this);
        inputController.AddObserver("SelectWeaponsTarget", this);
        inputController.AddObserver("MissileCount", this);
        inputController.AddObserver("AmmoCount", this);

        ArmButton.onClick.AddListener(delegate { if (inputController.ControlsEnabled) { ArmDisarm(); } });
        WeaponTargetSelectorDropdown.onValueChanged.AddListener(delegate { WeaponTargetSelectorDropdownOnValueChanged(); });
        WeaponTargetSelectorDropdown.value = 0;
        FireMissileButton.onClick.AddListener(delegate { if (inputController.ControlsEnabled) { FireMissile(); } });
        FireGunsButton.onClick.AddListener(delegate { if (inputController.ControlsEnabled) { FireGuns(); } });

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
            case "MissileCount":
                int? cv = value as int?;
                int c = cv == null ? 0 : cv.Value;
                missileCount = c;
                MissileCountText.text = c.ToString();
                break;
            case "AmmoCount":
                int? acv = value as int?;
                int ac = acv == null ? 0 : acv.Value;
                int ammoShown;
                if (ammoCount == -1) // initial
                {
                    ammoShown = ac * AmmoBurstSize;
                }
                else if (ammoCount > 1)
                {
                    ammoShown = ac * AmmoBurstSize + Random.Range(- AmmoBurstSize / 4, AmmoBurstSize / 4);
                }
                else if (ammoCount > 0)
                {
                    ammoShown = Random.Range(3 * AmmoBurstSize / 4, AmmoBurstSize);
                }
                else
                {
                    ammoShown = 0;
                }
                ammoCount = ac;
                AmmoCountText.text = ammoShown.ToString();
                break;
        }
    }

    private int AmmoBurstSize = 50;

    public void Update()
    {
        if (inputController != null)
        {
            if (isPrimaryPanel && inputController.ControlsEnabled)
            {
                UpdateKeyInput();
            }
        }
    }

    private void UpdateKeyInput()
    {
        if (Input.GetKeyDown(KeyCode.Keypad0))
        {
            FireGuns();
        }
    }

    private void Speak(string text)
    {
        gameController.GetComponent<MFDController>().Speak(text);
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
        Speak(DialogText.WeaponsArmed);
    }

    private void Disarm()
    {
        ArmButtonText.text = "ARM";
        WeaponStatusText.text = "OFF";
//        FireMissileButton.enabled = false;
//        FireGunsButton.enabled = false;
        armed = false;
        Speak(DialogText.WeaponsOffline);
    }

    public void DisarmForDocking()
    {
        if (armed)
        {
            ArmButtonText.text = "ARM";
            WeaponStatusText.text = "OFF";
            armed = false;
        }
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
        string msg;
        if (missileCount <= 0)
        {
            msg = DialogText.NoMissilesRemaining;
        }
        else if (weaponTarget == null)
        {
            msg = DialogText.SelectTargetToFire;
        }
        else if (!armed)
        {
            msg = DialogText.WeaponsOffline;
        }
        else
        {
            if (gameController.GetPlayerShip().FireFirstAvailableMissile(weaponTarget))
            {
                msg = DialogText.FireMissile;
            }
            else
            {
                msg = DialogText.WeaponsMalfunction;
            }
        }
        if (!string.IsNullOrEmpty(msg))
        {
            Speak(msg);
        }
    }

    private void FireGuns()
    {
        string msg;
        if (ammoCount <= 0)
        {
            msg = DialogText.NoAmmoRemaining;
        }
        else if (!armed)
        {
            msg = DialogText.WeaponsOffline;
        }
        else if (!gameController.GetPlayerShip().GunsReady())
        {
            msg = DialogText.GunsTooHot;
        }
        else
        {
            if (gameController.GetPlayerShip().FireGuns())
            {
                msg = "";
            }
            else
            {
                msg = DialogText.WeaponsMalfunction;
            }
        }
        if (!string.IsNullOrEmpty(msg))
        {
            Speak(msg);
        }
    }
}
