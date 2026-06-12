using UnityEngine;
using DG.Tweening;
using System.Threading.Tasks;
using TMPro;

public class Card : MonoBehaviour, InputHandler.IClickable, ICardCollectionHelper
{
    public CardName cardName;
    public GameManager gameManager;
    public Tween doLocalMoveY;
    public Sequence shakeAndColor;
    public bool hidden = false;
    public float zIndex = 0;
    private Sprite cardNameSprite;

    public enum CardName
    {
        Defuse, // Обезвредь
        Attack, // Нападай
        SeeTheFuture, // Подсмотри в будущее
        Nope, // Неть
        Skip, // Слиняй
        Shuffle, // Затасуй
        Favor, // Одолжение
        BeardCat, // Бородокот
        CatsSchrodinger, // Шрёдингер кота
        CatterMelon, // Кошкарбуз
        HairyPotatoCat, // Волосатая кошка-картошка
        TacoCat, // Такикот
        ExplodingKitten, // Взрывной котенок
        Default
    }

    async void Awake()
    {
        gameManager = GetTopParent(transform).gameObject.GetComponent<GameManager>();

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        cardNameSprite = spriteRenderer.sprite;
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        await SetHidden();
    }

    public async Task SetHidden(bool hide = true, float duration = 0f)
    {
        if (hide == hidden) return;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Vector3 eulerAngles = transform.localEulerAngles;
        TextMeshPro[] texts = GetComponentsInChildren<TextMeshPro>(true);

        if (hide)
        {
            await transform.DOLocalRotate(new(eulerAngles.x, 90, eulerAngles.z), duration > 0 ? duration / 2f : 0).Play().AsyncWaitForCompletion();

            spriteRenderer.sprite = Resources.Load<Sprite>($"Sprites/Default");

            foreach (var text in texts)
                text.gameObject.SetActive(false);

            await transform.DOLocalRotate(new(eulerAngles.x, 180, eulerAngles.z), duration > 0 ? duration / 2f : 0).Play().AsyncWaitForCompletion();
        }
        else
        {
            await transform.DOLocalRotate(new(eulerAngles.x, 90, eulerAngles.z), duration > 0 ? duration / 2f : 0).Play().AsyncWaitForCompletion();

            spriteRenderer.sprite = cardNameSprite;

            foreach (var text in texts)
                text.gameObject.SetActive(true);

            await transform.DOLocalRotate(new(eulerAngles.x, 0, eulerAngles.z), duration > 0 ? duration / 2f : 0).Play().AsyncWaitForCompletion();
        }

        hidden = hide;
    }

    public Transform GetTopParent(Transform target)
    {
        Transform current = target;
        while (current.parent != null)
        {
            current = current.parent;
        }
        return current;
    }

#if !UNITY_ANDROID && !UNITY_IOS
    public async void OnClick()
    {
        Debug.Log($"На меня кликнули! Я карта: {cardName}");

        switch (tag)
        {
            case "InDeck":
                if (gameManager.gameState == GameManager.GameState.PlayerTurn)
                    await gameManager.GetCardFromDeck();
                break;

            case "InHand":
                gameManager.CancelAllCardsToDraw();

                if (gameManager.gameState == GameManager.GameState.PlayerTurn && GetComponentInParent<Player>().turn)
                    gameManager.HandleCardDraw(this);

                if (gameManager.gameState == GameManager.GameState.NopeAwaiting && IsCardNameByItsName(CardName.Nope))
                {
                    Player player = GetComponentInParent<Player>();

                    if (player == null) return;

                    if (!player.CompareTag("Player")) return;

                    gameManager.SetCardToDraw(this);
                }
                break;

            case "ToDraw":
                await gameManager.PreDrawCard(this);
                gameManager.CancelAllCardsToDraw();
                break;

            case "ToSteal":
                if (gameManager.gameState == GameManager.GameState.DrawFavor)
                    await gameManager.GiveCard(this);
                break;

            default:
                break;
        }
    }

    void OnMouseEnter()
    {
        switch (gameManager.gameState)
        {
            case GameManager.GameState.PlayerTurn:
                Player player = transform.GetComponentInParent<Player>();

                if (player != null)
                    if (!player.turn) return;

                if (!CompareTag("InHand")) return;

                gameManager.GetCardFromHand(this);
                break;

            case GameManager.GameState.DrawFavor:
                if (CompareTag("ToSteal"))
                    gameManager.GetCardFromHand(this);
                break;

            case GameManager.GameState.NopeAwaiting:
                if (!IsCardNameByItsName(CardName.Nope)) return;

                if (!CompareTag("InHand")) return;

                player = transform.GetComponentInParent<Player>();

                if (player == null) return;

                if (!player.CompareTag("Player")) return;

                gameManager.GetCardFromHand(this);
                break;
        }
    }

    void OnMouseExit()
    {
        switch (gameManager.gameState)
        {
            case GameManager.GameState.PlayerTurn:
                Player player = transform.GetComponentInParent<Player>();

                if (player != null)
                    if (!player.turn) return;

                if (!CompareTag("InHand")) return;

                gameManager.CancelGetCardFromHand(this);
                break;

            case GameManager.GameState.DrawFavor:
                if (CompareTag("ToSteal"))
                    gameManager.CancelGetCardFromHand(this);
                break;

            case GameManager.GameState.NopeAwaiting:
                if (!IsCardNameByItsName(CardName.Nope)) return;

                if (!CompareTag("InHand")) return;

                player = transform.GetComponentInParent<Player>();

                if (player == null) return;

                if (!player.CompareTag("Player")) return;

                gameManager.CancelGetCardFromHand(this);
                break;
        }
    }
#endif
    public void TweenDoLocalMoveY()
    {
        doLocalMoveY ??= transform
            .DOLocalMoveY(transform.localPosition.y + 20, 0.15f)
            .SetAutoKill(false);

        if (!doLocalMoveY.IsPlaying())
            doLocalMoveY.PlayForward();
    }

    public bool IsDefuse() => cardName == CardName.Defuse;
    public bool IsExplodingKitten() => cardName == CardName.ExplodingKitten;
    public bool IsCardNameByItsName(CardName cardName) => this.cardName == cardName;
    public bool IsCatCard() => cardName == CardName.TacoCat
                            || cardName == CardName.CatterMelon
                            || cardName == CardName.BeardCat
                            || cardName == CardName.HairyPotatoCat
                            || cardName == CardName.CatsSchrodinger;
}