using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerBoardManager : MonoBehaviour {
    
    private const float COUNT_DOWN_WAITTIME = 0.2f;

    public GameObject m_Prefab;

    public void SetPlayerBoard(int num, int hp = 20)
    {
        for (int i = 0; i < num; i++)
        {
            GameObject prefab = Instantiate(m_Prefab);
            Text[] prefabText = prefab.gameObject.GetComponentsInChildren<Text>();
            prefabText[0].text = SetPlayerName(i);
            prefabText[1].text = hp.ToString();
            prefab.name = SetPlayerName(i);
            prefab.transform.SetParent(gameObject.transform, false);
        }
    }

    public IEnumerator AddPlayerScore(int beforeScore, int Afterscore, int playerNo)
    {
        string playerName = "player" + playerNo;
        GameObject player = gameObject.transform.Find(playerName).gameObject;
        Text[] playerScoreText = player.gameObject.GetComponentsInChildren<Text>();
        for (int i = beforeScore; i > Afterscore; i--)
        {
            yield return ScoreCountDown(playerScoreText[1], i - 1);
        }
    }

    private IEnumerator ScoreCountDown(Text playerText, int score)
    {
        playerText.text = score.ToString();
        yield return new WaitForSeconds(COUNT_DOWN_WAITTIME);
    }

    string SetPlayerName(int index){return "player" + (index + 1);}
}
