using UnityEngine;
using UnityEditor;
using Crosstales.RTVoice.Tool;
using Crosstales.RTVoice.Util;

namespace Crosstales.RTVoice.EditorExt
{
    /// <summary>Custom editor for the 'TextFileSpeaker'-class.</summary>
    [CustomEditor(typeof(TextFileSpeaker))]
    [CanEditMultipleObjects]
    public class TextFileSpeakerEditor : Editor
    {

        #region Variables

        private TextFileSpeaker script;

        #endregion

        #region Editor methods

        public void OnEnable()
        {
            script = (TextFileSpeaker)target;
        }

        public void OnDisable()
        {
            if (Helper.isEditorMode)
            {
                Speaker.Silence();
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorHelper.SeparatorUI();

            if (script.isActiveAndEnabled)
            {
                if (Speaker.isTTSAvailable)
                {
                    GUILayout.Label("Test-Drive", EditorStyles.boldLabel);

                    if (Helper.isEditorMode)
                    {
                        if (GUILayout.Button(new GUIContent("Speak", "Speaks a random text file with the selected voice and settings.")))
                        {
                            script.Speak();
                        }

                        if (GUILayout.Button(new GUIContent("Silence", "Silence the active speaker.")))
                        {
                            script.Silence();
                        }
                    }
                    else
                    {
                        GUILayout.Label("Disabled in Play-mode!");
                    }
                }
                else
                {
                    EditorHelper.NoVoicesUI();
                }
            }
            else
            {
                GUILayout.Label("Script is disabled!", EditorStyles.boldLabel);
            }
        }

        #endregion
    }
}
// Copyright 2016 www.crosstales.com