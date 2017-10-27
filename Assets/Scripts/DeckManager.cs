using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour {

    public GameObject m_Card;
    public Sprite[] m_CardType = new Sprite[10];

    private const int CARD_TYPE_NUM = 10;
    private const int CARD_MAX_NUM = 60;
    
    // ９とDoubleに関してのみ例外
    private const int CARD_9 = 8;
    private const int CARD_DOUBLE = 9;
    public int CARD_9_NUM = 13; //private const int CARD_9_NUM = 13;
    public int CARD_DOUBLE_NUM = 3;//private const int CARD_DOUBLE_NUM = 3;

    private List<int> m_Order = new List<int>();
    
    private int m_FieldNum = 0; 

    private class Card
    {
        private Sprite m_Sprite;
        private int m_Index;

        public Card(Sprite sprite, int index)
        {
            m_Sprite = sprite;
            m_Index = index + 1;
        }
        ~Card() { }
        public Sprite GetCardSprite() { return m_Sprite; }
        public int GetCardIndex() { return m_Index; }
    };
    private List<Card> m_CardList = new List<Card>();

    // デッキの作成
    public void CreateDeck()
    {
        for (int type = 0; type < CARD_TYPE_NUM; type++)
        {
            if (type == CARD_9)
            {
                for (int i = 0; i < CARD_9_NUM; i++) { CreateCard(type); }
            }
            else if (type == CARD_DOUBLE)
            {
                for (int i = 0; i < CARD_DOUBLE_NUM; i++) { CreateCard(type); }
            }
            else
            {
                for (int i = 0; i < type + 2; i++) { CreateCard(type); }
            }
        }
    }

    // カードの作成、追加
    private void CreateCard(int type)
    {
        Card card = new Card(m_CardType[type], type);
        m_CardList.Add(card);
    }

    // カードの順番を決めます
    public void Shuffle(int first_card)
    {
        for (int num = 0; num < CARD_MAX_NUM;)
        {
            int random = Random.Range(0, CARD_MAX_NUM);
            if (num == first_card && random >= CARD_MAX_NUM - CARD_DOUBLE_NUM) continue;     // 一番最初にDoubleカードが来ないように  
            bool issame = false;
            for (int conut = 0; conut < m_Order.Count; conut++)
            {
                if (m_Order[conut] == random) { issame = true; break; }
            }
            if (issame) continue;
            m_Order.Add(random);
            num++;
        }
    }

    // カードが引かれた時の挙動
    public int PullCard(int cardnum)
    {
        GameObject card = Instantiate(m_Card, transform.position + new Vector3(0, 0, -0.1f), Quaternion.Euler(0, 0, 0)) as GameObject;
        card.GetComponent<SpriteRenderer>().sprite = gameObject.GetComponent<SpriteRenderer>().sprite;
        card.GetComponent<CardController>().SetCardParam(m_CardList[m_Order[cardnum]].GetCardSprite(), m_CardList[m_Order[cardnum]].GetCardIndex());
        const float CARD_WIDTH = 2.1f;
        const float CARD_HEIGHT = 3.1f;
        Vector3 targetPosition = new Vector3(CARD_WIDTH + CARD_WIDTH * (m_FieldNum % 6), CARD_HEIGHT * (m_FieldNum >= 6 ? 1 : 0), 0.1f * m_FieldNum);
        card.GetComponent<CardController>().SetTargetPosition(transform.position - targetPosition);
        if(m_CardList[m_Order[cardnum]].GetCardIndex() == 10)
        {
            gameObject.GetComponent<GameManager>().OnDoubeleChallenge();
        }
        m_FieldNum++;
        return m_CardList[m_Order[cardnum]].GetCardIndex();
    }

    public List<int> GetCardIndex(int num, int total)
    {
        List<int> indexList = new List<int>();
        Debug.Log("num = " + num);
        for(int i = num; i <= total; i++)
        {
            indexList.Add(m_CardList[m_Order[i]].GetCardIndex());
        }
        return indexList;
    }
    public void ResetFieldNum() { m_FieldNum = 1; }
    public void PassFieldNum() { m_FieldNum--; }
}
