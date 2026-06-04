using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private Camera gameCamera;
    public GameManager gameManager;
    public InputActionAsset inputActions;
    public InputAction clickActions;
    public interface IClickable
    {
        void OnClick();
    }

    private void Awake()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;

        clickActions = inputActions.FindActionMap("Gameplay").FindAction("CardInteract");
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    private void OnEnable()
    {
        if (clickActions != null)
        {
            clickActions.performed += OnClickPerformed;
        }
    }

    private void OnDisable()
    {
        if (clickActions != null)
        {
            clickActions.performed -= OnClickPerformed;
        }
    }

    private void OnDestroy()
    {
        if (clickActions != null)
        {
            clickActions.performed -= OnClickPerformed;
        }
    }


    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        RaycastHit2D hit = Physics2D.Raycast(gameCamera.ScreenToWorldPoint(mousePosition), Vector2.zero);

        if (hit)
        {
            IClickable clickable = hit.collider.GetComponent<IClickable>();
            clickable?.OnClick();
        }
        else
            gameManager.CancelAllCardsToDraw();
    }
}