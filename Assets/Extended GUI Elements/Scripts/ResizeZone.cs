using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;
using System;

[Obsolete("This Component is no longer used and have had its functionality move to the window component.", true)]
[RequireComponent(typeof(Image))]
public class ResizeZone : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    [SerializeField]
    Window _mainWindow;
    [SerializeField]
    Direction _borderDirection;

    Vector2 _mousepos;

    Direction _direction;
    Vector2 _targetsize;

    protected virtual void Awake()
    {
        if (_mainWindow == null)
        {
            Debug.LogError("The variable 'Main Window' is null, please assign it in editor...");
        }
    }

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        if (_mainWindow.resizable)
        {
            Vector2 localCursor;

            RectTransform rect = GetComponent<RectTransform>();

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out localCursor))
                return;

            float xpos = localCursor.x;
            float ypos = localCursor.y;

            if (xpos < 0)
            {
                xpos = xpos + (rect.rect.width * (1 - rect.pivot.x));
            }
            else
            {
                xpos += (rect.rect.width * rect.pivot.x);
            }
            if (ypos < 0)
            {
                ypos = ypos + (rect.rect.height * (rect.pivot.y));
            }
            else
            {
                ypos += (rect.rect.height * (rect.pivot.y));
            }

            _mousepos = eventData.position;
            _targetsize = _mainWindow.GetComponent<RectTransform>().rect.size;

            Vector2 localpos = new Vector2(xpos, ypos);
            switch (_borderDirection)
            {
                case Direction.Left:
                    if (localpos.y < _mainWindow.borderSize)
                    {
                        _direction = Direction.Left | Direction.Down;
                    }
                    else if (localpos.y > (Mathf.Abs(this.GetComponent<RectTransform>().rect.size.y) - (_mainWindow.borderSize)))
                    {
                        _direction = Direction.Left | Direction.Up;
                    }
                    else
                    {
                        _direction = Direction.Left;
                    }
                    break;
                case Direction.Right:
                    if (localpos.y < _mainWindow.borderSize)
                    {
                        _direction = Direction.Right | Direction.Down;
                    }
                    else if (localpos.y > (Mathf.Abs(this.GetComponent<RectTransform>().rect.size.y) - (_mainWindow.borderSize)))
                    {
                        _direction = Direction.Right | Direction.Up;
                    }
                    else
                    {
                        _direction = Direction.Right;
                    }
                    break;
                case Direction.Up:
                    if (localpos.x < _mainWindow.borderSize)
                    {
                        _direction = Direction.Left | Direction.Up;
                    }
                    else if (localpos.x > (Mathf.Abs(this.GetComponent<RectTransform>().rect.size.x) - (_mainWindow.borderSize)))
                    {
                        _direction = Direction.Right | Direction.Up;
                    }
                    else
                    {
                        _direction = Direction.Up;
                    }
                    break;
                case Direction.Down:
                    if (localpos.x < _mainWindow.borderSize)
                    {
                        _direction = Direction.Left | Direction.Down;
                    }
                    else if (localpos.x > (Mathf.Abs(this.GetComponent<RectTransform>().rect.size.x) - (_mainWindow.borderSize)))
                    {
                        _direction = Direction.Right | Direction.Down;
                    }
                    else
                    {
                        _direction = Direction.Down;
                    }
                    break;
                default:
                    _direction = Direction.None;
                    break;
            }
        }
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        if (_mainWindow.resizable)
        {
            if (GetComponentInParent<Canvas>() != null && GetComponentInParent<Canvas>().renderMode == RenderMode.WorldSpace && GetComponentInParent<Canvas>().worldCamera != null)
            {
                Vector3 wposb = new Vector3();
                Vector3 wposa = new Vector3();

                Vector2 currentPointerPosition = eventData.position;

                if (GetComponentInParent<Canvas>().worldCamera.orthographic)
                {
                    wposb = GetComponentInParent<Canvas>().worldCamera.ScreenToWorldPoint(_mousepos);
                    wposa = GetComponentInParent<Canvas>().worldCamera.ScreenToWorldPoint(currentPointerPosition);
                }
                else
                {
                    wposb = GetComponentInParent<Canvas>().worldCamera.ScreenToWorldPoint(new Vector3(_mousepos.x, _mousepos.y, (GetComponentInParent<Canvas>().transform.position.z - GetComponentInParent<Canvas>().worldCamera.transform.position.z)));
                    wposa = GetComponentInParent<Canvas>().worldCamera.ScreenToWorldPoint(new Vector3(currentPointerPosition.x, currentPointerPosition.y, (GetComponentInParent<Canvas>().transform.position.z - GetComponentInParent<Canvas>().worldCamera.transform.position.z)));
                }

                wposb = new Vector3(wposb.x * (1.0f / GetComponentInParent<Canvas>().transform.localScale.x), wposb.y * (1.0f / GetComponentInParent<Canvas>().transform.localScale.y), wposb.z);
                wposa = new Vector3(wposa.x * (1.0f / GetComponentInParent<Canvas>().transform.localScale.x), wposa.y * (1.0f / GetComponentInParent<Canvas>().transform.localScale.y), wposa.z);

                Vector2 mouseDelta = (wposa - wposb);

                if ((_direction & Direction.Left) == Direction.Left)
                {
                    mouseDelta = new Vector2(-mouseDelta.x, mouseDelta.y);
                }
                if ((_direction & Direction.Down) == Direction.Down)
                {
                    mouseDelta = new Vector2(mouseDelta.x, -mouseDelta.y);
                }
                _mainWindow.ResizeWindow(_targetsize + mouseDelta, _direction);
                _targetsize += mouseDelta;
                _mousepos = currentPointerPosition;
            }
            else
            {
                Vector2 currentPointerPosition = eventData.position;
                Vector2 mouseDelta = (currentPointerPosition - _mousepos);

                if ((_direction & Direction.Left) == Direction.Left)
                {
                    mouseDelta = new Vector2(mouseDelta.x * -1, mouseDelta.y);
                }
                if ((_direction & Direction.Down) == Direction.Down)
                {
                    mouseDelta = new Vector2(mouseDelta.x, mouseDelta.y * -1);
                }
                _mainWindow.ResizeWindow(_targetsize + mouseDelta, _direction);
                _targetsize += mouseDelta;
                _mousepos = currentPointerPosition;
            }
        }
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        
    }
}

[Flags]
public enum Direction
{
    None = 0,
    Up = 1,
    Down = 2,
    Left = 4,
    Right = 8,
}
