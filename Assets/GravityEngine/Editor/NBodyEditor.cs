using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(NBody), true)]
public class NBodyEditor : Editor {


	private static string mTip = "Mass. Engine uses units in which G=1";
	private static string velTip = "Velocity component of the object.";
	private static string autoTip = "Determine particle capture radius using size of mesh filter child (typically a sphere)";
	private static string sizeTip = "Radius within which particles will be captured and removed from the scene.";

	public override void OnInspectorGUI()
	{
		GUI.changed = false;

		NBody nbody = (NBody) target;
		float mass = 0f;
		float size = 0.1f;
		bool autoSize = true;
		Vector3 velocity = Vector3.zero; 

		mass = EditorGUILayout.FloatField(new GUIContent("Mass", mTip), (float) nbody.mass);

		// If the velocity is controlled by an EllipseBase, or this NBody is the direct child of
		// BinaryPair or ThreeeBodySolution then don't allowit to be controlled. 
		string controlledBy = null;
		if (nbody.transform.gameObject.GetComponent<OrbitEllipse>() != null) {
			controlledBy = "Velocity is set by ellipse parameters.";
		} else if (nbody.transform.gameObject.GetComponent<OrbitHyper>() != null) {
			controlledBy = "Velocity is set by hyperbola parameters.";
		} else if (nbody.transform.parent != null) {
			if (nbody.transform.parent.gameObject.GetComponent<BinaryPair>() != null) {
				controlledBy = "Velocity is set by BinaryPair parent.";
			} else if (nbody.transform.parent.gameObject.GetComponent<ThreeBodySolution>() != null) {
				controlledBy = "Velocity is set by ThreeBodySolution parent.";
			}
		}
		if (controlledBy == null) {
			velocity = EditorGUILayout.Vector3Field(new GUIContent("Velocity", velTip), nbody.vel);
		} else {
			EditorGUILayout.LabelField(controlledBy, EditorStyles.boldLabel);
			//EditorGUILayout.LabelField(string.Format("vel= {0:F2} {1:F2} {2:F2}", nbody.vel.x, nbody.vel.y, nbody.vel.z));
		}
		// particle capture size
		EditorGUIUtility.labelWidth = 200f;
		EditorGUIUtility.fieldWidth = 20f;
		autoSize = EditorGUILayout.Toggle(new GUIContent("Automatic particle capture size", autoTip), nbody.automaticParticleCapture);
		EditorGUIUtility.labelWidth = 0;
		EditorGUIUtility.fieldWidth = 0;
		if (!autoSize) {
			EditorGUIUtility.labelWidth = 200f;
			EditorGUIUtility.fieldWidth = 40f;
			size = EditorGUILayout.FloatField(new GUIContent("Particle capture radius", sizeTip), (float) nbody.size);
			EditorGUIUtility.labelWidth = 0;
			EditorGUIUtility.fieldWidth = 0;
		} else {
			float detectedSize = nbody.CalculateSize();
			if (detectedSize < 0) {
				EditorGUILayout.LabelField("Did not detect a child with a MeshFilter.", EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Using size=" + size);
			} else {
				EditorGUILayout.LabelField("Particle Capture radius=" + detectedSize);
				size = detectedSize;
			}
		}
		if (mass < 0)
			mass = 0; 

		if (GUI.changed) {
			Undo.RecordObject(nbody, "NBody Change");
			nbody.mass = FixNaN.FixIfNaN(mass);
			nbody.vel = FixNaN.FixIfNaN(velocity);
			nbody.size = size;
			nbody.automaticParticleCapture = autoSize;
			EditorUtility.SetDirty(nbody);
		}

	}
}
