using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class PlayedCards : MonoBehaviour, ICardCollectionHelper
{
    public List<Card> cards = new();

    public async Task AddCard(GameObject newCard)
    {
        if (newCard == null) return;

        Card card = newCard.GetComponent<Card>();
        card.zIndex = -cards.Count;

        Transform cardTransform = card.transform;
        newCard.transform.SetParent(transform);

        newCard.tag = "PlayedCard";
        cards.Add(card);

        newCard.transform.DOLocalMove(
            new(transform.localPosition.x + Random.Range(-50f, 50f),
            transform.localPosition.y + Random.Range(-5f, 5f)), 0.2f).Play();

        Vector3 eulerAngles = transform.localEulerAngles;
        await newCard.transform.DOLocalRotate(new(
            eulerAngles.x,
            eulerAngles.y,
            eulerAngles.z + Random.Range(-10, 11)), 0.2f).Play().AsyncWaitForCompletion();

        cardTransform.localPosition = new(cardTransform.localPosition.x, cardTransform.localPosition.y, -cards.Count);
    }

    public void RemoveCard(Card cardToRemove)
    {
        if (cards.Count == 0)
        {
            Debug.LogWarning("Разыгранных карт нет!");
            return;
        }

        cards.Remove(cardToRemove);
    }
}