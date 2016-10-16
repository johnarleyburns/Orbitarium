using UnityEngine;
using UnityEngine.VR;
using System.Collections.Generic;
using System;

/// <summary>
/// Off screen indicator.
/// Classic wrapper, user doesn't need to worry about implementation
/// </summary>
namespace Greyman
{
    public class OffScreenIndicator : MonoBehaviour
    {

        public GameController gameController;
        public bool enableDebug = true;
        public bool VirtualRealitySupported = false;
        public float VR_cameraDistance = 5;
        public float VR_radius = 1.8f;
        public float VR_indicatorScale = 0.1f;
        public GameObject canvas;
        public int Canvas_circleRadius = 5; //size in pixels
        public int Canvas_border = 10; // when Canvas is Square pixels in border
        public int Canvas_indicatorSize = 100; //size in pixels
        public Indicator[] indicators;
        public FixedTarget[] targets;

        //public 
        private OffScreenIndicatorManager manager;
        private bool showing;
        private int runtimeIndicatorsAddedCount = 0;
        private List<Transform> allTargets = new List<Transform>();

        void Awake()
        {
            Setup();
        }

        void Update()
        {
            if (gameController.IsReady())
            {
                if (!showing)
                {
                    AddFixedIndicators();
                    showing = true;
                }
            }
            else
            {
                if (showing)
                {
                    TearDown();
                    showing = false;
                }
            }
        }
        /*
        void Awake() {
            Setup();
        }
        */
        /* JAB NEW */
        public void UpdateIndicatorText(int indicatorId, string text)
        {
            if (manager != null && manager.indicators != null && manager.indicators.Length > 0)
            {
                manager.indicators[indicatorId].onScreenTextString = text;
            }
        }

        public void AddIndicator(Transform target, int indicatorID)
        {
            manager.AddIndicator(target, indicatorID);
            allTargets.Add(target);
        }

        public void RemoveIndicator(Transform target)
        {
            manager.RemoveIndicator(target);
            allTargets.Remove(target);
        }

        private void Setup()
        {
            /*
			if (VRSettings.enabled){
				VR = true;
			} else {
				VR = false;
			}
			*/
            if (VirtualRealitySupported)
            {
                manager = gameObject.AddComponent<OffScreenIndicatorManagerVR>();
                (manager as OffScreenIndicatorManagerVR).cameraDistance = VR_cameraDistance;
                (manager as OffScreenIndicatorManagerVR).radius = VR_radius;
                (manager as OffScreenIndicatorManagerVR).indicatorScale = VR_indicatorScale;
                (manager as OffScreenIndicatorManagerVR).CreateIndicatorsParent();
            }
            else
            {
                manager = gameObject.AddComponent<OffScreenIndicatorManagerCanvas>();
                (manager as OffScreenIndicatorManagerCanvas).indicatorsParentObj = canvas;
                (manager as OffScreenIndicatorManagerCanvas).circleRadius = Canvas_circleRadius;
                (manager as OffScreenIndicatorManagerCanvas).border = Canvas_border;
                (manager as OffScreenIndicatorManagerCanvas).indicatorSize = Canvas_indicatorSize;
            }
            manager.indicators = indicators;
            manager.enableDebug = enableDebug;
            manager.CheckFields();
        }

        private void AddFixedIndicators()
        {
            foreach (FixedTarget target in targets)
            {
                manager.AddIndicator(target.target, target.indicatorID);
                allTargets.Add(target.target);
            }
        }

        private void TearDown()
        {
            /*
             *             foreach (FixedTarget target in targets)
                        {
                            manager.RemoveIndicator(target.target);
                        }
            */
            foreach (Transform target in allTargets)
            {
                if (manager.ExistsIndicator(target))
                {
                    manager.RemoveIndicator(target);
                }
            }
            manager.indicators = new Indicator[] { };
            if (runtimeIndicatorsAddedCount > 0)
            {
                Array.Resize(ref indicators, indicators.Length - runtimeIndicatorsAddedCount);
                runtimeIndicatorsAddedCount = 0;
            }
        }

        public int AddNewIndicatorFromClone(int indicatorId, string text)
        {
            Indicator clone = indicators[indicatorId].Clone();
            clone.onScreenTextString = text;
            int newIndicatorId = indicators.Length;
            Array.Resize(ref indicators, newIndicatorId + 1);
            indicators[newIndicatorId] = clone;
            manager.indicators = indicators;
            runtimeIndicatorsAddedCount++;
            return newIndicatorId;
        }
    }


    /// <summary>
    /// Indicator.
    /// References and colors for indicator sprites
    /// </summary>
    [System.Serializable]
    public class Indicator
    {

        public Indicator Clone()
        {
            Indicator clone = this.MemberwiseClone() as Indicator;
            clone.targetOffset = new Vector3(clone.targetOffset.x, clone.targetOffset.y, clone.targetOffset.z);
            clone.onScreenTextString = "";
            return clone;
        }

        public Sprite onScreenSprite;
        public Color onScreenColor = Color.white;
        public bool onScreenRotates;

        /* JAB NEW */
        public bool hasOnScreenText;
        public float onScreenTextPosY;
        public float onScreenTextWidth;
        public float onScreenTextHeight;
        public string onScreenTextString;
        public Font onScreenFont;
        public FontStyle onScreenFontStyle;
        public int onScreenFontSize;
        public Color onScreenFontColor;
        /* JAB END */

        public Sprite offScreenSprite;
        public Color offScreenColor = Color.white;
        public bool offScreenRotates;
        public int CanvasIndicatorSizeX = 100;
        public int CanvasIndicatorSizeY = 100;
        public Vector3 targetOffset;
        /// <summary>
        /// Both sprites need to have the same transition
        /// aswell both sprites need to have the same duration.
        /// </summary>
        public Transition transition;
        public float transitionDuration = 1;
        [System.NonSerialized]
        public bool showOnScreen;
        [System.NonSerialized]
        public bool showOffScreen;

        public enum Transition
        {
            None,
            Fading,
            Scaling
        }


    }

    [System.Serializable]
    public class FixedTarget
    {
        public Transform target;
        public int indicatorID;
    }
}