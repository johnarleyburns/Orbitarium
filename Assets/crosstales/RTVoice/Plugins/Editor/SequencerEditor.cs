using UnityEngine;
using UnityEditor;
using Crosstales.RTVoice.Tool;

namespace Crosstales.RTVoice.EditorExt
{
    /// <summary>Custom editor for the 'Sequencer'-class.</summary>
    [CustomEditor(typeof(Sequencer))]
    public class SequencerEditor : Editor
    {
        #region Variables

        private Sequencer script;

        #endregion

        #region Editor methods

        public void OnEnable()
        {
            script = (Sequencer)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (script.isActiveAndEnabled)
            {
                if (!Speaker.isTTSAvailable)
                {
                    EditorHelper.SeparatorUI();

                    EditorHelper.NoVoicesUI();
                }
            }
            else
            {
                EditorHelper.SeparatorUI();

                GUILayout.Label("Script is disabled!", EditorStyles.boldLabel);
            }
        }

        #endregion
    }
}
// Copyright 2016 www.crosstales.com