using UnityEngine;
using Greyman;
using System.Collections.Generic;

public class HUDController : MonoBehaviour, IPropertyChangeObserver
{

    private GameController gameController;
    private InputController inputController;
    private OffScreenIndicator OffscreenIndicator;
    private GameObject selectedTarget = null;
    private static int HUD_INDICATOR_NONTHREAT_TEMPLATE = 0;
    private static int HUD_INDICATOR_ENEMY_SHIP_TEMPLATE = 3;
    private static int HUD_INDICATOR_TARGET_DIRECTION = 4;
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
                UpdateTargetIndicator(target);
                i++;
            }
        }
    }

    private void UpdateTargetIndicator(GameObject target)
    {
        int indicatorId = targetIndicatorId[target];
        bool isSelectedTarget = target == selectedTarget;
        TargetDB.TargetType targetType = gameController.TargetData().GetTargetType(target);
        GameObject targetNBody = NUtils.GetNBodyGameObject(target);
        bool calcRelV = targetNBody != null;
        if (calcRelV)
        {
            Vector3 targetVec;
            float targetRelV;
            Vector3 targetRelVUnitVec;
            PhysicsUtils.CalcRelV(gameController.GetPlayer(), target, out targetVec, out targetRelV, out targetRelVUnitVec);
            float distToCenter = targetVec.magnitude;
            float radius = gameController.TargetData().GetTargetRadius(target);
            float targetDist = distToCenter - radius;
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
        string targetString = string.Format("{0}\n{1}", name, DisplayUtils.DistanceText(dist));
        OffscreenIndicator.UpdateIndicatorText(indicatorId, targetString);
    }

    private void UpdateSelectedTargetIndicator(float dist, float relV)
    {
        string distText = DisplayUtils.DistanceText(dist);
        string relvText = DisplayUtils.RelvText(relV);
        string timeToTargetText = DisplayUtils.TimeToTargetText(dist, relV);
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

    public void UpdateRelativeVelocityIndicators(Vector3 relVUnitVec)
    {
        Vector3 p = gameController.GetPlayerShip().transform.position;
        Vector3 a = relVUnitVec;
        Vector3 b = gameController.GetPlayerShip().transform.up;
        Vector3 nmlPos = -Vector3.Cross(a, b).normalized; // left handed coordinate system
        inputController.RelativeVelocityDirectionIndicator.transform.position = p + RelVIndicatorScaled(a);
        inputController.RelativeVelocityAntiDirectionIndicator.transform.position = p + RelVIndicatorScaled(-a);
        inputController.RelativeVelocityNormalPlusDirectionIndicator.transform.position = p + RelVIndicatorScaled(nmlPos);
        inputController.RelativeVelocityNormalMinusDirectionIndicator.transform.position = p + RelVIndicatorScaled(-nmlPos);
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
        TargetDB.TargetType t = gameController.TargetData().GetTargetType(target);
        int i = gameController.TargetData().GetTargetIndex(target);
        int j = gameController.TargetData().TargetTypeIndex(t);
        UpdateTargetIndicator(target);
        inputController.PropertyChanged("SelectTarget", i);
        inputController.PropertyChanged("SelectedTargetType", j);
    }
    
    public void SelectNextTargetPreferClosestEnemy()
    {
        GameObject target = gameController.NextClosestTarget(selectedTarget, TargetDB.TargetType.ENEMY);
        if (target == null)
        {
            target = gameController.NextClosestTarget(selectedTarget, TargetDB.TargetType.FRIEND);
        }
        if (target == null)
        {
            target = gameController.NextClosestTarget(selectedTarget, TargetDB.TargetType.DOCK);
        }
        if (target == null)
        {
            target = gameController.NextClosestTarget(selectedTarget, TargetDB.TargetType.MOON);
        }
        if (target == null)
        {
            target = gameController.NextClosestTarget(selectedTarget, TargetDB.TargetType.ASTEROID);
        }
        if (target == null)
        {
            target = gameController.NextClosestTarget(selectedTarget, TargetDB.TargetType.PLANET);
        }
        if (target == null)
        {
            target = gameController.NextClosestTarget(selectedTarget);
        }
        SelectTarget(target);
    }

    public void SelectTargetFromDropdown(int index)
    {
        GameObject target = gameController.TargetData().GetTargetAtIndex(index);
        SelectTarget(target);
    }

    public void AddFixedIndicators()
    {
        OffscreenIndicator.AddFixedIndicators(); // relative direction and target highlight vectors
    }

    public void AddTargetIndicator(GameObject target)
    {
        TargetDB.TargetType t = gameController.TargetData().GetTargetType(target);
        int indicatorTemplate;
        switch (t)
        {
            default:
            case TargetDB.TargetType.PLANET:
            case TargetDB.TargetType.ASTEROID:
            case TargetDB.TargetType.MOON:
                indicatorTemplate = HUD_INDICATOR_NONTHREAT_TEMPLATE;
                break;
            case TargetDB.TargetType.ENEMY:
                indicatorTemplate = HUD_INDICATOR_ENEMY_SHIP_TEMPLATE;
                break;
            case TargetDB.TargetType.FRIEND:
                indicatorTemplate = HUD_INDICATOR_NONTHREAT_TEMPLATE;
                break;
        }
        int newIndicatorId = OffscreenIndicator.AddNewIndicatorFromClone(indicatorTemplate, target.name);
        OffscreenIndicator.AddIndicator(target.transform, newIndicatorId);
        targetIndicatorId[target] = newIndicatorId;
    }

    public void RemoveIndicators()
    {
        OffscreenIndicator.RemoveIndicators();
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
