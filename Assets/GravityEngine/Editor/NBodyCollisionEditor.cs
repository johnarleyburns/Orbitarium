﻿using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(NBodyCollision), true)]
public class NBodyCollisionEditor : Editor {

	private static string collTip = "Select the type of collision response. (write more)"; 
	private static string bTip = "Fraction of momentum conserved in the bounce. 1 = lossless bounce"; 
	private static string cTip = "Precedence for cases where both objects have NBodyCollision attached (lower value has precedence)";
    private static string dTip = "Minimum relative velocity between objects for explosion, otherwise bounce will occur";
	public override void OnInspectorGUI()
	{
		GUI.changed = false;

		NBodyCollision nbc = (NBodyCollision) target;
		GameObject explodePF = nbc.explosionPrefab;
		int precedence = nbc.collisionPrecedence;
        float minRelVtoExplode = nbc.minRelVtoExplode;
		float bounceFactor = 1f; 

		NBodyCollision.CollisionType type = NBodyCollision.CollisionType.ABSORB_IMMEDIATE;

		type = (NBodyCollision.CollisionType)EditorGUILayout.EnumPopup(new GUIContent("Collision Type", collTip), nbc.collisionType);
		precedence = EditorGUILayout.IntField(new GUIContent("Collision Precedence", cTip), nbc.collisionPrecedence);
		if (type == NBodyCollision.CollisionType.EXPLODE) {
			explodePF = (GameObject) EditorGUILayout.ObjectField(
				new GUIContent("Explosion Prefab", "Particle System with NBodyParticles"), explodePF, typeof(GameObject), true);
		}
        else if (type == NBodyCollision.CollisionType.BOUNCE)
        {
            bounceFactor = EditorGUILayout.Slider(new GUIContent("Bounce", bTip), nbc.bounceFactor, 0f, 1f);

        }
        else if (type == NBodyCollision.CollisionType.EXPLODE_OR_BOUNCE)
        {
            explodePF = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Explosion Prefab", "Particle System with NBodyParticles"), explodePF, typeof(GameObject), true);
            bounceFactor = EditorGUILayout.Slider(new GUIContent("Bounce", bTip), nbc.bounceFactor, 0f, 1f);
            minRelVtoExplode = EditorGUILayout.FloatField(new GUIContent("Min RelV to Explode", dTip), nbc.minRelVtoExplode);
        }
        if (GUI.changed) {
			Undo.RecordObject(nbc, "NBodyCollision Change");
			nbc.explosionPrefab = explodePF;
			nbc.collisionType = type; 
			nbc.bounceFactor = bounceFactor;
            nbc.minRelVtoExplode = minRelVtoExplode;
			nbc.collisionPrecedence = precedence;
			EditorUtility.SetDirty(nbc);
		}
	}
}
