using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TargetDB : MonoBehaviour {

    public GameController gameController;

    private List<GameObject> targetOrder = new List<GameObject>();
    private Dictionary<GameObject, TargetType> targets = new Dictionary<GameObject, TargetType>();
    private Dictionary<GameObject, float> targetRadius = new Dictionary<GameObject, float>();

    public enum TargetType
    {
        UNKNOWN,
        PLANET,
        ASTEROID,
        MOON,
        FRIEND,
        ENEMY,
        APPROACH,
        DOCK
    }

    public int TargetTypeIndex(TargetType t)
    {
        return (int)t;
    }

    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    public void AddTarget(GameObject target, TargetType targetType)
    {
        if (target != null)
        {
            NBodyDimensions dim = target.GetComponent<NBodyDimensions>();
            float targetRadiusM = dim != null ? dim.RadiusM : 0f;
            targets[target] = targetType;
            targetRadius[target] = targetRadiusM;
            if (!targetOrder.Contains(target))
            {
                targetOrder.Add(target);
            }
            gameController.InputControl().PropertyChanged("TargetList", targetOrder);

        }
    }

    public void RemoveTarget(GameObject target)
    {
        if (target != null)
        {
            targetOrder.Remove(target);
            targetRadius.Remove(target);
            targets.Remove(target);
            gameController.InputControl().PropertyChanged("TargetList", targetOrder);
        }
    }

    public void ClearTargets()
    {
        targetOrder.Clear();
        targetRadius.Clear();
        targets.Clear();
        gameController.InputControl().PropertyChanged("TargetList", targetOrder);
    }

    public TargetType GetTargetType(GameObject target)
    {
        TargetType t;
        if (target == null || !targets.TryGetValue(target, out t))
        {
            t = TargetType.UNKNOWN;
        }
        return t;
    }

    public float GetTargetRadius(GameObject target)
    {
        float r;
        if (target == null || !targetRadius.TryGetValue(target, out r))
        {
            r = 0;
        }
        return r;
    }

    public IEnumerable<GameObject> GetTargets(TargetType targetType)
    {
        return targetOrder.Where(x => targets[x] == targetType);
    }

    public GameObject GetTargetAtIndex(int i)
    {
        return i >= 0 && i < targetOrder.Count ? targetOrder[i] : null;
    }

    public bool HasTarget(GameObject target)
    {
        return target != null && targetOrder.IndexOf(target) != -1;
    }

    public int GetTargetIndex(GameObject target)
    {
        int i;
        if (target != null)
        {
            i = targetOrder.IndexOf(target);
        }
        else
        {
            i = -1;
        }
        return i;
    }

    public IEnumerable<GameObject> GetAllTargets()
    {
        return targetOrder;
    }

}
