using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// Detects a long press on a UI button without interfering with the normal short click.
//
// Short click : Button.onClick fires as normal.
// Long press  : OnLongPress fires after HoldDuration seconds.
//               LongPressConsumed is set true for one frame so PuzzleUI
//               can suppress the digit insert that would fire on PointerUp.
public class LongPressButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float HoldDuration = 0.4f;

    // Fired when the button is held for HoldDuration seconds.
    public event Action OnLongPress;

    // True for one frame after a long press fires.
    // PuzzleUI reads this to suppress the digit insert on the same PointerUp.
    public bool LongPressConsumed { get; private set; }

    private bool  _holding   = false;
    private bool  _longFired = false;
    private float _holdTimer = 0f;

    void Update()
    {
        if (!_holding) return;

        _holdTimer += Time.deltaTime;

        if (_holdTimer >= HoldDuration && !_longFired)
        {
            _longFired          = true;
            LongPressConsumed   = true;
            OnLongPress?.Invoke();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _holding   = true;
        _longFired = false;
        _holdTimer = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _holding = false;
        // Reset the consumed flag after one frame so it doesn't persist.
        if (_longFired) StartCoroutine(ResetConsumed());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _holding            = false;
        _longFired          = false;
        _holdTimer          = 0f;
        LongPressConsumed   = false;
    }

    private IEnumerator ResetConsumed()
    {
        yield return null; // wait one frame
        LongPressConsumed = false;
    }
}