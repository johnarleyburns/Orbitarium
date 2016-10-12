using UnityEngine;
using System.Collections;

[AddComponentMenu("Utility/Auto spin object")]

public class AutoSpinObject : MonoBehaviour
{	
	public Vector3 spinVector = new Vector3 (0, 1, 0);
	private Transform myTransform;
	public float speed = 100f;
	public int direction = 1;
	
	void Start ()
	{
		myTransform = transform;	
	}
	
	void Update ()
	{
		myTransform.Rotate (direction * speed * spinVector * Time.deltaTime);	
	}
}
