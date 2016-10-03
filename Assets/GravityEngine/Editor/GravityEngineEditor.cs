using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(GravityEngine), true)]
public class GravityEngineEditor : Editor {


	private const string mTip = "Scale applied to all masses in the scene. Increasing will result in larger forces and faster evolution.";
	private const string algoTip = "Integration algorithm used to evolve massive bodies.\nLeapfrog is default. AZTTriple is for exactly three massive bodies.";
	private const string timeTip = "Timescale controls the difference between game time and physics time in Gravity Engine. Larger values result in faster evolution BUT more calculations being performed. ";
	private const string mlessTip = "Evolve massless bodies seperately using a built-in Leapfrog algorithm.";
	private const string ppTip = "Particle accuracy (0=best performance/lower accuracy, 1 = highest accuracy/most CPU)";
	private const string autoStartTip = "Begin evolving bodies as soon as the scene starts. If false then starting is via script API.";
	private const string autoAddTip = "Automatically detect Nbodies in the scene and add them to the Gravity Engine.";
	private const string phyWTip = "Scale factor applied to physics position to scale up to world view. Typically 1 unless initializing from known solutions via scripts (e.g. ThreeBodySolution)";

	private const string stepTip = "Number of integration steps gravity engine will target for each 60fps frame. " + 
				"The value depends in how much accuracy is desired and the masses in the scene. Larger values may impact the frame rate.\n" +
				"Default value is 8.";

	private const string pstepTip = "Number of particle integration steps gravity engine will target for each 60fps frame. " + 
				"The value depends in how much accuracy is desired and the masses in the scene." +
				" Larger values can slow the simulation significantly if many particles are used.\n" + 
				"Default value is 2.";

	[MenuItem("GameObject/3D Object/GravityEngine")]
	static void Init()
    {
    	if (FindObjectOfType(typeof(GravityEngine))) {
    		Debug.LogWarning("NBodyEngine already in scene.");
    		return;
    	}
		GameObject nbodyEngine = new GameObject();
		nbodyEngine.name = "GravityEngine";
		nbodyEngine.AddComponent<GravityEngine>();
    }

	public override void OnInspectorGUI()
	{
		GUI.changed = false;
		GravityEngine gravityEngine = (GravityEngine) target;
		float massScale = gravityEngine.massScale; 
		float timeScale = gravityEngine.timeScale; 
		float physToWorldFactor = gravityEngine.physToWorldFactor; 

		bool optimizeMassless = gravityEngine.optimizeMassless;
		bool detectNbodies = gravityEngine.detectNbodies;
		bool evolveAtStart = gravityEngine.evolveAtStart;
		bool scaling = gravityEngine.editorShowScale;
		bool showAdvanced = gravityEngine.editorShowAdvanced;
		bool cmFoldout = gravityEngine.editorCMfoldout;

		int stepsPerFrame = gravityEngine.stepsPerFrame;
		int particleStepsPerFrame = gravityEngine.particleStepsPerFrame;

		GravityEngine.Algorithm algorithm = GravityEngine.Algorithm.LEAPFROG;

		EditorGUIUtility.labelWidth = 200;

		scaling = EditorGUILayout.Foldout(scaling, "Scaling");
		if (scaling) {
			massScale = EditorGUILayout.FloatField(new GUIContent("Mass Scale", mTip), gravityEngine.massScale);
			timeScale = EditorGUILayout.FloatField(new GUIContent("Time Scale", timeTip), gravityEngine.timeScale);
		}

		showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced");
		if (showAdvanced) {
			algorithm =
			(GravityEngine.Algorithm)EditorGUILayout.EnumPopup(new GUIContent("Algorithm", algoTip), gravityEngine.algorithm);
			optimizeMassless = EditorGUILayout.Toggle(new GUIContent("Optimize Massless Bodies", mlessTip), 
					gravityEngine.optimizeMassless);
			detectNbodies = EditorGUILayout.Toggle(new GUIContent("Automatically Add NBody objects", autoAddTip), gravityEngine.detectNbodies);
			evolveAtStart = EditorGUILayout.Toggle(new GUIContent("Evolve at Start", autoStartTip), gravityEngine.evolveAtStart);
			physToWorldFactor = EditorGUILayout.FloatField(new GUIContent("Physics to World Scale", phyWTip), gravityEngine.physToWorldFactor);

			stepsPerFrame = EditorGUILayout.IntField(new GUIContent("Physics steps per frame", stepTip), stepsPerFrame);
			particleStepsPerFrame = EditorGUILayout.IntField(new GUIContent("Physics (particles) steps per frame", pstepTip), particleStepsPerFrame);
		}
		// Switch bodies list on/off based on option 
		if (!gravityEngine.detectNbodies) {
			// use native Inspector look & feel for bodies object
			EditorGUILayout.LabelField("Control Nbody in following gameObjects (and children)", EditorStyles.boldLabel);
         	SerializedProperty bodiesProp = serializedObject.FindProperty ("bodies");
         	EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(bodiesProp, true);
         	if(EditorGUI.EndChangeCheck())
             	serializedObject.ApplyModifiedProperties();
		} else {
			EditorGUILayout.LabelField("NBody objects will be detected automatically", EditorStyles.boldLabel);
		}
		// Show the CM and the velocity of the CM
		cmFoldout = EditorGUILayout.Foldout(cmFoldout, "Center of Mass Info");
		if (cmFoldout) {
			EditorGUILayout.LabelField("Center of Mass:" + gravityEngine.GetWorldCenterOfMass());
			EditorGUILayout.LabelField("CM Velocity:" + gravityEngine.GetWorldCenterOfMassVelocity());
		}

		if (GUI.changed) {
			Undo.RecordObject(gravityEngine, "GE Change");
			gravityEngine.timeScale = timeScale; 
			gravityEngine.massScale = massScale; 
			gravityEngine.physToWorldFactor = physToWorldFactor; 
			gravityEngine.optimizeMassless = optimizeMassless; 
			gravityEngine.detectNbodies = detectNbodies;
			gravityEngine.algorithm = algorithm;
			gravityEngine.evolveAtStart = evolveAtStart;
			gravityEngine.editorShowScale = scaling; 
			gravityEngine.editorShowAdvanced = showAdvanced;
			gravityEngine.editorCMfoldout = cmFoldout;
			gravityEngine.stepsPerFrame = stepsPerFrame;
			gravityEngine.particleStepsPerFrame = particleStepsPerFrame;
			EditorUtility.SetDirty(gravityEngine);
		}
	}
}
