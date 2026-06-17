using UnityEngine;

public class StatsManager : MonoBehaviour
{
    public float time = 0;
    public int turnsCount = 0, playedCardsCount = 0;
    public bool win;

    public void SaveStats()
    {
        time = Time.time - time;
        int playedSessionsCount = PlayerPrefs.GetInt("PlayedSessions");
        float cardsPerTurnCount = (float)playedCardsCount / turnsCount;

        float totalTime = PlayerPrefs.GetFloat("AvgTime") * playedSessionsCount;
        float totalTurns = PlayerPrefs.GetFloat("AvgTurns") * playedSessionsCount;
        float totalPlayedCards = PlayerPrefs.GetFloat("AvgPlayedCards") * playedSessionsCount;
        float totalCardsPerTurn = PlayerPrefs.GetFloat("AvgCardsPerTurn") * playedSessionsCount;

        playedSessionsCount++;
        PlayerPrefs.SetInt("PlayedSessions", playedSessionsCount);

        PlayerPrefs.SetFloat("AvgTime", (totalTime + time) / playedSessionsCount);
        PlayerPrefs.SetFloat("AvgTurns", (totalTurns + turnsCount) / playedSessionsCount);
        PlayerPrefs.SetFloat("AvgPlayedCards", (totalPlayedCards + playedCardsCount) / playedSessionsCount);
        PlayerPrefs.SetFloat("AvgCardsPerTurn", (totalCardsPerTurn + cardsPerTurnCount) / playedSessionsCount);

        if (win)
            PlayerPrefs.SetInt("WinSessions", PlayerPrefs.GetInt("WinSessions") + 1);
        else
            PlayerPrefs.SetInt("LoseSessions", PlayerPrefs.GetInt("LoseSessions") + 1);

        int winSessions = PlayerPrefs.GetInt("WinSessions");
        PlayerPrefs.SetFloat("WinPercentage", (winSessions > 0 ? (float)winSessions / playedSessionsCount : 0) * 100);
    }
}
