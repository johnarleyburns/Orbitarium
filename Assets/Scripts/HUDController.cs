using UnityEngine;
using Greyman;
using System.Collections.Generic;

public class HUDController : MonoBehaviour
{

    private GameController gameController;
    private InputController inputController;
    private OffScreenIndicator OffscreenIndicator;
    private GameObject referenceBody = null;
    private GameObject selectedTarget = null;
    private int selectedTargetIndex = -1;
    private static int HUD_INDICATOR_DIDYMOS = 0;
    private static int HUD_INDICATOR_RELV_PRO = 1;
    private static int HUD_INDICATOR_RELV_RETR = 2;
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

    public GameObject GetReferenceBody()
    {
        return referenceBody;
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

    public void SelectNextTarget()
    {
        SelectNextTarget(1);
    }

    public void UpdateHUD()
    {
        if (gameController != null && gameController.GetPlayer() != null)
        {
            for (int i = 0; i < gameController.TargetCount(); i++)
            {
                GameObject target = gameController.GetTarget(i);
                int indicatorId = targetIndicatorId[target];
                UpdateTargetIndicator(indicatorId, target);
            }
        }
    }

    private void UpdateTargetIndicator(int indicatorId, GameObject target)
    {
        bool hasText = OffscreenIndicator.indicators[indicatorId].hasOnScreenText;
        bool isRefBody = target == referenceBody;
        bool isSelectedTarget = target == selectedTarget;
        bool calcRelV = hasText || isRefBody || isSelectedTarget;
        if (calcRelV)
        {
            float targetDist;
            float targetRelV;
            Vector3 targetRelVUnitVec;
            PhysicsUtils.CalcRelV(gameController.GetPlayer().transform, target, out targetDist, out targetRelV, out targetRelVUnitVec);
            if (hasText)
            {
                UpdateTargetDistance(indicatorId, target.name, targetDist);
            }
            if (isRefBody)
            {
                UpdateRelativeVelocityIndicators(targetRelVUnitVec);
            }
            if (isSelectedTarget)
            {
                UpdateSelectedTargetIndicator(targetRelV);
            }
        }
    }

    public void UpdateTargetDistance(int indicatorId, string name, float dist)
    {
        string targetString = string.Format("{0}\n{1:0,0} m", name, dist);
        OffscreenIndicator.UpdateIndicatorText(indicatorId, targetString);
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

    private void UpdateSelectedTargetIndicator(float relV)
    {
        inputController.TargetDirectionIndicator.transform.position = selectedTarget.transform.position;
        if (OffscreenIndicator.indicators[HUD_INDICATOR_TARGET_DIRECTION].hasOnScreenText)
        {
            string targetString = string.Format("{0:0,0.0} m/s", relV);
            OffscreenIndicator.UpdateIndicatorText(HUD_INDICATOR_TARGET_DIRECTION, targetString);
        }
    }

    public void SelectNextTarget(int offset)
    {
        // prefer enemy targets
        if (gameController.EnemyCount() > 0)
        {
            selectedTargetIndex = (selectedTargetIndex + offset) % gameController.EnemyCount();
            if (selectedTargetIndex < 0)
            {
                selectedTargetIndex = gameController.EnemyCount() + selectedTargetIndex;
            }
            selectedTarget = gameController.GetEnemy(selectedTargetIndex);
            inputController.TargetToggleText.text = selectedTarget.name;
            ShowTargetIndicator();
            SelectNextReferenceBody(selectedTarget);
        }
        else if (gameController.TargetCount() > 0)
        {
            if (selectedTargetIndex < 0)
            {
                selectedTargetIndex = gameController.TargetCount() + selectedTargetIndex;
            }
            else
            {
                selectedTargetIndex = (selectedTargetIndex + offset) % gameController.TargetCount();
            }
            selectedTarget = gameController.GetTarget(selectedTargetIndex);
            inputController.TargetToggleText.text = selectedTarget.name;
            ShowTargetIndicator();
            SelectNextReferenceBody(selectedTarget);
        }
        else
        {
            selectedTargetIndex = -1;
            selectedTarget = null;
            inputController.TargetToggleText.text = "NONE";
            HideTargetIndicator();
            SelectNextReferenceBody();
        }
    }

    public void SelectNextReferenceBody(GameObject specificTarget = null)
    {
        if (specificTarget != null)
        {
            referenceBody = specificTarget;
        }
        else if (gameController.TargetCount() > 0)
        {
            referenceBody = gameController.GetTarget(gameController.TargetCount() - 1);
        }
        else
        {
            referenceBody = null;
        }
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
        selectedTargetIndex = -1;
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
