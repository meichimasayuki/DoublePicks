using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleButtonManager : MonoBehaviour {

    public GameObject m_Popup;

    // 初期化処理
    void Start()
    {
        m_Popup.SetActive(false);
    }

    // スタートボタン
    public void TouchStartButton()
    {
        m_Popup.gameObject.SetActive(true);
    }

    // チュートリアルボタン
    public void TouchTutorialButton()
    {
        SceneManager.LoadScene("Tutorial");
    }

    // プレイヤー人数の選択
    public void GameStart(int num)
    {
        GlobalObjectManager.LoadLevelWithSharedObject("MainGame", num);
    }

    // 閉じるボタン
    public void TouchExitButton()
    {
        m_Popup.gameObject.SetActive(false);
    }
}
