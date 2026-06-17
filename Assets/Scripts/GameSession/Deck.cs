using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;

public class Deck : MonoBehaviour, ICardCollectionHelper
{
    public List<Card> cards = new();
    const byte AttackCount = 4;
    const byte FavorCount = 4;
    const byte NopeCount = 5;
    const byte SeeTheFutureCount = 5;
    const byte ShuffleCount = 4;
    const byte SkipCount = 4;
    const byte BeardCatCount = 4;
    const byte CatsSchrodingerCount = 4;
    const byte CatterMelonCount = 4;
    const byte HairyPotatoCatCount = 4;
    const byte TacoCatCount = 4;
    const byte DefuseCount = 1;
    byte explodingKittenCount = 0;

    public void Fill(List<GameObject> cardPrefabs)
    {
        cards = new();

        AddCardsToList(cards, Card.CardName.Attack, cardPrefabs[0], AttackCount);
        AddCardsToList(cards, Card.CardName.BeardCat, cardPrefabs[1], BeardCatCount);
        AddCardsToList(cards, Card.CardName.CatsSchrodinger, cardPrefabs[2], CatsSchrodingerCount);
        AddCardsToList(cards, Card.CardName.CatterMelon, cardPrefabs[3], CatterMelonCount);
        AddCardsToList(cards, Card.CardName.Defuse, cardPrefabs[4], DefuseCount);
        AddCardsToList(cards, Card.CardName.Favor, cardPrefabs[6], FavorCount);
        AddCardsToList(cards, Card.CardName.HairyPotatoCat, cardPrefabs[7], HairyPotatoCatCount);
        AddCardsToList(cards, Card.CardName.Nope, cardPrefabs[8], NopeCount);
        AddCardsToList(cards, Card.CardName.SeeTheFuture, cardPrefabs[9], SeeTheFutureCount);
        AddCardsToList(cards, Card.CardName.Shuffle, cardPrefabs[10], ShuffleCount);
        AddCardsToList(cards, Card.CardName.Skip, cardPrefabs[11], SkipCount);
        AddCardsToList(cards, Card.CardName.TacoCat, cardPrefabs[12], TacoCatCount);

        ICardCollectionHelper cardCollectionHelper = this;
        cardCollectionHelper.ShuffleList(cards);

        SetFirstThreeCardsActive();
    }

    public void AddExplodingKitten(GameObject cardPrefab, byte playersCount)
    {
        explodingKittenCount = Math.Min((byte)(playersCount - 1), (byte)4);
        explodingKittenCount = 100;

        AddCardsToList(cards, Card.CardName.ExplodingKitten, cardPrefab, explodingKittenCount);

        ICardCollectionHelper cardCollectionHelper = this;
        cardCollectionHelper.ShuffleList(cards);

        SetFirstThreeCardsActive();
    }

    public async Task ReturnExplodingKitten(Card explodingKittenCard, GameObject exlodingKittenPrefab)
    {
        explodingKittenCard.tag = "InDeck";
        explodingKittenCard.transform.SetParent(transform);

        explodingKittenCard.transform.DOLocalMove(Vector3.zero, 0.2f).Play();
        await explodingKittenCard.transform.DOLocalRotate(new(0f, explodingKittenCard.transform.localEulerAngles.y, 0f), 0.2f).Play().AsyncWaitForCompletion();

        ICardCollectionHelper cardCollectionHelper = this;
        cardCollectionHelper.SafeDestroy(explodingKittenCard.gameObject);

        AddExplodingKitten(exlodingKittenPrefab, 2);

        cardCollectionHelper.ShuffleList(cards);

        SetFirstThreeCardsActive();
    }

    public void SetFirstThreeCardsActive(bool state = true)
    {
        for (int i = 0; i < cards.Count; i++)
            if (cards[i].gameObject.activeSelf)
                cards[i].gameObject.SetActive(false);

        if (cards.Count > 0)
        {
            int minCount = Math.Min(3, cards.Count);

            for (int i = 0; i < minCount; i++)
                cards[i].gameObject.SetActive(state);
        }
    }

    private void AddCardsToList(List<Card> cards, Card.CardName cardName, GameObject cardPrefab, byte count)
    {
        cardPrefab.GetComponent<Card>().cardName = cardName;

        for (int i = 0; i < count; i++)
        {
            GameObject cardObject = Instantiate(cardPrefab, transform, true);

            cardObject.name = $"{cardName}_{i}";
            cardObject.tag = "InDeck";

            Card card = cardObject.GetComponent<Card>();
            cards.Add(card);
        }
    }

    public Card TopCard()
    {
        if (cards.Count == 0)
        {
            Debug.LogWarning("������ �����!");
            return null;
        }

        Card drawnCard = cards[0];
        drawnCard.transform.SetParent(null);
        cards.RemoveAt(0);

        SetFirstThreeCardsActive();

        return drawnCard;
    }

    public Card PeekTopCard()
    {
        if (cards.Count == 0) return null;
        return cards[0];
    }

    public List<Card> PeekTopThreeCards()
    {
        if (cards.Count == 0) return null;

        if (cards.Count > 3) cards[3].gameObject.SetActive(true);
        
        return cards.GetRange(0, Math.Min(cards.Count, 3));
    }

    public void PrintDeck()
    {
        Debug.Log($"� ������ {cards.Count} ����:");
        byte index = 0;
        
        foreach (var card in cards)
            Debug.Log($"{index++}: {card.cardName}");
    }
}