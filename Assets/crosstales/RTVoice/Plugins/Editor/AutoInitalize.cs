﻿using UnityEditor;
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
#endif
using Crosstales.RTVoice.Util;

namespace Crosstales.RTVoice.EditorExt
{
    /// <summary>Automatically adds the neccessary RTVoice-prefabs to the current scene.</summary>
    [InitializeOnLoad]
    public class AutoInitalize
    {

        #region Variables

#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
        private static Scene currentScene;
#else
      private static string currentScene;
#endif

        #endregion

        #region Constructor

        static AutoInitalize()
        {
            EditorApplication.hierarchyWindowChanged += hierarchyWindowChanged;
        }

        #endregion

        #region Private static methods

        private static void hierarchyWindowChanged()
        {
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
            if (currentScene != EditorSceneManager.GetActiveScene())
            {
#else
            if (currentScene != EditorApplication.currentScene) 
            {
#endif
                if (Constants.PREFAB_AUTOLOAD)
                {
                    EditorHelper.AddRTVoice();
                }

#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
                currentScene = EditorSceneManager.GetActiveScene();
#else
                currentScene = EditorApplication.currentScene;
#endif
            }
        }

        #endregion
    }
}
// Copyright 2016 www.crosstales.com