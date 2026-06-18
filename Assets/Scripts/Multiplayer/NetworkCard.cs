using UnityEngine;
using DG.Tweening;
using System.Threading.Tasks;
using TMPro;
using Mirror;

public class NetworkCard : NetworkBehaviour, InputHandler.IClickable, ICardCollectionHelper
{
    public CardName cardName;
    public NetworkGameManager gameManager;
    public Tween doLocalMoveY;
    public bool hidden = false;
    public float zIndex = 0;
    private Sprite cardNameSprite;

    public enum CardName
    {
        Defuse, // ���������
        Attack, // �������
        SeeTheFuture, // ��������� � �������
        Nope, // ����
        Skip, // ������
        Shuffle, // �������
        Favor, // ���������
        BeardCat, // ���������
        CatsSchrodinger, // �������� ����
        CatterMelon, // ���������
        HairyPotatoCat, // ��������� �����-��������
        TacoCat, // �������
        ExplodingKitten, // �������� �������
        Default
    }

    async void Awake()
    {
        gameManager = GetTopParent(transform).gameObject.GetComponent<NetworkGameManager>();

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

    public async void OnClick()
    {
        Debug.Log($"�� ���� ��������! � �����: {cardName}");

        switch (tag)
        {
            case "InDeck":
                if (gameManager.gameState == NetworkGameManager.GameState.PlayerTurn)
                    await gameManager.GetCardFromDeck();
                break;

            case "InHand":
                _ = gameManager.CancelAllCardsToDraw();

                if (gameManager.gameState == NetworkGameManager.GameState.PlayerTurn && GetComponentInParent<Player>().turn)
                {
                    if (gameManager.HandleCardDraw(this)) return;
                    
                    _ = gameManager.CancelGetCardFromHand(this);
                }

                if (gameManager.gameState == NetworkGameManager.GameState.NopeAwaiting && IsCardNameByItsName(CardName.Nope))
                {
                    Player player = GetComponentInParent<Player>();

                    if (player == null) return;

                    if (!player.CompareTag("Player")) return;

                    gameManager.SetCardToDraw(this);
                }
                break;

            case "ToDraw":
                await gameManager.PreDrawCard(this);
                _ = gameManager.CancelAllCardsToDraw();
                break;

            case "ToSteal":
                if (gameManager.gameState == NetworkGameManager.GameState.DrawFavor)
                    await gameManager.GiveCard(this);
                break;

            default:
                break;
        }
    }

    public void OnMouseEnter()
    {
        switch (gameManager.gameState)
        {
            case NetworkGameManager.GameState.PlayerTurn:
                Player player = transform.GetComponentInParent<Player>();

                if (player != null)
                    if (!player.turn) return;

                if (!CompareTag("InHand")) return;

                gameManager.GetCardFromHand(this);
                break;

            case NetworkGameManager.GameState.DrawFavor:
                if (CompareTag("ToSteal"))
                    gameManager.GetCardFromHand(this);
                break;

            case NetworkGameManager.GameState.NopeAwaiting:
                if (!IsCardNameByItsName(CardName.Nope)) return;

                if (!CompareTag("InHand")) return;

                player = transform.GetComponentInParent<Player>();

                if (player == null) return;

                if (!player.CompareTag("Player")) return;

                gameManager.GetCardFromHand(this);
                break;
        }
    }

    public void OnMouseExit()
    {
        switch (gameManager.gameState)
        {
            case NetworkGameManager.GameState.PlayerTurn:
                Player player = transform.GetComponentInParent<Player>();

                if (player != null)
                    if (!player.turn) return;

                if (!CompareTag("InHand")) return;

                _ = gameManager.CancelGetCardFromHand(this);
                break;

            case NetworkGameManager.GameState.DrawFavor:
                if (CompareTag("ToSteal"))
                    _ = gameManager.CancelGetCardFromHand(this);

                break;

            case NetworkGameManager.GameState.NopeAwaiting:
                if (!IsCardNameByItsName(CardName.Nope)) return;

                if (!CompareTag("InHand")) return;

                player = transform.GetComponentInParent<Player>();

                if (player == null) return;

                if (!player.CompareTag("Player")) return;

                _ = gameManager.CancelGetCardFromHand(this);
                break;
        }
    }
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