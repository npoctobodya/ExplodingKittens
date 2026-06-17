using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private Camera gameCamera;
    public GameManager gameManager;
    public Card pickedCard = null;
    public InputActionAsset inputActions;
    private InputAction clickActions;

    public interface IClickable
    {
        void OnClick();
    }

    private void Awake()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;

        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        
        if (inputActions != null)
        {
            clickActions = inputActions.FindActionMap("Gameplay").FindAction("CardInteract");
        }
    }

    private void OnEnable()
    {
        if (clickActions != null)
        {
            clickActions.canceled += OnInteract;
            clickActions.Enable();
        }
    }

    private void OnDisable()
    {
        if (clickActions != null)
        {
            clickActions.canceled -= OnInteract;
            clickActions.Disable();
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        // Получаем позицию в зависимости от устройства
        Vector2 screenPosition = GetScreenPosition(context);
        
        // Преобразуем в мировые координаты
        Vector3 worldPosition = gameCamera.ScreenToWorldPoint(screenPosition);
        worldPosition.z = 0;
        
        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);

        if (hit)
        {
            IClickable clickable = hit.collider.GetComponent<IClickable>();
            clickable?.OnClick();
        }
        else
            _ = gameManager.CancelAllCardsToDraw();
    }

    private Vector2 GetScreenPosition(InputAction.CallbackContext context)
    {
        // Для мыши
        if (context.control.device is Mouse mouse)
        {
            return mouse.position.ReadValue();
        }
        
        // Для касания (телефон)
        if (context.control.device is Touchscreen touchscreen)
        {
            // Берём первое активное касание
            if (touchscreen.touches.Count > 0)
                return touchscreen.touches[0].position.ReadValue();
        }

        return new(-1, -1);
    }

    private void OnValidate()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
    }
}