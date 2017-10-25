using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // 待機時間（マジックナンバー防止）
    private const float NEXT_TURM_WAITTIME = 0.5f;                  // 0.5秒の指定あり
    private const float PICK_FAILURE_WAITTIME = 1.0f;               // 1.0秒の指定あり
    private const float DOUBLE_CHALLENGE_MESSAGE_WAITTIME = 2.0f;   // 2.0秒の指定あり
    private const float CARD_MOVE_WAITTIME = 0.8f;
    private const float CARD_OUT_WAITTIME = 0.2f;
    private const float COUNT_UP_WAITTIME = 0.03f;
    private const float COUNT_DOWN_WAITTIME = 0.2f;
    private const float OTHERS_WAITTIME = 0.5f;

    public GameObject m_PlayerBoard;
    public GameObject m_Infomation;
    public GameObject m_PullButton;
    public GameObject m_Announce;
    public GameObject m_JudgmentButton;
    public Text m_DeckNumText;

    private GameObject[] m_Cards;
    private const int LOST_CARD_NUM = 5;
    private const int TOTAL_CARD_NUM = 60;
    private int m_Turn = 0;     // ターン制御(次プレイヤーの取得)
    private int m_DeckNum;      // 引かれたカードの枚数
    private int m_PlayerNum = 3;
    private List<int> m_PlayerHP = new List<int>();
    private int m_MaxHP = 20;
    private bool m_IsPlaying = false;
    private bool m_IsChallengeSuccess = false;

    enum ACTION_MODE
    {
        NONE,
        PULL,
        PASS,
    }
    private ACTION_MODE m_ActionMode = ACTION_MODE.NONE;

    enum DOUBLE_MODE
    {
        NONE,
        CHALLENGE,
        JUGDMENT,
    }
    private DOUBLE_MODE m_DoubleMode = DOUBLE_MODE.NONE;

    enum JUDG_COLOR
    {
        NONE,
        RED,
        BLUE,
    }
    private JUDG_COLOR m_JudgColor = JUDG_COLOR.NONE;

    void Start()
    {
        m_Announce.SetActive(false);
        m_DeckNum = TOTAL_CARD_NUM - LOST_CARD_NUM;
        StartCoroutine(GameReady());
    }

    // ゲーム開始の準備
    private IEnumerator GameReady()
    {
        if (GlobalObjectManager.GetSharedObject() != null)
        {
            m_PlayerNum = GlobalObjectManager.GetSharedObject().GetGamePlayerNum();
            m_MaxHP = GlobalObjectManager.GetSharedObject().GetPlayerHP();
        }
        m_PlayerBoard.GetComponent<PlayerBoardManager>().SetPlayerBoard(m_PlayerNum);
        for (int i = 0; i < m_PlayerNum; i++)
        {
            m_PlayerHP.Add(m_MaxHP);
        }
        SetPullButton(false);
        SetAnnouncePanel(false);
        GameInfo();
        yield return StartCoroutine(SetDeckCount());
        yield return StartCoroutine(SetFirstCard());
        StartCoroutine(GameLoop());
    }

    // ゲームループ
    private IEnumerator GameLoop()
    {
        // プレイヤーの動作待ち(引くorパス)
        NextPlayerInfo();
        if (IsDoubleJudg())
        {
            yield return new WaitForSeconds(OTHERS_WAITTIME);
            SetAnnouncePanel(true, true);
            // プレイヤーの操作入力待ち
            while (!IsJudgColor()) { yield return null; }
        }
        SetPullButton(true);
        // プレイヤーの操作入力待ち
        while (!m_IsPlaying)
        {
            PassAction();
            yield return null;
        }

        m_IsPlaying = false;
        yield return new WaitForSeconds(CARD_MOVE_WAITTIME);

        // Doubleカード出現時
        if (IsDoubleChallenge())
        {
            if (m_DeckNum > 2)
            {
                yield return new WaitForSeconds(CARD_MOVE_WAITTIME);  // ダブルカードが重なった時に表示が早い
                SetAnnouncePanel(true);
                m_DoubleMode = DOUBLE_MODE.JUGDMENT;
                yield return new WaitForSeconds(DOUBLE_CHALLENGE_MESSAGE_WAITTIME);
            }
        }
        else if (IsDoubleJudg())
        {
            SearchFieldCard();
            yield return new WaitForSeconds(PICK_FAILURE_WAITTIME);
            yield return PenaltyScore();
            yield return ResetField();
            m_DoubleMode = DOUBLE_MODE.NONE;
            m_JudgColor = JUDG_COLOR.NONE;
        }
        else
        {
            if (m_ActionMode == ACTION_MODE.PULL)
            {
                // 成功or失敗の確認
                SearchFieldCard();
                if (!IsSuccess())
                {
                    yield return new WaitForSeconds(PICK_FAILURE_WAITTIME);
                    yield return PenaltyScore();
                    yield return ResetField();
                }
            }
            else if(m_ActionMode == ACTION_MODE.PASS)
            {
                SearchFieldCard();
                yield return PassPenalty();
            }
        }

        // ゲーム終了の確認
        yield return new WaitForSeconds(NEXT_TURM_WAITTIME);
        m_ActionMode = ACTION_MODE.NONE;
        SetAnnouncePanel(false);
        if (!IsFinish())
        {
            m_Turn++;
            StartCoroutine(GameLoop());
        }
        else StartCoroutine(GameEnd());
    }

    // ゲーム終了
    private IEnumerator GameEnd()
    {
        Debug.Log("終わり！");
        yield return null;
    }










    /*
     * 
     * ゲームの準備時に使うメソッド
     * 
     */
    private IEnumerator SetDeckCount()
    {
        for (int i = 0; i < TOTAL_CARD_NUM; i++) { yield return DeckCountUp(i + 1); }
        yield return new WaitForSeconds(OTHERS_WAITTIME);
        for (int i = 0; i < LOST_CARD_NUM; i++) { yield return DeckCountDown(i + 1); }
        yield return new WaitForSeconds(OTHERS_WAITTIME);
    }

    private IEnumerator DeckCountUp(int num)
    {
        m_DeckNumText.GetComponent<Text>().text = "" + num;
        yield return new WaitForSeconds(COUNT_UP_WAITTIME);
    }

    private IEnumerator DeckCountDown(int num)
    {
        m_DeckNumText.GetComponent<Text>().text = "" + (TOTAL_CARD_NUM - num);
        yield return new WaitForSeconds(COUNT_DOWN_WAITTIME);
    }

    private IEnumerator SetFirstCard()
    {
        gameObject.GetComponent<DeckManager>().PullCard(m_Turn);
        m_Turn++;
        m_DeckNum--;
        UpdateDeckCount();
        yield return new WaitForSeconds(CARD_MOVE_WAITTIME);
    }

    private void GameInfo()
    {
        Text[] infoText = m_Infomation.gameObject.GetComponentsInChildren<Text>();
        infoText[0].text = "ゲームの準備中です。";
    }










    /*
     * 
     * ゲームループ時に使うメソッド
     * 
     */
    private void NextPlayerInfo()
    {
        Text[] infoText = m_Infomation.gameObject.GetComponentsInChildren<Text>();
        int playerNo = ((m_Turn - 1) % m_PlayerNum) + 1;
        infoText[0].text = "プレイヤー" + playerNo + "の番です。";
    }

    // カードを引く（外部ボタンと連携）
    public void PullAction()
    {
        StartCoroutine(PullCard());
        m_ActionMode = ACTION_MODE.PULL;
        m_IsPlaying = true;
        SetPullButton(false);
    }

    // カードを引いた時の挙動
    private IEnumerator PullCard()
    {
        int loopNum = IsDoubleJudg() ? 2 : 1;
        int[] pullcrads = { 0, 0 };
        for (int i = 0; i < loopNum; i++)
        {
            pullcrads[i] = gameObject.GetComponent<DeckManager>().PullCard(m_DeckNum);
            m_DeckNum--;
            UpdateDeckCount();
            if (m_DeckNum <= 0) gameObject.GetComponent<SpriteRenderer>().color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
            if ((IsDoubleJudg() || IsDoubleChallenge()) && i == 0) yield return new WaitForSeconds(CARD_MOVE_WAITTIME);
            else yield return null;
        }
        if (IsDoubleJudg()) { m_IsChallengeSuccess = IsChallengeSuccess(pullcrads[0], pullcrads[1]); }
    }

    // フィールドのカードを探す
    private void SearchFieldCard()
    {
        m_Cards = GameObject.FindGameObjectsWithTag("Card");
    }

    // フィールドにカードが何枚あるか取得
    private int GetFieldCardNum()
    {
        GameObject[] obj = GameObject.FindGameObjectsWithTag("Card");
        return obj.Length;
    }

    // ピックの成功判定
    private bool IsSuccess()
    {
        bool success = true;
        if (m_Cards.Length <= 1) { return success; }
        for (int i = 0; i < m_Cards.Length; i++)
        {
            int index = m_Cards[i].GetComponent<CardController>().GetCardIndex();
            for (int n = i + 1; n < m_Cards.Length; n++)
            {
                if (index == m_Cards[n].GetComponent<CardController>().GetCardIndex()) { success = false; }
            }
        }
        return success;
    }

    // 失敗したカード以外の得点の合算・消去
    private IEnumerator PenaltyScore()
    {
        int totalScore = 0;
        for (int i = 0; i < m_Cards.Length - 1; i++)
        {
            int score = m_Cards[i].GetComponent<CardController>().GetCardIndex();
            if (score >= 5) totalScore += 1;    // 赤色カードは＋１得点
            else totalScore += score;           // 青色カードは番号がそのまま得点
            m_Cards[i].GetComponent<CardController>().OutOfScreen();
            yield return new WaitForSeconds(CARD_OUT_WAITTIME);
        }
        int targetPlayerIndex = !m_IsChallengeSuccess ? (m_Turn - 1) % m_PlayerNum : ((m_Turn - 1) % m_PlayerNum) - 1;
        if (targetPlayerIndex < 0) targetPlayerIndex = m_PlayerNum - 1;
        int beforeScore = m_PlayerHP[targetPlayerIndex];
        m_PlayerHP[targetPlayerIndex] -= totalScore;
        if (m_PlayerHP[targetPlayerIndex] <= 0) m_PlayerHP[targetPlayerIndex] = 0;
        yield return AddPlayerScore(beforeScore, m_PlayerHP[targetPlayerIndex], targetPlayerIndex);
    }

    // フィールドのカード初期状態にもどす
    private IEnumerator ResetField()
    {
        m_Cards[m_Cards.Length - 1].GetComponent<CardController>().ResetPosition(); // 失敗したカードを一枚目の位置に
        gameObject.GetComponent<DeckManager>().ResetFieldNum();                     // 出枚数を１に
        yield return new WaitForSeconds(OTHERS_WAITTIME);
    }

    // スコアボードを更新します
    private IEnumerator AddPlayerScore(int beforeScore, int afterScore, int playerIndex)
    {
        yield return m_PlayerBoard.GetComponent<PlayerBoardManager>().AddPlayerScore(beforeScore, afterScore, playerIndex + 1);
    }

    // ゲーム終了判定
    private bool IsFinish()
    {
        bool fin = false;
        if (m_DeckNum <= 0) fin = true;
        for(int i = 0; i < m_PlayerNum; i++)
        {
            if(m_PlayerHP[i] <= 0) fin = true;
        }
        if (IsDoubleChallenge())
        {
            if (m_DeckNum <= 2) fin = true;
        }
        return fin;
    }


    /// <summary>
    /// スワイプ機能
    /// どうしたものか。。。
    /// </summary>
    private Vector2 m_StartPosition;
    private Vector2 m_EndPosition;
    private bool m_IsSwipe;
    private const float SWIPE_LENGTH = 130.0f;
    private const float DECK_POSITION_X = 1076.0f;
    private const float CARD_WIDTH = 139.0f;
    private const float DECK_POSITION_Y = 414.0f;
    private const float CARD_HEIGHT = 214.0f;
    private const float CARD_SPAN = 18.0f;
    private void PassAction()
    {
        if (GetFieldCardNum() <= 1) { return; }
        if (Input.GetMouseButtonDown(0))
        {
            m_StartPosition = Input.mousePosition;
            float x = DECK_POSITION_X - ((CARD_WIDTH + CARD_SPAN) * GetFieldCardNum());
            float y = DECK_POSITION_Y;
            if (GetFieldCardNum() > 7) { y -= (CARD_HEIGHT + CARD_SPAN); }

            if (m_StartPosition.x >= x && m_StartPosition.x <= x + CARD_WIDTH)
            {
                if (m_StartPosition.y >= y && m_StartPosition.y <= y + CARD_HEIGHT) { m_IsSwipe = true; }
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            m_EndPosition = Input.mousePosition;
            if (m_IsSwipe)
            {
                float dis = Vector2.Distance(m_StartPosition, m_EndPosition);
                if (dis > SWIPE_LENGTH) {
                    m_ActionMode = ACTION_MODE.PASS;
                    m_IsPlaying = true;
                    SetPullButton(false);
                }
            }
            m_IsSwipe = false;
        }
    }

    private IEnumerator PassPenalty()
    {
        int score = m_Cards[m_Cards.Length - 1].GetComponent<CardController>().GetCardIndex();
        if (score >= 5) score = 1;    // 赤色カードは＋１得点
        m_Cards[m_Cards.Length - 1].GetComponent<CardController>().OutOfScreen();
        yield return new WaitForSeconds(CARD_OUT_WAITTIME);
        int targetPlayerIndex = (m_Turn - 1) % m_PlayerNum;
        int beforeScore = m_PlayerHP[targetPlayerIndex];
        m_PlayerHP[targetPlayerIndex] -= score;
        yield return AddPlayerScore(beforeScore, m_PlayerHP[targetPlayerIndex], targetPlayerIndex);
        gameObject.GetComponent<DeckManager>().PassFieldNum();
        yield return new WaitForSeconds(OTHERS_WAITTIME);
    }

    // カードを引くボタン（連続で引けないようにするため）
    private void SetPullButton(bool isOK) { m_PullButton.SetActive(isOK); }

    // カード枚数の更新
    private void UpdateDeckCount()
    {
        m_DeckNumText.GetComponent<Text>().text = "" + m_DeckNum;
    }

    /*
     *
     * ダブルカード関連
     * 
     */
    // ダブルカードのモード管理
    public void OnDoubeleChallenge() { m_DoubleMode = DOUBLE_MODE.CHALLENGE; }
    private bool IsDoubleChallenge() { return m_DoubleMode == DOUBLE_MODE.CHALLENGE; }
    private bool IsDoubleJudg() { return m_DoubleMode == DOUBLE_MODE.JUGDMENT; }

    // ダブルチャレンジの選択
    public void SelectJudgColor(bool color)
    {
        if (color) m_JudgColor = JUDG_COLOR.RED;
        else m_JudgColor = JUDG_COLOR.BLUE;
        SetAnnouncePanel(false);
        PullAction();
    }
    private bool IsJudgColor() { return m_JudgColor != JUDG_COLOR.NONE; }

    // ダブルチャレンジの成功確認
    private bool IsChallengeSuccess(int firstcard, int secondcard)
    {
        bool success = true;
        if(m_JudgColor == JUDG_COLOR.RED)
        {
            if(firstcard <= 4 || secondcard <= 4) { success = false; }
        }
        if (m_JudgColor == JUDG_COLOR.BLUE)
        {
            if (firstcard > 4 && secondcard > 4) { success = false; }
        }
        return success;
    }

    // ダブルカードを引いた時のアナウンス
    private void SetAnnouncePanel(bool isOK, bool isJudg = false)
    {
        m_Announce.SetActive(isOK);
        if (isOK)
        {
            Text[] announceText = m_Announce.gameObject.GetComponentsInChildren<Text>();
            string text = isJudg ? "Judgment" : "Double Challenge!!";
            announceText[0].text = text;
            m_JudgmentButton.SetActive(isJudg);
        }
    }
}
