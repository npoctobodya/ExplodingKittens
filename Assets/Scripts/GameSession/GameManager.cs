using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Random = System.Random;

[RequireComponent(typeof(StatsManager))]
public class GameManager : MonoBehaviour, ICardCollectionHelper
{
    public List<Player> players = new();
    public List<GameObject> cardPrefabs = new();
    public Deck deck;
    public GameObject playersObject;
    public GameObject playedCardsObject;
    public GameObject uiGameManagerObject;
    public PlayedCards playedCards;
    public StatsManager statsManager;
    public UIGameManager uiGameManager;
    public byte playersCount = 5;
    public bool AITraining = false;
    public GameState gameState = GameState.Preparing;
    public enum GameState
    {
        Preparing,
        PlayerTurn,
        NopeAwaiting,
        DrawFavor,
        Finished
    }

    void Awake()
    {
        playedCards = playedCardsObject.GetComponent<PlayedCards>();
        statsManager = GetComponent<StatsManager>();
        uiGameManager = uiGameManagerObject.GetComponent<UIGameManager>();

        if (!AITraining)
            playersCount = (byte)PlayerPrefs.GetInt("PlayersCount");
    }

    void Start()
    {
        Preparing();
    }

    public void Preparing()
    {
        playersCount = Math.Min(playersCount, (byte)5);
        DrawDeckAndPlayers();
        StartGame();
    }

    async void StartGame()
    {
        InitializeTurn();
        deck.Fill(cardPrefabs);
        // deck.PrintDeck();
        await FillPlayersHand();

        statsManager.time = Time.time;
        statsManager.turnsCount = 0;
        statsManager.playedCardsCount = 0;

        gameState = GameState.PlayerTurn;
    }

    public async void RestartGame()
    {
        deck.cards = new();

        for (int i = 0; i < deck.transform.childCount; i++)
        {
            Transform child = deck.transform.GetChild(i);

            ICardCollectionHelper cardCollectionHelper = this;
            cardCollectionHelper.SafeDestroy(child.gameObject);
        }

        for (int i = 0; i < playersObject.transform.childCount; i++)
        {
            Transform child = playersObject.transform.GetChild(i);
            Player player = child.GetComponent<Player>();

            player.hand = new();
            player.cardsToTake = 1;
            player.turn = false;
            player.alive = true;
        }

        for (int i = 0; i < playedCards.transform.childCount; i++)
        {
            Transform child = playedCards.transform.GetChild(i);

            ICardCollectionHelper cardCollectionHelper = this;
            cardCollectionHelper.SafeDestroy(child.gameObject);
        }

        gameState = GameState.Preparing;

        StartGame();
    }

    private void DrawDeckAndPlayers()
    {
        GameObject deckObject = Instantiate(Resources.Load<GameObject>($"Prefabs/For{playersCount}Players/Deck"), transform.Find("Canvas"));

        deckObject.name = "Deck";
        deck = deckObject.GetComponent<Deck>();

        players = new();

        GameObject playerObject;

        string playerType = "Player";

        if (AITraining)
            playerType = "AIBot";

        playerObject = Instantiate(Resources.Load<GameObject>($"Prefabs/For{playersCount}Players/{playerType}_0"), playersObject.transform, false);

        playerObject.tag = playerType;
        playerObject.name = $"{playerObject.tag}_0";

        Player player = playerObject.GetComponent<Player>();
        players.Add(player);
        playerType = "AIBot";

        for (int i = 1; i < playersCount; i++)
        {
            playerObject = Instantiate(Resources.Load<GameObject>($"Prefabs/For{playersCount}Players/{playerType}_{i}"), playersObject.transform, false);

            playerObject.tag = playerType;
            playerObject.name = $"{playerObject.tag}_{i}";

            player = playerObject.GetComponent<Player>();
            players.Add(player);
        }
    }

    private void InitializeTurn()
    {
        Random rng = new();
        players[rng.Next(0, players.Count)].turn = true;
    }

    public async Task FillPlayersHand()
    {
        bool hide;

        foreach (var player in players)
        {
            hide = player != players[0];

            await player.AddDefuseCard(deck, cardPrefabs[4]);
            _ = player.hand[0].SetHidden(hide, 0.2f);

            const byte cardsToAdd = 7;

            for (byte i = 0; i < cardsToAdd; i++)
            {
                Card cardFromDeck = deck.TopCard();

                await player.AddCard(cardFromDeck.gameObject, true);
                _ = cardFromDeck.SetHidden(hide, 0.2f);
            }

            ICardCollectionHelper cardCollectionHelper = this;
            cardCollectionHelper.ShuffleList(player.hand);
            cardCollectionHelper.SetZIndexes(player.hand, true);

            player.RepositionAllCards();
        }

        deck.AddExplodingKitten(cardPrefabs[5], playersCount);
    }

    public async Task CheckAndSetNextTurn(Player turnPlayer, int nextPlayerCardsToTake = 1, bool kill = false)
    {
        gameState = GameState.Preparing;

        bool isPlayer = turnPlayer.CompareTag("Player");

        if (isPlayer)
            statsManager.turnsCount++;

        await DestroyAllPlayedCards();
        Player nextTurnPlayer = FindNextTurnPlayer(turnPlayer);

        if (!kill)
        {
            if (turnPlayer.cardsToTake > 0 && deck.cards.Count > 0)
            {
                gameState = GameState.PlayerTurn;
                return;
            }
        }
        else
        {
            turnPlayer.alive = false;

            if (!isPlayer)
                turnPlayer.GetComponent<AIAgent>().PenalizeForExploding();
            else
            {
                gameState = GameState.Finished;

                statsManager.win = false;
                statsManager.SaveStats();

                uiGameManager.ResultsScreen(statsManager.win, statsManager.turnsCount, statsManager.playedCardsCount, statsManager.time);
                return;
            }

            List<Player> alivePlayers = players.FindAll(p => p.alive);

            if (alivePlayers.Count < 2)
            {
                Debug.LogWarning($"{alivePlayers[0]} win!");

                if (alivePlayers[0].gameObject.CompareTag("AIBot"))
                    alivePlayers[0].gameObject.GetComponent<AIAgent>().RewardForWin();
                else
                {
                    gameState = GameState.Finished;

                    statsManager.win = true;
                    statsManager.SaveStats();

                    uiGameManager.ResultsScreen(statsManager.win, statsManager.turnsCount, statsManager.playedCardsCount, statsManager.time);
                    return;
                }

                for (int i = 0; i < alivePlayers[0].hand.Count; i++)
                {
                    ICardCollectionHelper cardCollectionHelper = this;
                    cardCollectionHelper.SafeDestroy(alivePlayers[0].hand[i].gameObject);
                }

                gameState = GameState.Finished;
                RestartGame();
                return;
            }
        }

        turnPlayer.cardsToTake = 1;
        turnPlayer.turn = false;

        nextTurnPlayer.turn = true;
        nextTurnPlayer.cardsToTake = nextPlayerCardsToTake;

        gameState = GameState.PlayerTurn;
    }

    public Player FindTurnPlayer() => players.Find(p => p.turn && p.alive);

    public Player FindNextTurnPlayer(Player turnplayer)
    {
        List<Player> alivePlayers = players.FindAll(p => p.alive);

        if (alivePlayers.Count < 2)
            return null;

        int index = alivePlayers.IndexOf(turnplayer);

        if (index < alivePlayers.Count - 1)
            return alivePlayers[index + 1];

        return alivePlayers[0];
    }

    public async Task CancelAllCardsToDraw()
    {
        Player player = FindTurnPlayer();

        if (player.hand.Count == 0) return;

        try
        {
            List<Card> cardsToDraw = player.hand.FindAll(card => card.CompareTag("ToDraw"));
            if (cardsToDraw == null) return;

            var task = Task.Delay(0);

            for (int i = 0; i < cardsToDraw.Count; i++)
            {
                cardsToDraw[i].tag = "InHand";
                task = CancelGetCardFromHand(cardsToDraw[i]);
            }

            await task;
        }

        catch (Exception)
        {
            Debug.LogWarning("EXCEPTION");
            return;
        }

    }

    public void SetCardToDraw(Card card)
    {
        card.tag = "ToDraw";
        GetCardFromHand(card);
    }

    public async Task PreDrawCard(Card card, bool triggeredByBot = false)
    {
        GameState oldState = gameState;
        gameState = GameState.Preparing;

        if (card.IsCardNameByItsName(Card.CardName.Nope) && playedCards.cards.Count == 0)
        {
            await CancelAllCardsToDraw();
            gameState = oldState;
            return;
        }

        if (playedCards.cards.Count == 1 && playedCards.cards.Count(c => c.IsCardNameByItsName(Card.CardName.Nope)) > 0)
        {
            await DestroyAllPlayedCards();
        }

        Player player = card.GetComponentInParent<Player>();
        List<Card> cardsToDraw = player.hand.FindAll(cardInHand => cardInHand.CompareTag("ToDraw"));

        for (int i = 0; i < cardsToDraw.Count; i++)
        {
            player.RemoveCard(cardsToDraw[i]);
            await cardsToDraw[i].SetHidden(false, 0.2f);
            await playedCards.AddCard(cardsToDraw[i].gameObject);

            if (!triggeredByBot)
                statsManager.playedCardsCount++;
        }

        await SetNotTransparent(player);

        if (oldState == GameState.NopeAwaiting)
        {
            gameState = oldState;
            _ = NopeAwaiter().ConfigureAwait(false);
            return;
        }

        gameState = GameState.NopeAwaiting;
        int playedCardsCount = playedCards.cards.Count;

        for (int i = 10; i > 0; i--)
        {
            await Task.Delay(300);

            if (playedCardsCount != playedCards.cards.Count && gameState == GameState.NopeAwaiting)
            {
                playedCardsCount = playedCards.cards.Count;
                i = 10;
            }
        }

        _ = NopeAwaiter().ConfigureAwait(false);

        if (gameState == GameState.NopeAwaiting)
            gameState = GameState.Preparing;
        else
        {
            gameState = GameState.Preparing;

            if (playedCards.cards.Count > 0)
            {
                if (playedCards.cards[0].IsCardNameByItsName(Card.CardName.Nope))
                {
                    await DestroyAllPlayedCards();
                    gameState = GameState.PlayerTurn;
                    return;
                }
            }
        }

        playedCardsCount = playedCards.cards.Count;
        print(playedCards.cards.Count - playedCards.transform.childCount);

        if ((playedCardsCount - cardsToDraw.Count) % 2 != 0)
        {
            await DestroyAllPlayedCards();

            if (triggeredByBot)
                player.GetComponent<AIAgent>().lastPlayedCard = null;

            gameState = oldState;
            return;
        }

        await DrawCard(player, cardsToDraw[0], triggeredByBot);
    }

    public async Task DrawCard(Player turnPlayer, Card cardToDraw, bool triggeredByBot = false)
    {
        Task task = Task.Delay(0);

        switch (cardToDraw.cardName)
        {
            case Card.CardName.Attack:
                turnPlayer.cardsToTake--;

                int nextPlayerCardsToTake = turnPlayer.cardsToTake + 2;
                turnPlayer.cardsToTake = 0;

                task = CheckAndSetNextTurn(turnPlayer, nextPlayerCardsToTake);
                break;

            case Card.CardName.Favor:
                gameState = GameState.DrawFavor;
                task = DestroyAllPlayedCards();
                await task;

                Player attackedPlayer = FindNextTurnPlayer(turnPlayer);

                if (attackedPlayer.hand.Count != 0 && attackedPlayer != null)
                {
                    foreach (var card in attackedPlayer.hand)
                        card.tag = "ToSteal";
                }
                else
                    gameState = GameState.PlayerTurn;
                break;

            case Card.CardName.SeeTheFuture:
                task = DestroyAllPlayedCards();

                if (triggeredByBot)
                {
                    AIAgent aiAgent = turnPlayer.GetComponent<AIAgent>();
                    aiAgent.seeTheFutureCards = deck.PeekTopThreeCards();
                }
                else
                {
                    List<Card> seeTheFutureCards = deck.PeekTopThreeCards();

                    for (int i = 0; i < seeTheFutureCards.Count; i++)
                    {
                        seeTheFutureCards[i].transform.DOMoveX(turnPlayer.transform.position.x + 1.8f * (i - 1), 0.2f).Play();
                        seeTheFutureCards[i].transform.DOMoveY(0, 0.2f).Play();
                        seeTheFutureCards[i].transform.SetParent(null, true);

                        await seeTheFutureCards[i].transform.DOLocalRotate(new(
                            turnPlayer.transform.localEulerAngles.x,
                            seeTheFutureCards[i].transform.localEulerAngles.y,
                            turnPlayer.transform.localEulerAngles.z), 0.2f).Play().AsyncWaitForCompletion();

                        await seeTheFutureCards[i].SetHidden(false, 0.2f);
                    }

                    await Task.Delay(3000);

                    for (int i = seeTheFutureCards.Count - 1; i >= 0; i--)
                    {
                        seeTheFutureCards[i].transform.SetParent(deck.transform, true);
                        seeTheFutureCards[i].transform.DOLocalMove(Vector3.zero, 0.2f).Play();

                        await seeTheFutureCards[i].transform.DOLocalRotate(new(0f, seeTheFutureCards[i].transform.localEulerAngles.y, 0f), 0.2f).Play().AsyncWaitForCompletion();
                        await seeTheFutureCards[i].SetHidden(true, 0.2f);
                    }
                }
                break;

            case Card.CardName.Shuffle:
                task = DestroyAllPlayedCards();
                await task;

                deck.SetFirstThreeCardsActive(false);

                ICardCollectionHelper cardCollectionHelper = this;
                cardCollectionHelper.ShuffleList(deck.cards);
                cardCollectionHelper.SetZIndexes(deck.cards);

                deck.SetFirstThreeCardsActive();

                UpdateSeeTheFutureLists(true);
                break;

            case Card.CardName.Skip:
                turnPlayer.cardsToTake--;
                task = CheckAndSetNextTurn(turnPlayer);
                break;

            case Card.CardName.BeardCat:
            case Card.CardName.CatsSchrodinger:
            case Card.CardName.CatterMelon:
            case Card.CardName.HairyPotatoCat:
            case Card.CardName.TacoCat:
                task = DestroyAllPlayedCards();
                await task;

                attackedPlayer = FindNextTurnPlayer(turnPlayer);

                if (attackedPlayer.hand.Count != 0 && attackedPlayer != null)
                {
                    bool hide = turnPlayer.CompareTag("AIBot") || attackedPlayer.CompareTag("Player");
                    Card takenCard = attackedPlayer.RemoveCard(attackedPlayer.hand[UnityEngine.Random.Range(0, attackedPlayer.hand.Count)]);

                    await takenCard.SetHidden(hide, 0.2f);
                    await turnPlayer.AddCard(takenCard.gameObject, true);
                }
                else
                    gameState = GameState.PlayerTurn;
                break;

            default:
                task = DestroyAllPlayedCards();
                return;
        }

        await task;

        if (gameState != GameState.DrawFavor)
            gameState = GameState.PlayerTurn;
    }

    public async Task GetCardFromDeck()
    {
        gameState = GameState.Preparing;

        Player turnPlayer = FindTurnPlayer();
        Card cardFromDeck = deck.TopCard();

        bool hide = !turnPlayer.CompareTag("Player");
        bool kill = false;

        UpdateSeeTheFutureLists();

        turnPlayer.cardsToTake--;

        if (cardFromDeck.IsExplodingKitten())
        {
            await turnPlayer.AddCard(cardFromDeck.gameObject);
            await cardFromDeck.SetHidden(false, 0.2f);

            Card defuseCard = turnPlayer.hand.Find(card => card.IsDefuse());

            if (defuseCard != null)
            {
                await defuseCard.transform.DOLocalMove(new(
                    cardFromDeck.transform.localPosition.x,
                    cardFromDeck.transform.localPosition.y,
                    cardFromDeck.transform.localPosition.z - 1f), 0.2f).Play().AsyncWaitForCompletion();

                turnPlayer.RemoveCard(defuseCard, false);
                await DestroyCardAnimation(defuseCard);

                turnPlayer.RemoveCard(cardFromDeck);
                await deck.ReturnExplodingKitten(cardFromDeck, cardPrefabs[5]);

                UpdateSeeTheFutureLists(true);
            }
            else
            {
                await DoColorAndDoScaleSequence(cardFromDeck);

                cardFromDeck.GetComponent<Explosion>().Explode();

                turnPlayer.RemoveCard(cardFromDeck, false);
                Task lastDestroyCardAnimationTask = DestroyCardAnimation(cardFromDeck);

                if (turnPlayer.hand.Count != 0)
                {
                    int halfHandCount = turnPlayer.hand.Count / 2;
                    int negativeIterations = 0;

                    _ = DestroyCardAnimation(turnPlayer.hand[halfHandCount]);

                    if (turnPlayer.hand.Count != 0)
                    {
                        if (turnPlayer.hand.Count % 2 == 0)
                            negativeIterations = 1;

                        if (turnPlayer.hand.Count > 1)
                        {
                            for (int i = 1; i <= halfHandCount - negativeIterations; i++)
                            {
                                _ = DestroyCardAnimation(turnPlayer.hand[halfHandCount + i]);
                                lastDestroyCardAnimationTask = DestroyCardAnimation(turnPlayer.hand[halfHandCount - i]);
                                await Task.Delay(5);
                            }

                            if (negativeIterations == 1)
                                lastDestroyCardAnimationTask = DestroyCardAnimation(turnPlayer.hand[0]);
                        }

                        else lastDestroyCardAnimationTask = DestroyCardAnimation(turnPlayer.hand[0]);

                    }
                }

                kill = true;
                await lastDestroyCardAnimationTask;
            }
        }
        else
        {
            await turnPlayer.AddCard(cardFromDeck.gameObject, true);
            await cardFromDeck.SetHidden(hide, 0.2f);
        }

        await CheckAndSetNextTurn(turnPlayer, kill: kill);
    }

    public async Task DestroyCardAnimation(Card card)
    {
        if (card == null) return;

        SpriteRenderer spriteRenderer = card.GetComponent<SpriteRenderer>();
        TextMeshPro[] texts = card.GetComponentsInChildren<TextMeshPro>();

        foreach (var text in texts)
            text.DOColor(Color.clear, 0.3f).Play();

        await spriteRenderer.DOColor(Color.clear, 0.3f).Play().AsyncWaitForCompletion();

        ICardCollectionHelper cardCollectionHelper = this;
        if (card != null)
            cardCollectionHelper.SafeDestroy(card.gameObject);
    }

    public async Task DoColorAndDoScaleSequence(Card explodingKitten)
    {
        Transform cardTransform = explodingKitten.transform;
        SpriteRenderer cardSprite = explodingKitten.GetComponent<SpriteRenderer>();
        Vector3 originalScale = cardTransform.localScale;

        float duration = 0.3f;
        float strength = 1.05f;
        for (int i = 0; i < 10; i++)
        {
            Sequence sequence = DOTween.Sequence();

            Vector3 targetScale = originalScale * strength;

            sequence.Append(cardTransform.DOScale(targetScale, duration / 2).SetEase(Ease.OutQuad));
            sequence.Join(cardSprite.DOColor(new Color(1f, 0.44f, 0f), duration / 2).SetEase(Ease.Linear));

            sequence.Append(cardTransform.DOScale(originalScale, duration / 2).SetEase(Ease.InQuad));
            sequence.Join(cardSprite.DOColor(new Color(1f, 1f, 1f), duration / 2).SetEase(Ease.Linear));

            await sequence.Play().AsyncWaitForCompletion();

            duration -= 0.008f * i;
            strength += 0.01f * i;
        }
    }

    public bool HandleCardDraw(Card card)
    {
        if (card.IsDefuse() || card.IsExplodingKitten() || card.IsCardNameByItsName(Card.CardName.Nope)) return false;

        Player player = card.transform.GetComponentInParent<Player>();
        List<Card> hand = player.hand;

        if (card.IsCatCard())
        {
            Card secondCard = hand.Find(secondCard => secondCard != card && secondCard.cardName == card.cardName);

            if (!secondCard) return false;

            SetCardToDraw(secondCard);
        }

        SetCardToDraw(card);

        if (player.CompareTag("Player"))
        {
            hand = hand.FindAll(c => c.CompareTag("InHand"));
            foreach (var c in hand)
            {
                c.GetComponent<SpriteRenderer>().DOColor(new(0.75f, 0.75f, 0.75f, 1f), 0.1f).Play();
                c.tag = "Transparent";
            }
        }

        return true;
    }

    public void GetCardFromHand(Card card)
    {
        Transform cardTransform = card.transform;

        if (cardTransform)
        {
            Player player = card.GetComponentInParent<Player>();
            int zOffset = 0;
            if (card.IsCatCard())
                zOffset = player.hand.FindAll(cardToDraw => cardToDraw.CompareTag("ToDraw")).Count;

            cardTransform.localPosition = new(
                cardTransform.localPosition.x,
                cardTransform.localPosition.y,
                -player.hand.Count - zOffset);

            card.TweenDoLocalMoveY();
        }
    }

    public async Task CancelGetCardFromHand(Card card)
    {
        Player player = card.transform.GetComponentInParent<Player>();
        await SetNotTransparent(player);

        if (!player.turn)
        {
            if (card.doLocalMoveY == null) return;

            if (card.doLocalMoveY.IsPlaying())
            {
                card.doLocalMoveY.Complete();
                card.doLocalMoveY.SetAutoKill(true);
            }
        }

        Transform cardTransform = card.transform;

        if (cardTransform)
        {
            cardTransform.localPosition = new(
                cardTransform.localPosition.x,
                cardTransform.localPosition.y,
                card.zIndex);

            card.doLocalMoveY.PlayBackwards();
            card.doLocalMoveY.SetAutoKill(true);
        }
    }

    public async Task SetNotTransparent(Player player)
    {
        if (!player.alive) return;
        List<Card> transpCards = player.hand.FindAll(c => c.CompareTag("Transparent"));

        var task = Task.Delay(0);

        foreach (var c in transpCards)
        {
            task = c.GetComponent<SpriteRenderer>().DOColor(new(1, 1, 1, 1), 0.1f).Play().AsyncWaitForCompletion();
            c.tag = "InHand";
        }

        await task;
    }

    public async Task GiveCard(Card card)
    {
        gameState = GameState.Preparing;

        Player attackedPlayer = card.GetComponentInParent<Player>();
        Player turnPlayer = FindTurnPlayer();

        bool hide = turnPlayer.CompareTag("AIBot") || attackedPlayer.CompareTag("Player");

        await card.SetHidden(hide, 0.2f);
        attackedPlayer.RemoveCard(card);

        await turnPlayer.AddCard(card.gameObject, true);

        foreach (var c in attackedPlayer.hand)
            c.tag = "InHand";

        gameState = GameState.PlayerTurn;
    }

    public void UpdateSeeTheFutureLists(bool clearLists = false)
    {
        List<Player> aiBotsWithSeeTheFutureCardsLists = players.FindAll(p => p.CompareTag("AIBot") && p.GetComponent<AIAgent>().seeTheFutureCards.Count > 0);

        if (clearLists)
            foreach (var aiBot in aiBotsWithSeeTheFutureCardsLists)
                aiBot.GetComponent<AIAgent>().seeTheFutureCards = new();
        else
            foreach (var aiBot in aiBotsWithSeeTheFutureCardsLists)
                aiBot.GetComponent<AIAgent>().seeTheFutureCards.RemoveAt(0);
    }

    public async Task DestroyAllPlayedCards()
    {
        GameState oldState = gameState;
        gameState = GameState.Preparing;

        var task = Task.Delay(0);

        for (int i = 0; i < playedCards.transform.childCount; i++)
        {
            playedCards.RemoveCard(playedCards.transform.GetChild(i).GetComponent<Card>());
            task = DestroyCardAnimation(playedCards.transform.GetChild(i).GetComponent<Card>());
        }

        await task;
        gameState = oldState;
        print("Cleared down");
    }

    public async Task NopeAwaiter()
    {
        for (int i = 0; i < 500; i++)
        {
            await Task.Delay(10);

            if (gameState != GameState.NopeAwaiting)
                return;
        }

        await DestroyAllPlayedCards();
        gameState = GameState.PlayerTurn;
    }
}