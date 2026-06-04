using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using Unity.VisualScripting;

public class Player : MonoBehaviour, ICardCollectionHelper
{
    [Header("Контейнеры")]
    public Transform cardsContainer;
    public RectTransform containerRect;

    [Header("Настройки карт")]
    [SerializeField] private float cardWidth = 200f;
    [SerializeField] private float first = 5f;
    [SerializeField] private float second = 50f;

    [Header("Отступы от краёв")]
    [SerializeField] private float horizontalPadding = 50f;

    public List<Card> hand = new();
    public bool alive = true;

    public bool turn = false;
    public int cardsToTake = 1;

    public async Task AddDefuseCard(Deck deck, GameObject defusePrefab)
    {
        GameObject defuseCardObject = Instantiate(defusePrefab, deck.transform, true);
        
        defuseCardObject.transform.localEulerAngles = new(
            defuseCardObject.transform.localEulerAngles.x,
            180f,
            defuseCardObject.transform.localEulerAngles.z
        );

        Card defuseCard = defuseCardObject.GetComponent<Card>();

        defuseCard.cardName = Card.CardName.Defuse;
        defuseCardObject.name = "Defuse_" + name[^1];
        await AddCard(defuseCardObject, true);
    }

    public async Task AddCard(GameObject newCard, bool repositionCards = false)
    {
        if (newCard == null) return;

        newCard.transform.SetParent(cardsContainer);
        
        newCard.transform.DOLocalRotate(new(0f, newCard.transform.localEulerAngles.y, 0f), 0.2f).Play();
        await newCard.transform.DOLocalMove(new(0, 0, -hand.Count - 1), 0.2f).Play().AsyncWaitForCompletion();

        newCard.tag = "InHand";
   
        Card card = newCard.GetComponent<Card>();

        card.zIndex = -hand.Count;

        hand.Add(card);

        if (repositionCards)
            RepositionAllCards();
    }

    public Card RemoveCard(Card cardToRemove, bool repositionCards = true)
    {
        if (hand.Count == 0)
        {
            Debug.LogWarning("Рука пуста!");
            return null;
        }
        
        hand.Remove(cardToRemove);
        
        if (repositionCards)
        {
            RepositionAllCards();
            ICardCollectionHelper cardCollectionHelper = this;
            cardCollectionHelper.SetZIndexes(hand, true);
        }
        
        return cardToRemove;
    }

    public void RepositionAllCards()
    {
        if (hand.Count == 0) return;

        float containerWidth = containerRect.rect.width;
        float totalCardsWidth = hand.Count * cardWidth;
        float availableWidth = Mathf.Min(containerWidth, totalCardsWidth / first + totalCardsWidth * second) - horizontalPadding / 2;
        int numberOfSpaces = hand.Count - 1;

        float currentSpacing;

        if (numberOfSpaces > 0)
            currentSpacing = (availableWidth - totalCardsWidth) / numberOfSpaces;
        else
            currentSpacing = 0;

        float totalGroupWidth = totalCardsWidth + (currentSpacing * numberOfSpaces);

        float startX = -totalGroupWidth / 2f;
        for (int i = 0; i < hand.Count; i++)
        {
            float xPos = startX + (i * (cardWidth + currentSpacing)) + cardWidth / 2;
            hand[i].transform.DOLocalMoveX(xPos, 0.05f).Play();
        }
    }
}