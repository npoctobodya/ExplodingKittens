using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

public class AIAgent : Agent
{
    public GameManager gameManager;
    public Player player;
    public Player attackedPlayer;
    public List<Card> playableCards;
    public List<Card> seeTheFutureCards;
    public bool actionInProgress = false;
    public Card lastPlayedCard; // ссылка на последнюю сыгранную карту
    public float actionsCount = 0;
    public float avgActions = 0;
    public float sumActions = 0;
    public float gamesCount = 0;

    void Start()
    {
        gameManager = GetTopParent(transform).gameObject.GetComponent<GameManager>();
    }
    protected override void Awake()
    {
        gameManager = GetTopParent(transform).gameObject.GetComponent<GameManager>();
    }
    public override void Initialize()
    {
        player = GetComponent<Player>();
        gameManager = GetTopParent(transform).gameObject.GetComponent<GameManager>();
        attackedPlayer = player;
        seeTheFutureCards = new();
    }

    public Transform GetTopParent(Transform target)
    {
        Transform current = target;
        while (current.parent != null)
            current = current.parent;

        return current;
    }

    public override void OnEpisodeBegin()
    {
        actionInProgress = false;
        actionsCount = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        playableCards = player.hand.FindAll(c => !c.IsDefuse() && !c.IsCardNameByItsName(Card.CardName.Nope) && !c.IsCatCard());

        if (player.hand.Find(c => c.IsCatCard()) != null)
        {
            var catGroups = player.hand.Where(c => c.IsCatCard()).GroupBy(c => c.cardName);
            List<Card> catPairs = catGroups.Where(g => g.Count() >= 2).SelectMany(g => g).ToList();

            if (catPairs.Count > 0)
                playableCards.AddRange(catPairs);
        }

        attackedPlayer = gameManager.FindNextTurnPlayer(player);

        // Количество карт в руке
        sensor.AddObservation(player.hand.Count / 15f);
        sensor.AddObservation(player.hand.Count(c => c.IsDefuse()) / 4f);

        // Количество играбельных карт
        sensor.AddObservation(playableCards.Count / 15f);
        sensor.AddObservation(player.hand.Count(c => c.IsCardNameByItsName(Card.CardName.Attack)) / 4f);
        sensor.AddObservation(player.hand.Count(c => c.IsCardNameByItsName(Card.CardName.Favor)) / 4f);
        sensor.AddObservation(player.hand.Count(c => c.IsCardNameByItsName(Card.CardName.Nope)) / 4f);
        sensor.AddObservation(player.hand.Count(c => c.IsCardNameByItsName(Card.CardName.Shuffle)) / 4f);
        sensor.AddObservation(player.hand.Count(c => c.IsCardNameByItsName(Card.CardName.Skip)) / 4f);
        sensor.AddObservation(player.hand.Count(c => c.IsCardNameByItsName(Card.CardName.SeeTheFuture)) / 4f);

        // Количество карт у атакуемого игрока
        if (attackedPlayer != null)
            sensor.AddObservation(attackedPlayer.hand.Count / 15f);
        else
            sensor.AddObservation(0f);

        // Количество карт в колоде
        sensor.AddObservation(gameManager.deck.cards.Count / 50f);
        sensor.AddObservation(gameManager.players.Count(p => p.alive) / 5f);

        // Информация о последней сыгранной карте
        if (lastPlayedCard != null)
        {
            sensor.AddObservation(gameManager.playedCards.cards.Contains(lastPlayedCard) ? 1f : 0f);

            // Информация о типе последней сыгранной карты
            sensor.AddObservation(lastPlayedCard.IsCatCard() ? 1f : (float)lastPlayedCard.cardName / 7f);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        // Прогноз опасности (взрывной котёнок вверху колоды)
        if (seeTheFutureCards.Count > 0)
            sensor.AddObservation(seeTheFutureCards[0].IsExplodingKitten() ? 1f : 0f);
        else
        {
            int kittensLeft = gameManager.deck.cards.Count(c => c.IsExplodingKitten());
            float dangerLevel = gameManager.deck.cards.Count > 0 ? (float)kittensLeft / gameManager.deck.cards.Count : 0f;
            sensor.AddObservation(dangerLevel);
        }

        // Состояние игры
        sensor.AddObservation((float)gameManager.gameState / 6f);
        sensor.AddObservation(player.turn ? 1f : 0f);

        // Информация о необходимости взять карты, если был применён атак
        sensor.AddObservation(player.cardsToTake / 5f);
    }

    public override async void OnActionReceived(ActionBuffers actions)
    {
        if (actionInProgress) return;
        if (!player.alive) return;
        if (attackedPlayer == null) return;

        actionInProgress = true;

        // Выбор действия в зависимости от текущего состояния игры
        switch (gameManager.gameState)
        {
            case GameManager.GameState.PlayerTurn:
                if (player.turn)
                    HandlePlayerTurnActions(actions);
                else
                    actionInProgress = false;
                break;

            case GameManager.GameState.NopeAwaiting:
                Card nopeCard = player.hand.Find(c => c.IsCardNameByItsName(Card.CardName.Nope));

                if (nopeCard != null)
                    await HandleNopeAwaitingActions(actions, nopeCard);
                else
                    actionInProgress = false;
                break;

            case GameManager.GameState.DrawFavor:
                if (player == gameManager.FindNextTurnPlayer(gameManager.FindTurnPlayer()))
                {
                    if (player.hand.Count == 0)
                    {
                        actionInProgress = false;
                        return;
                    }

                    Card cardToGive = player.hand.Find(c => c.IsCatCard());

                    for (int cardname = (int)Card.CardName.Favor; cardToGive == null; cardname--)
                        cardToGive = player.hand.Find(c => c.IsCardNameByItsName((Card.CardName)cardname));

                    StartCoroutine(GiveFavorCardCoroutine(cardToGive));
                }
                else
                    actionInProgress = false;
                break;

            default:
                actionInProgress = false;
                break;
        }
    }

    private void AddRewardForPlayCard(bool playCard = true)
    {
        if (playCard)
        {
            AddReward(MathF.Pow(MathF.Abs(40 - gameManager.deck.cards.Count), 1.5f) * 0.02f);

            if (playableCards.Count > 4)
                AddReward(MathF.Pow(playableCards.Count, 2f) * 0.02f);

            if (gameManager.deck.cards.Count <= gameManager.players.Count(p => p.alive))
                AddReward(0.5f);
        }
        else
        {
            if (gameManager.deck.cards.Count <= gameManager.players.Count(p => p.alive))
            {
                if (playableCards.Count > 0)
                    AddReward(-MathF.Pow(playableCards.Count, 2f) * 0.5f);
                else
                    AddReward(0.2f);
            }
            else
            {
                if (player.hand.Count > 5)
                    AddReward(-MathF.Pow(player.hand.Count, 1.5f) * 0.1f);
                else
                    AddReward(0.1f);
            }
        }

    }

    // ============ Обработка действий в ход игрока ============
    private void HandlePlayerTurnActions(ActionBuffers actions)
    {
        int actionType = actions.DiscreteActions[0];
        int behaviour = actions.DiscreteActions[1]; // 0 - сбросить; 1 - атаковать

        switch (actionType)
        {
            case 0: // Взять карту из колоды
                if (gameManager.deck.cards.Count > 0)
                    StartCoroutine(TakeCardCoroutine());
                else
                    actionInProgress = false;
                break;

            case 1: // Сыграть карту
                if (playableCards.Count == 0)
                {
                    AddReward(-1f);
                    actionInProgress = false;
                    return;
                }

                Card cardToPlay = null;

                switch (behaviour)
                {
                    case 0:
                        cardToPlay = playableCards.Find(c => c.IsCardNameByItsName(Card.CardName.Shuffle) || c.IsCardNameByItsName(Card.CardName.Skip));

                        if (cardToPlay == null && seeTheFutureCards.Count != Math.Min(gameManager.deck.cards.Count, 3))
                            cardToPlay = playableCards.Find(c => c.IsCardNameByItsName(Card.CardName.SeeTheFuture));

                        if (cardToPlay != null)
                            StartCoroutine(PlayCardCoroutine(cardToPlay));
                        else
                        {
                            AddReward(-0.1f);
                            actionInProgress = false;
                        }
                        break;

                    case 1:
                        cardToPlay = playableCards.Find(c => c.IsCardNameByItsName(Card.CardName.Attack) || c.IsCardNameByItsName(Card.CardName.Favor));

                        if (cardToPlay == null)
                        {
                            cardToPlay = playableCards.Find(c => c.IsCatCard());

                            if (cardToPlay != null)
                            {
                                if (!TryPlayCatCombo())
                                    AddReward(-0.5f);
                                break;
                            }
                            else
                            {
                                if (seeTheFutureCards.Count != Math.Min(gameManager.deck.cards.Count, 3))
                                    cardToPlay = playableCards.Find(c => c.IsCardNameByItsName(Card.CardName.SeeTheFuture));
                            }
                        }

                        if (cardToPlay != null)
                            StartCoroutine(PlayCardCoroutine(cardToPlay));
                        else
                        {
                            AddReward(-0.1f);
                            actionInProgress = false;
                        }
                        break;
                }
                break;
        }
    }

    private async Task HandleNopeAwaitingActions(ActionBuffers actions, Card nopeCard)
    {
        int useNope = actions.DiscreteActions[0]; // 0 - не использовать, 1 - использовать

        if (useNope == 1)
        {
            // Проверяем, можно ли сыграть Nope на последнюю карту
            if (gameManager.playedCards.cards.Contains(lastPlayedCard))
            {
                // Играем Nope на свою карту
                AddReward(-1f);
                Debug.LogWarning($"{player.name}: Ошибка! Попытка отменить свою карту {lastPlayedCard.cardName}");
            }
            else if (gameManager.playedCards.cards.Count > 0)
            {
                Card lastCard = gameManager.playedCards.cards[^1];

                if (lastCard.cardName <= Card.CardName.Nope)
                    AddReward(-((int)Card.CardName.Nope - (int)lastCard.cardName + 1) * 0.1f);
                else
                {
                    if (lastCard.IsCatCard())
                        AddReward(0.5f);
                    else
                        AddReward(0.1f);
                }

                Debug.Log($"{player.name}: Использовал Nope на карту {lastCard.name}");
            }
            else
            {
                print("@@!!!!!!!!");
            }

            lastPlayedCard = nopeCard;
            StartCoroutine(PlayNopeCoroutine(nopeCard));
        }
        else
        {
            // Отказываемся от использования Nope
            AddReward(0.1f); // Небольшое поощрение за отказ
            await gameManager.CancelAllCardsToDraw();
            actionInProgress = false;
        }
    }

    // ============ Корутины для действий ============
    private IEnumerator TakeCardCoroutine()
    {
        if (seeTheFutureCards.Count > 0)
        {
            if (seeTheFutureCards[0].IsExplodingKitten() && gameManager.deck.cards.Count > 1)
            {
                if (playableCards.Count > 0)
                {
                    Debug.LogWarning($"{player.name} знал о взрывном котёнке и всё равно взял!");
                    AddReward(-5f);
                }

            }
        }

        var task = gameManager.GetCardFromDeck();
        yield return new WaitUntil(() => task.IsCompleted);

        AddRewardForPlayCard(false);

        actionsCount++;
        actionInProgress = false;
    }

    private IEnumerator PlayCardCoroutine(Card card)
    {
        if (!gameManager.HandleCardDraw(card))
        {
            AddReward(-1f);
            actionInProgress = false;
            yield break;
        }

        if (card.IsCatCard() || card.IsCardNameByItsName(Card.CardName.Favor))
        {
            if (attackedPlayer.hand.Count == 0)
            {
                Debug.LogWarning($"{name} попытался сыграть карту {card.name} на игрока {attackedPlayer.name}");
                AddReward(-5f);
                actionInProgress = false;
                yield break;
            }
        }

        lastPlayedCard = card;
        // Вызов PreDrawCard
        var task = gameManager.PreDrawCard(card, true);
        yield return new WaitUntil(() => task.IsCompleted);
        AddRewardForPlayCard();

        if (lastPlayedCard != null)
        {
            switch (card.cardName)
            {
                case Card.CardName.Attack:
                    if (CheckPeekCardForExplodingKitten())
                        AddReward(0.5f);
                    break;

                case Card.CardName.Shuffle:
                    if (gameManager.deck.cards.Count > 0)
                        if (CheckPeekCardForExplodingKitten())
                            AddReward(0.5f);
                    break;

                case Card.CardName.Skip:
                    if (player.cardsToTake == 0)
                        if (CheckPeekCardForExplodingKitten())
                            AddReward(0.5f);
                    break;

                default:
                    AddReward(0.1f);
                    break;
            }

        }

        actionsCount++;
        actionInProgress = false;
    }

    private bool CheckPeekCardForExplodingKitten()
    {
        var nextCard = gameManager.deck.PeekTopCard();

        if (nextCard != null)
            if (nextCard.IsExplodingKitten())
                return true;

        return false;
    }

    private IEnumerator PlayNopeCoroutine(Card nopeCard)
    {
        gameManager.SetCardToDraw(nopeCard);

        var task = gameManager.PreDrawCard(nopeCard, true);
        yield return new WaitUntil(() => task.IsCompleted);

        actionsCount++;
        actionInProgress = false;
    }

    private IEnumerator GiveFavorCardCoroutine(Card cardToGive)
    {
        var task = gameManager.GiveCard(cardToGive);
        yield return new WaitUntil(() => task.IsCompleted);

        Debug.Log($"{player.name} отдал карту {cardToGive.cardName} по требованию Favor");
        actionsCount++;
        actionInProgress = false;
    }

    private bool TryPlayCatCombo()
    {
        var catGroups = player.hand.Where(c => c.IsCatCard()).GroupBy(c => c.cardName);
        var pairGroup = catGroups.FirstOrDefault(g => g.Count() >= 2);

        if (pairGroup != null)
        {
            Card catCard = pairGroup.First();
            StartCoroutine(PlayCardCoroutine(catCard));
            return true;
        }

        return false;
    }

    // ============ Методы для связи с GameManager ============
    public void PenalizeForExploding()
    {
        AddReward(-playableCards.Count);

        gamesCount++;
        sumActions += actionsCount;
        avgActions = sumActions / gamesCount;

        Debug.LogWarning($"Reward for {player.name} {GetCumulativeReward()}\nTurns: {actionsCount}\tAVG Turns: {avgActions}");
        EndEpisode();
    }

    public void RewardForWin()
    {
        AddReward(10f);
        gamesCount++;
        sumActions += actionsCount;
        avgActions = sumActions / gamesCount;

        Debug.LogWarning($"Reward for {player.name} {GetCumulativeReward()}\nTurns: {actionsCount}\tAVG Turns: {avgActions}");
        EndEpisode();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        return;
    }
}