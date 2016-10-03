using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(BinaryPair), true)]
public class BinaryPairEditor : EllipseBaseEditor {

	public override void OnInspectorGUI()
	{
		GUI.changed = false;
		BinaryPair bPair = (BinaryPair) target;
		Vector3 velocity = Vector3.zero;

		velocity = EditorGUILayout.Vector3Field(new GUIContent("Velocity", "velocity of binary center of mass"), bPair.velocity);


		if (GUI.changed) {
			Undo.RecordObject(bPair, "EllipseBase Change");
			bPair.velocity = velocity;
			EditorUtility.SetDirty(bPair);
		}	
		base.OnInspectorGUI();
	
	}
}
