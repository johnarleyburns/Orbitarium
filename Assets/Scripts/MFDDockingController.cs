using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MFDDockingController : IPropertyChangeObserver
{
    private InputController inputController;
    private GameController gameController;
    private GameObject panel;
    private bool isPrimaryPanel = false;
    private Dropdown DockTargetSelectorDropdown;
    private Text ClosingDistText;
    private Text ClosingVText;
    private Text ClosingTimeText;
    private Text DockAnglePitchText;
    private Text DockAngleRollText;
    private Text DockAngleYawText;
    private RectTransform DockingX;
    private List<GameObject> dockTargets = new List<GameObject>();
    private GameObject dockTarget;

    public void Connect(GameObject autoPanel, InputController input, GameController game, bool panelIsPrimaryPanel)
    {
        inputController = input;
        gameController = game;
        panel = autoPanel;
        isPrimaryPanel = panelIsPrimaryPanel;

        DockTargetSelectorDropdown = panel.transform.Search("DockTargetSelectorDropdown").GetComponent<Dropdown>();
        ClosingDistText = panel.transform.Search("ClosingDistText").GetComponent<Text>();
        ClosingVText = panel.transform.Search("ClosingVText").GetComponent<Text>();
        ClosingTimeText = panel.transform.Search("ClosingTimeText").GetComponent<Text>();
        DockAnglePitchText = panel.transform.Search("DockAnglePitchText").GetComponent<Text>();
        DockAngleRollText = panel.transform.Search("DockAngleRollText").GetComponent<Text>();
        DockAngleYawText = panel.transform.Search("DockAngleYawText").GetComponent<Text>();
        DockingX = panel.transform.Search("DockingX").GetComponent<RectTransform>();

        inputController.AddObserver("TargetList", this);
        inputController.AddObserver("ClosingDistText", this);
        inputController.AddObserver("ClosingVText", this);
        inputController.AddObserver("ClosingTimeText", this);
        inputController.AddObserver("DockAnglePitchText", this);
        inputController.AddObserver("DockAngleRollText", this);
        inputController.AddObserver("DockAngleYawText", this);
        inputController.AddObserver("DockingX", this);
        inputController.AddObserver("DockingXRoll", this);

        ClosingDistText.text = "INF";
        ClosingVText.text = "INF";
        ClosingTimeText.text = "INF";
        DockingX.anchoredPosition = Vector2.zero;

        DockTargetSelectorDropdown.onValueChanged.AddListener(delegate { DockTargetSelectorDropdownOnValueChanged(); });
        DockTargetSelectorDropdown.value = 0;
        SyncDockTarget();
    }

    public void PropertyChanged(string name, object value)
    {
        switch (name)
        {
            case "TargetList":
                GameObject oldTarget = dockTarget;
                List<GameObject> targets = value as List<GameObject>;
                if (targets != null)
                {
                    List<string> names = new List<string>();
                    names.Add("No Target");
                    dockTargets.Clear();
                    foreach (GameObject g in targets)
                    {
                        if (gameController.TargetData().GetTargetType(g) == TargetDB.TargetType.DOCK)
                        {
                            names.Add(g.name);
                            dockTargets.Add(g);
                        }
                    }
                    DockTargetSelectorDropdown.ClearOptions();
                    DockTargetSelectorDropdown.AddOptions(names);
                    DockTargetSelectorDropdown.value = 0;
                }
                break;
            case "ClosingDistText":
                ClosingDistText.text = value as string;
                break;
            case "ClosingVText":
                ClosingVText.text = value as string;
                break;
            case "ClosingTimeText":
                ClosingTimeText.text = value as string;
                break;
            case "DockAnglePitchText":
                DockAnglePitchText.text = value as string;
                break;
            case "DockAngleRollText":
                DockAngleRollText.text = value as string;
                break;
            case "DockAngleYawText":
                DockAngleYawText.text = value as string;
                break;
            case "DockingX":
                Vector2? vec = value as Vector2?;
                Vector2 planarVec = vec == null ? Vector2.zero : vec.Value;
                UpdateDockingX(planarVec);
                break;
            case "DockingXRoll":
                float? roll = value as float?;
                float r = roll == null ? 0 : roll.Value;
                UpdateDockingXRoll(r);
                break;
        }
    }

    public void Update()
    {
        if (dockTarget != null)
        {
            Transform playerShipTransform = gameController.GetPlayerShip().transform;

            Vector3 targetVec;
            float relv;
            Vector3 relunitvec;
            PhysicsUtils.CalcRelV(playerShipTransform.parent.transform, dockTarget, out targetVec, out relv, out relunitvec);

            //float closingDist;
            //float closingRelv;
            Vector2 planeVec;
            //PhysicsUtils.CalcDockPlanar(playerShipTransform, dockTarget, relv, relunitvec, out closingDist, out closingRelv, out planeVec);
            Vector3 dockVec = dockTarget.transform.GetChild(0).position - playerShipTransform.position;
            Vector3 planeVecX = Vector3.Project(dockVec, playerShipTransform.right);
            Vector3 planeVecY = Vector3.Project(dockVec, playerShipTransform.up);
            Vector3 planeVecZ = Vector3.Project(dockVec, playerShipTransform.forward);
            float dotX = Vector3.Dot(dockVec, playerShipTransform.right);
            float dotY = Vector3.Dot(dockVec, playerShipTransform.up);
            float dotZ = Vector3.Dot(dockVec, playerShipTransform.forward);
            planeVec.x = -Mathf.Sign(dotX) * planeVecX.magnitude;
            planeVec.y = -Mathf.Sign(dotY) * planeVecY.magnitude;
            float closingDist = Mathf.Sign(dotZ) * planeVecZ.magnitude;

            Vector3 closingRelVec = Vector3.Project(relv * relunitvec, playerShipTransform.forward);
            float dotClosingZ = Vector3.Dot(relv * relunitvec, playerShipTransform.forward);
            float closingRelV = Mathf.Sign(dotClosingZ) * closingRelVec.magnitude;

            Transform dockModel = dockTarget.transform.GetChild(0);
            Quaternion dockAlignQ = playerShipTransform.rotation * Quaternion.Inverse(dockModel.rotation);
            float dockAngleX = dockAlignQ.eulerAngles.x <= 180 ? dockAlignQ.eulerAngles.x : dockAlignQ.eulerAngles.x - 360;
            float dockAngleY = dockAlignQ.eulerAngles.y <= 180 ? dockAlignQ.eulerAngles.y : dockAlignQ.eulerAngles.y - 360;
            float dockAngleZ = dockAlignQ.eulerAngles.z <= 180 ? dockAlignQ.eulerAngles.z : dockAlignQ.eulerAngles.z - 360;

            gameController.InputControl().PropertyChanged("ClosingDistText", DisplayUtils.DistanceText(closingDist));
            gameController.InputControl().PropertyChanged("ClosingVText", DisplayUtils.RelvText(closingRelV));
            gameController.InputControl().PropertyChanged("ClosingTimeText", DisplayUtils.TimeToTargetText(closingDist, closingRelV));
            gameController.InputControl().PropertyChanged("DockAnglePitchText", DisplayUtils.DegreeText(dockAngleZ));
            gameController.InputControl().PropertyChanged("DockAngleRollText", DisplayUtils.DegreeText(dockAngleY));
            gameController.InputControl().PropertyChanged("DockAngleYawText", DisplayUtils.DegreeText(dockAngleX));
            //gameController.InputControl().PropertyChanged("DockAngleColor", DisplayUtils.ColorValueBetween(dockAngle, warnThreshold, badThreshold));
            //gameController.InputControl().PropertyChanged("DockAngleText", DisplayUtils.Angle3Text(dockAngleZ, dockAngleY, dockAngleX));
            //gameController.InputControl().PropertyChanged("DockAngleText", DisplayUtils.QText(dockAlignQ));
            gameController.InputControl().PropertyChanged("DockingX", planeVec);
            gameController.InputControl().PropertyChanged("DockingXRoll", dockAngleY);
        }
    }

    private void SyncDockTarget()
    {
        int tgtVal = DockTargetSelectorDropdown.value - 1;
        if (tgtVal >= 0 && tgtVal < dockTargets.Count)
        {
            dockTarget = dockTargets[tgtVal];
        }
        else
        {
            dockTarget = null;
        }
    }

    private void DockTargetSelectorDropdownOnValueChanged()
    {
        SyncDockTarget();
    }

    private void UpdateDockingX(Vector2 planeVec)
    {
        Vector2 planeVecN = planeVec.normalized;
        float r = MapWorldToMFDDockR(planeVec.magnitude);
        float x = r * planeVecN.x;
        float y = r * planeVecN.y;
        DockingX.anchoredPosition = new Vector2(x, y);
    }
    private void UpdateDockingXRoll(float roll)
    {
        DockingX.rotation = Quaternion.Euler(DockingX.rotation.x, DockingX.rotation.y, roll);
    }

    private float innerRadius = 25f;
    private float outerRadius = 100f;
    // 0 - 0 - nan
    // 1 - 25 - 0.1
    // 10 - 50 - 1
    // 100 - 75 - 2
    // 1000 - 100 - 3
    private float MapWorldToMFDDockR(float x)
    {
        float y;
        if (x <= 1)
        {
            y = x * innerRadius;
        }
        else if (x <= 1000)
        {
            y = innerRadius + Mathf.Log10(x) * innerRadius;
        }
        else
        {
            y = outerRadius;
        }
        return y;
    }

}

