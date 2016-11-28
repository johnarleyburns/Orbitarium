using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TargetDB : MonoBehaviour {

    public GameController gameController;

    private List<GameObject> targetOrder = new List<GameObject>();
    private Dictionary<GameObject, TargetType> targets = new Dictionary<GameObject, TargetType>();

    public enum TargetType
    {
        UNKNOWN,
        PLANET,
        ASTEROID,
        MOON,
        FRIEND_SHIP,
        FRIEND_BASE,
        ENEMY_SHIP,
        ENEMY_BASE
    }

    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void AddTarget(GameObject target, TargetType targetType)
    {
        targets[target] = targetType;
        if (!targetOrder.Contains(target))
        {
            targetOrder.Add(target);
        }
        gameController.InputControl().PropertyChanged("TargetList", targetOrder);
    }

    public void RemoveTarget(GameObject target)
    {
        targetOrder.Remove(target);
        targets.Remove(target);
        gameController.InputControl().PropertyChanged("TargetList", targetOrder);
    }

    public void ClearTargets()
    {
        targetOrder.Clear();
        targets.Clear();
        gameController.InputControl().PropertyChanged("TargetList", targetOrder);
    }

    public TargetType GetTargetType(GameObject target)
    {
        TargetType t;
        if (!targets.TryGetValue(target, out t))
        {
            t = TargetType.UNKNOWN;
        }
        return t;
    }

    public IEnumerable<GameObject> GetTargets(TargetType targetType)
    {
        return targetOrder.Where(x => targets[x] == targetType);
    }

    public GameObject GetTargetAtIndex(int i)
    {
        return i >= 0 && i < targetOrder.Count ? targetOrder[i] : null;
    }

    public int GetTargetIndex(GameObject target)
    {
        return targetOrder.IndexOf(target);
    }

    public IEnumerable<GameObject> GetAllTargets()
    {
        return targetOrder;
    }

}
