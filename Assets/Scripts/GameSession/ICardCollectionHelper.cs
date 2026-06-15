using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public interface ICardCollectionHelper
{
    public void ShuffleList(List<Card> cardList)
    {
        System.Random rng = new();
        for (int i = cardList.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (cardList[i], cardList[j]) = (cardList[j], cardList[i]);
            cardList[i].zIndex = -j;
        }
    }

    public void SetZIndexes(List<Card> cardList, bool changeTransform = false)
    {
        for (byte i = 0; i < cardList.Count; i++)
        {
            cardList[i].zIndex = -i;

            if (changeTransform)
            {
                Transform cardTransform = cardList[i].transform;
                cardTransform.localPosition = new(cardTransform.localPosition.x, cardTransform.localPosition.y, -i);
            }
        }
    }

    public void SafeDestroy(GameObject gameObject, bool log = false)
    {
        if (log)
            Debug.LogWarning($"Killed tweens: {gameObject.transform.DOKill()}");
        else
            gameObject.transform.DOKill();
        
        GameObject.Destroy(gameObject);
    }
}