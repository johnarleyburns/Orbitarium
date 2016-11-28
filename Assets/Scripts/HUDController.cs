using UnityEngine;
using Greyman;
using System.Collections.Generic;

public class HUDController : MonoBehaviour, IPropertyChangeObserver
{

    private GameController gameController;
    private InputController inputController;
    private OffScreenIndicator OffscreenIndicator;
    private GameObject selectedTarget = null;
    private int selectedTargetType = TARGET_TYPE_ALL;
    private static int TARGET_TYPE_ALL = 0;
    private static int TARGET_TYPE_ENEMY_SHIP = 1;
    private static int HUD_INDICATOR_DIDYMOS = 0;
    private static int HUD_INDICATOR_DIDYMOON = 3;
    private static int HUD_INDICATOR_ENEMY_SHIP_TEMPLATE = 4;
    private static int HUD_INDICATOR_TARGET_DIRECTION = 5;
    private static float RelativeVelocityIndicatorScale = 1000;
    private Dictionary<GameObject, int> targetIndicatorId = new Dictionary<GameObject, int>();

    // Use this for initialization
    void Awake()
    {
        gameController = GetComponent<GameController>();
        inputController = GetComponent<InputController>();
        OffscreenIndicator = inputController.HUDLogic.GetComponent<Greyman.OffScreenIndicator>();
        inputController.AddObserver("SelectTargetFromDropdown", this);
    }

    // Update is called once per frame
    void Update()
    {
        switch (gameController.GetGameState())
        {
            case GameController.GameState.RUNNING:
                UpdateHUD();
                break;
        }
    }

    public void PropertyChanged(string name, object value)
    {
        switch (name)
        {
            case "SelectTargetFromDropdown":
                int? valueP = value as int?;
                int index = valueP == null ? -1 : valueP.Value;
                SelectTargetFromDropdown(index);
                break;
        }
    }

    public GameObject GetSelectedTarget()
    {
        return selectedTarget;
    }

    public void HideTargetIndicator()
    {
        inputController.TargetDirectionIndicator.SetActive(true);
        OffscreenIndicator.indicators[HUD_INDICATOR_TARGET_DIRECTION].showOnScreen = false;
        OffscreenIndicator.indicators[HUD_INDICATOR_TARGET_DIRECTION].showOffScreen = false;
    }

    public void ShowTargetIndicator()
    {
        OffscreenIndicator.indicators[HUD_INDICATOR_TARGET_DIRECTION].showOnScreen = true;
        OffscreenIndicator.indicators[HUD_INDICATOR_TARGET_DIRECTION].showOffScreen = true;
    }

    public void UpdateHUD()
    {
        if (gameController != null && gameController.GetPlayer() != null)
        {
            int i = 0;
            foreach (GameObject target in gameController.TargetData().GetAllTargets())
            {
                int indicatorId = targetIndicatorId[target];
                UpdateTargetIndicator(indicatorId, target);
                i++;
            }
        }
    }

    private void UpdateTargetIndicator(int indicatorId, GameObject target)
    {
        bool isSelectedTarget = target == selectedTarget;
        bool calcRelV = target.GetComponent<NBody>() != null;
        if (calcRelV)
        {
            float targetDist;
            float targetRelV;
            Vector3 targetRelVUnitVec;
            PhysicsUtils.CalcRelV(gameController.GetPlayer().transform, target, out targetDist, out targetRelV, out targetRelVUnitVec);
            if (isSelectedTarget)
            {
                UpdateSelectedTargetIndicator(targetDist, targetRelV);
                UpdateRelativeVelocityIndicators(targetRelVUnitVec);
            }
            UpdateTargetDistance(indicatorId, target.name, targetDist);
        }
    }

    public void UpdateTargetDistance(int indicatorId, string name, float dist)
    {
        string targetString = string.Format("{0}\n{1}", name, DistanceText(dist));
        OffscreenIndicator.UpdateIndicatorText(indicatorId, targetString);
    }

    private void UpdateSelectedTargetIndicator(float dist, float relV)
    {
        string distText = DistanceText(dist);
        string relvText = RelvText(relV);
        string timeToTargetText = TimeToTargetText(dist, relV);
        inputController.TargetDirectionIndicator.transform.position = selectedTarget.transform.position;
        if (OffscreenIndicator.indicators[HUD_INDICATOR_TARGET_DIRECTION].hasOnScreenText)
        {
            string targetString = string.Format("{0}\n{1}", relvText, timeToTargetText);
            OffscreenIndicator.UpdateIndicatorText(HUD_INDICATOR_TARGET_DIRECTION, targetString);
        }
        inputController.PropertyChanged("DistanceText", distText);
        inputController.PropertyChanged("RelvText", relvText);
        inputController.PropertyChanged("TimeToTargetText", timeToTargetText);
    }

    private string TimeToTargetText(float dist, float relv)
    {
        string timeToTargetText;
        if (relv <= 0)
        {
            timeToTargetText = "Inf";
        }
        else
        {
            float sec = dist / relv;
            timeToTargetText = string.Format("{0:,0} s", sec);
        }
        return timeToTargetText;
    }

    private string DistanceText(float dist)
    {
        float adist = Mathf.Abs(dist);
        string distText = adist > 100000 ? string.Format("{0:,0} km", dist / 1000)
            : (adist > 10000 ? string.Format("{0:,0.0} km", dist / 1000)
            : (adist > 1000 ? string.Format("{0:,0.00} km", dist / 1000)
                : (adist > 100 ? string.Format("{0:,0} m", dist)
                : string.Format("{0:,0.0} m", dist))));
        return distText;
    }

    private string RelvText(float relV)
    {
        float arelV = Mathf.Abs(relV);
        string relvText = arelV > 10000 ? string.Format("{0:,0} km/s", relV / 1000)
            : (arelV > 1000 ? string.Format("{0:,0.0} m/s", relV / 1000)
            : (arelV > 100 ? string.Format("{0:,0} m/s", relV)
            : (arelV > 10 ? string.Format("{0:,0} m/s", relV)
            : (arelV > 1 ? string.Format("{0:,0.0} m/s", relV)
            : string.Format("{0:,0.00} m/s", relV)))));
        return relvText;
    }

    public void UpdateRelativeVelocityIndicators(Vector3 relVUnitVec)
    {
        Vector3 relVIndicatorScaled = RelVIndicatorScaled(relVUnitVec);
        Vector3 myPos = gameController.GetPlayer().transform.position;
        Quaternion rotNormalPlus = Quaternion.Euler(0, 90, 0);
        Quaternion rotNormalMinus = Quaternion.Euler(0, -90, 0);
        inputController.RelativeVelocityDirectionIndicator.transform.position = myPos + relVIndicatorScaled;
        inputController.RelativeVelocityAntiDirectionIndicator.transform.position = myPos + -relVIndicatorScaled;
        inputController.RelativeVelocityNormalPlusDirectionIndicator.transform.position = myPos + rotNormalPlus * relVIndicatorScaled;
        inputController.RelativeVelocityNormalMinusDirectionIndicator.transform.position = myPos + rotNormalMinus * relVIndicatorScaled;
    }

    private void SelectTarget(GameObject target)
    {
        selectedTarget = target;
        if (target != null)
        {
            ShowTargetIndicator();
        }
        else {
            HideTargetIndicator();
        }
        int i = gameController.TargetData().GetTargetIndex(target);
        inputController.PropertyChanged("SelectTarget", i);
        UpdateTargetType();
    }

    private void UpdateTargetType()
    {
        selectedTargetType = TARGET_TYPE_ALL;
        if (selectedTarget != null)
        {
            TargetDB.TargetType t = gameController.TargetData().GetTargetType(selectedTarget);
            if (t == TargetDB.TargetType.ENEMY_BASE || t == TargetDB.TargetType.ENEMY_SHIP)
            {
                selectedTargetType = TARGET_TYPE_ENEMY_SHIP;
            }
        }
        inputController.PropertyChanged("SelectedTargetType", selectedTargetType);
    }
    
    public void SelectNextTargetPreferClosestEnemy()
    {
        GameObject target = gameController.ClosestTarget(TargetDB.TargetType.ENEMY_SHIP);
        if (target == null)
        {
            target = gameController.ClosestTarget(TargetDB.TargetType.ENEMY_BASE);
        }
        if (target == null)
        {
            target = gameController.ClosestTarget(TargetDB.TargetType.FRIEND_BASE);
        }
        if (target == null)
        {
            target = gameController.ClosestTarget();
        }
        SelectTarget(target);
    }

    public void SelectTargetFromDropdown(int index)
    {
        GameObject target = gameController.TargetData().GetTargetAtIndex(index);
        SelectTarget(target);
    }

    public void AddEnemyIndicator(GameObject enemyShip)
    {
        int newIndicatorId = OffscreenIndicator.AddNewIndicatorFromClone(HUD_INDICATOR_ENEMY_SHIP_TEMPLATE, enemyShip.name);
        OffscreenIndicator.AddIndicator(enemyShip.transform, newIndicatorId);
        targetIndicatorId[enemyShip] = newIndicatorId;
    }

    public void AddPlanetaryObjectIndicators(GameObject didymos, GameObject didymoon)
    {
        targetIndicatorId[didymos] = HUD_INDICATOR_DIDYMOS;
        targetIndicatorId[didymoon] = HUD_INDICATOR_DIDYMOON;
    }

    public void RemoveIndicators()
    {
        OffscreenIndicator.RemoveIndicators();
    }

    public void AddTargetIndicator(GameObject target, int indicatorId)
    {
        targetIndicatorId[target] = indicatorId;
    }

    public void AddFixedIndicators()
    {
        OffscreenIndicator.AddFixedIndicators();
    }

    public void RemoveIndicator(Transform transform)
    {
        OffscreenIndicator.RemoveIndicator(transform);
    }

    public void ClearTargetIndicators()
    {
        selectedTarget = null;
        foreach (GameObject target in targetIndicatorId.Keys)
        {
            OffscreenIndicator.RemoveIndicator(target.transform);
        }
        targetIndicatorId.Clear();
    }

    public GameObject SelectedTarget()
    {
        return selectedTarget;
    }

    private static Vector3 RelVIndicatorScaled(Vector3 relVelUnit)
    {
        return RelativeVelocityIndicatorScale * relVelUnit;
    }

}
