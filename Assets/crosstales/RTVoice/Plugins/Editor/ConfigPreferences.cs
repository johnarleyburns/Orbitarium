﻿using UnityEditor;
using UnityEngine;
using Crosstales.RTVoice.Util;

namespace Crosstales.RTVoice.EditorExt
{
    /// <summary>Unity "Preferences" extension.</summary>
    public class ConfigPreferences : ConfigBase
    {

        #region Variables

        private static int tab = 0;
        private static int lastTab = 0;

        #endregion

        #region Static methods

        [PreferenceItem(Constants.ASSET_NAME)]
        private static void RTVPreferencesGUI()
        {

            tab = GUILayout.Toolbar(tab, new string[] { "Configuration", "About" });

            if (tab != lastTab)
            {
                lastTab = tab;
                GUI.FocusControl(null);
            }

            if (tab == 0)
            {
                showConfiguration();
            }
            else
            {
                showAbout();
            }

            if (GUI.changed)
            {
                save();
            }
        }

        #endregion
    }
}
// Copyright 2016 www.crosstales.com