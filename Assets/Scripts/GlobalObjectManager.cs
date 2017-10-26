using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalObjectManager : MonoBehaviour
{

    // 共有データ（シーン切り替え時に渡したいデータ）
    private static GlobalObjectManager sharedObject = null;
    private int m_GamePlayerNum = 3;    // 最低人数
    private int m_PlayerHP = 20;        // 体力

    public static GlobalObjectManager GetSharedObject() { return sharedObject; }

    public int GetGamePlayerNum() { return m_GamePlayerNum; }
    public int GetPlayerHP() { return m_PlayerHP; }

    public void Awake()
    {
        if (sharedObject == null)
        {
            sharedObject = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public static void LoadLevelWithSharedObject(string levelName, int num, int hp = 20)
    {
        GetSharedObject().m_GamePlayerNum = num;
        GetSharedObject().m_PlayerHP = hp;
        SceneManager.LoadScene(levelName);
    }
}
