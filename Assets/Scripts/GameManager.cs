using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // 待機時間（マジックナンバー防止）
    private const float NEXT_TURM_WAITTIME = 0.5f;                  // 0.5秒の指定あり
    private const float PICK_FAILURE_WAITTIME = 1.0f;               // 1.0秒の指定あり
    private const float DOUBLE_CHALLENGE_MESSAGE_WAITTIME = 2.0f;   // 2.0秒の指定あり
    private const float CARD_MOVE_WAITTIME = 0.95f;
    private const float CARD_OUT_WAITTIME = 0.2f;
    private const float COUNT_UP_WAITTIME = 0.03f;
    private const float COUNT_DOWN_WAITTIME = 0.2f;
    private const float OTHERS_WAITTIME = 0.5f;

    public GameObject m_PlayerBoard;
    public GameObject m_Infomation;
    public GameObject m_PullButton;
    public GameObject m_Announce;
    public GameObject m_JudgmentButton;
    public GameObject m_BlackMask;
    public GameObject m_HelpPanel;
    public List<Text> m_HelpTextList;
    public GameObject m_TitleBackPanel;
    public Text m_DeckNumText;

    public GameObject m_Miss;
    public GameObject m_Success;

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
        gameObject.GetComponent<DeckManager>().CreateDeck();
        gameObject.GetComponent<DeckManager>().Shuffle(m_DeckNum);
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
        NextTrunPlayer();
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
        SetPullButton(false); // 念のため
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
                else
                {
                    yield return new WaitForSeconds(PICK_FAILURE_WAITTIME);
                }
            }
            else if (m_ActionMode == ACTION_MODE.PASS)
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
        else WhoWins();
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
        gameObject.GetComponent<DeckManager>().PullCard(m_DeckNum);
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
            if (pullcrads[0] == 10) break; // DoubleCard を引いたら強制的に抜けます　MG 気を付けて
            if ((IsDoubleJudg() || IsDoubleChallenge()) && i == 0) yield return new WaitForSeconds(CARD_MOVE_WAITTIME);
            else yield return null;
        }
        if (IsDoubleJudg()) {
            m_IsChallengeSuccess = IsChallengeSuccess(pullcrads[0], pullcrads[1]);
            yield return new WaitForSeconds(CARD_MOVE_WAITTIME);
            AnimationSuccess(m_IsChallengeSuccess);
        }
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
        AnimationSuccess(success);
        return success;
    }

    private void AnimationSuccess(bool success)
    {
        Animator animator;
        if (success)
        {
            animator = m_Success.GetComponent(typeof(Animator)) as Animator;
            animator.speed = 1;
            animator.Play("Success");
        }
        else
        {
            animator = m_Miss.GetComponent(typeof(Animator)) as Animator;
            animator.speed = 1;
            animator.Play("Miss");
        }
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
        int targetPlayerIndex = (m_Turn - 1) % m_PlayerNum;
        if (m_IsChallengeSuccess)
        {
            targetPlayerIndex -= 1;
            if (targetPlayerIndex < 0) targetPlayerIndex = m_PlayerNum - 1;
            m_IsChallengeSuccess = false;
        }
        if (targetPlayerIndex < 0) targetPlayerIndex = m_PlayerNum - 1;
        int beforeScore = m_PlayerHP[targetPlayerIndex];
        m_PlayerHP[targetPlayerIndex] -= totalScore;
        if (m_PlayerHP[targetPlayerIndex] <= 0) m_PlayerHP[targetPlayerIndex] = 0;
        yield return UpdatePlayerScore(beforeScore, m_PlayerHP[targetPlayerIndex], targetPlayerIndex);
    }

    // フィールドのカード初期状態にもどす
    private IEnumerator ResetField()
    {
        m_Cards[m_Cards.Length - 1].GetComponent<CardController>().ResetPosition(); // 失敗したカードを一枚目の位置に
        gameObject.GetComponent<DeckManager>().ResetFieldNum();                     // 出枚数を１に
        yield return new WaitForSeconds(OTHERS_WAITTIME);
    }

    // スコアボードを更新します
    private IEnumerator UpdatePlayerScore(int beforeScore, int afterScore, int target)
    {
        yield return m_PlayerBoard.GetComponent<PlayerBoardManager>().UpdatePlayerScore(beforeScore, afterScore, target);
    }

    private void NextTrunPlayer()
    {
        m_PlayerBoard.GetComponent<PlayerBoardManager>().NextTrunPlayer(m_Turn - 1);
    }

    // ゲーム終了判定
    private bool IsFinish()
    {
        bool fin = false;
        if (m_DeckNum <= 0) fin = true;
        for (int i = 0; i < m_PlayerNum; i++)
        {
            if (m_PlayerHP[i] <= 0) fin = true;
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
    private const float SWIPE_LENGTH = 10.0f;
    private Vector2 m_StartPosition;
    private Vector2 m_EndPosition;
    private bool m_IsSwipe;
    private void PassAction()
    {
        if (GetFieldCardNum() <= 1) { return; }
        if (Input.GetMouseButtonDown(0))
        {
            m_StartPosition = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(m_StartPosition);
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(ray, out hit))
            {
                CardController card = hit.collider.gameObject.GetComponent<CardController>();
                if (card)
                {
                    int endcard = m_Cards[m_Cards.Length - 1].GetComponent<CardController>().GetCardIndex();
                    if (endcard == card.GetCardIndex())
                    {
                        m_IsSwipe = true;
                        Debug.Log("パス可能なカードです");
                    }
                }
                Debug.Log(hit.collider.gameObject.name);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            m_EndPosition = Input.mousePosition;
            if (m_IsSwipe)
            {
                float dis = Vector2.Distance(m_StartPosition, m_EndPosition);
                if (dis > SWIPE_LENGTH)
                {
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
        yield return UpdatePlayerScore(beforeScore, m_PlayerHP[targetPlayerIndex], targetPlayerIndex);
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









    // 誰がかったか
    private void WhoWins()
    {
        int winerScore = 0;
        int winerIndex = 0;
        bool isDrow = false;
        for (int i = 0; i < m_PlayerNum; i++)
        {
            if (winerScore < m_PlayerHP[i])
            {
                winerScore = m_PlayerHP[i];
                winerIndex = i;
            }
        }
        for (int i = 0; i < m_PlayerNum; i++)
        {
            if (winerIndex == i) continue;
            if (winerScore == m_PlayerHP[i])
            {
                isDrow = true;
                break;
            }
        }
        WinerAnnounce(winerIndex, isDrow);
    }
    private void WinerAnnounce(int index, bool isDrow)
    {
        m_BlackMask.SetActive(true);
        Text[] winerText = m_BlackMask.gameObject.GetComponentsInChildren<Text>();
        string text = isDrow ? "DROW GAME" : "WIN : PLAYER" + (index + 1).ToString();
        winerText[0].text = text;
    }


    // タイトルボタン
    public void TouchTitleButton()
    {
        SceneManager.LoadScene("Title");
    }

    // リトライボタン
    public void TouchRetryButton()
    {
        SceneManager.LoadScene("MainGame");
    }

    // ヘルプボタン
    private bool m_IsHelp = false;
    public void TouchHelpButton()
    {
        List<int> list = gameObject.GetComponent<DeckManager>().GetCardIndex(m_DeckNum+1, TOTAL_CARD_NUM - LOST_CARD_NUM);
        Debug.Log(list.Count);
        if (!m_IsHelp)
        {
            m_HelpPanel.SetActive(true);
            for (int i = 0; i < m_HelpTextList.Count; i++)
            {
                int deno = (i + 2);
                if (i == 8) deno = 13;
                else if (i == 9) deno = 3;

                int pullnum = 0;
                for(int n = 0; n < list.Count; n++)
                {
                    Debug.Log(list[n]);
                    if (i + 1 == list[n]) pullnum++;
                }
                m_HelpTextList[i].text = pullnum.ToString() + "/" + deno.ToString();
            }
            m_IsHelp = true;
        }
    }
    public void TouchExitButton()
    {
        m_IsHelp = false;
        m_HelpPanel.SetActive(false);
    }

    // タイトルボタン
    public void TouchTitleBackButton()
    {
        m_TitleBackPanel.SetActive(true);
    }
    public void TouchNoButton()
    {
        m_TitleBackPanel.SetActive(false);
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
        if (m_JudgColor == JUDG_COLOR.RED)
        {
            if (firstcard <= 4 || secondcard <= 4) { success = false; }
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