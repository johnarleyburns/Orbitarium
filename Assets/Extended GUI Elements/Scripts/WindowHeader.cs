using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;
using System;

[Obsolete("This Component is no longer used and have had its functionality move to the window component.", true)]
public class WindowHeader : UIBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    [SerializeField]
    Window _mainWindow;
    Vector3 _mousepos;

    protected override void Awake()
    {
        if (_mainWindow == null)
        {
            if (this.transform.parent.GetComponent<Window>() != null)
            {
                _mainWindow = this.transform.parent.GetComponent<Window>();
            }
            else
            {
                Debug.LogError("The variable 'Main Window' is null, please assign it in editor...");
                return;
            }
        }
    }

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        _mousepos = eventData.position;
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        if (_mainWindow.dragable)
        {
            if (GetComponentInParent<Canvas>() != null && GetComponentInParent<Canvas>().renderMode == RenderMode.WorldSpace && GetComponentInParent<Canvas>().worldCamera != null)
            {
                Vector3 wposb = new Vector3();
                Vector3 wposa = new Vector3();
                if (GetComponentInParent<Canvas>().worldCamera.orthographic)
                {
                    wposb = GetComponentInParent<Canvas>().worldCamera.ScreenToWorldPoint(new Vector3(_mousepos.x, _mousepos.y, 0));
                    wposa = GetComponentInParent<Canvas>().worldCamera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, 0));
                }
                else
                {
                    wposb = GetComponentInParent<Canvas>().worldCamera.ScreenToWorldPoint(new Vector3(_mousepos.x, _mousepos.y,
                        (GetComponentInParent<Canvas>().transform.position.z - GetComponentInParent<Canvas>().worldCamera.transform.position.z)));
                    wposa = GetComponentInParent<Canvas>().worldCamera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y,
                        (GetComponentInParent<Canvas>().transform.position.z - GetComponentInParent<Canvas>().worldCamera.transform.position.z)));
                }
                _mainWindow.MoveWindow(_mainWindow.transform.position + new Vector3(wposa.x - wposb.x, wposa.y - wposb.y, 0));
            }
            else
            {
                _mainWindow.MoveWindow(_mainWindow.transform.position + new Vector3(eventData.position.x - _mousepos.x, eventData.position.y - _mousepos.y, 0));
            }
            _mousepos = eventData.position;
        }
    }
}
