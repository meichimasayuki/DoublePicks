using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerBoardManager : MonoBehaviour {

    private const int MAX_PLAYING_NUM = 8;
    private const float COUNT_DOWN_WAITTIME = 0.2f;

    public GameObject m_Prefab;

    [System.SerializableAttribute]
    public class TextureList
    {
        public Texture2D not_Texture;
        public Texture2D off_Texture;
        public Texture2D on_Texture;
        private List<Texture2D> m_TextureList = new List<Texture2D>();
        public TextureList(){}
        public void SetTexture()
        {
            if (not_Texture) { m_TextureList.Add(not_Texture); }
            if (off_Texture) { m_TextureList.Add(off_Texture); }
            if (on_Texture) { m_TextureList.Add(on_Texture); }
        }
        public Texture2D GetTexture(int type)
        {
            return m_TextureList[type];
        }
    };
    [SerializeField]
    private TextureList[] m_PlayerTexture = new TextureList[MAX_PLAYING_NUM];

    private List<GameObject> m_BourdList = new List<GameObject>();

    // 最初
    public void SetPlayerBoard(int num, int hp = 20)
    {
        for (int i = 0; i < MAX_PLAYING_NUM; i++)
        {
            m_PlayerTexture[i].SetTexture();
            Texture2D texture = m_PlayerTexture[i].GetTexture(1);// MG　気を付けて
            GameObject prefab = Instantiate(m_Prefab);
            Image img = prefab.GetComponent<Image>();
            if (i < num)
            {
                texture = m_PlayerTexture[i].GetTexture(0);// MG　気を付けて

                Text[] prefabText = prefab.gameObject.GetComponentsInChildren<Text>();
                prefabText[0].text = hp.ToString();
            }
            prefab.name = SetPlayerName(i);
            img.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            prefab.transform.SetParent(gameObject.transform, false);
            if (i < num) m_BourdList.Add(prefab);
        }
    }

    // 行動者のアイコン変更
    public void NextTrunPlayer(int turn)
    {
        int target = turn % m_BourdList.Count;
        for (int i = 0; i< m_BourdList.Count; i++)
        {
            Texture2D texture = m_PlayerTexture[i].GetTexture(0);// MG　気を付けて
            Image img = m_BourdList[i].GetComponent<Image>();
            if(i == target)
            {
                texture = m_PlayerTexture[i].GetTexture(2);// MG　気を付けて
            }
            img.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }
    }

    // プレイヤーのスコア更新
    public IEnumerator UpdatePlayerScore(int beforeScore, int Afterscore, int target)
    {
        Text[] playerScoreText = m_BourdList[target].gameObject.GetComponentsInChildren<Text>();
        for (int i = beforeScore; i > Afterscore; i--)
        {
            yield return ScoreCountDown(playerScoreText[0], i - 1);
        }
    }

    // カウントダウン
    private IEnumerator ScoreCountDown(Text playerText, int score)
    {
        playerText.text = score.ToString();
        yield return new WaitForSeconds(COUNT_DOWN_WAITTIME);
    }

    string SetPlayerName(int index){return "player" + (index + 1);}
}
