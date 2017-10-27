using UnityEngine;

public class CardController : MonoBehaviour {

    private Vector3 m_DeckPoaition;
    private Vector3 m_TargetPoaition;
    private Sprite m_Sprite;
    private int m_Index;
    private const float ROTATION_SPEED = 180.0f;
    private float m_Speed;

    enum STATE_TYPE
    {
        NONE,
        FLICKER,
        MOVE,
        DUMMY,
        ETC,
    }
    private STATE_TYPE m_State;

    private Animator animator;

    private float m_MoveStartTime;

    void Start()
    {
        m_DeckPoaition = transform.position;
        m_State = STATE_TYPE.FLICKER;

        animator = GetComponent(typeof(Animator)) as Animator;
        animator.speed = 4;
        animator.Play("Flicker1");
    }

    void Update()
    {
        switch (m_State)
        {
            case STATE_TYPE.FLICKER:
                Flicker();
                break;
            case STATE_TYPE.MOVE:
                Move();
                break;
            case STATE_TYPE.DUMMY:
                Move();
                break;
            default:
                break;
        }
    }

    private void Flicker()
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Flicker1End"))
        {
            ChangeSprite();
            animator.Play("Flicker2");
        }
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Flicker2End"))
        {
            m_MoveStartTime = Time.realtimeSinceStartup;
            m_Speed = 0.3f;
            m_State = STATE_TYPE.MOVE;
        }
    }

    private void Move()
    {
        float diffTime = (Time.realtimeSinceStartup - m_MoveStartTime) / m_Speed;
        transform.position = Vector3.Lerp(m_DeckPoaition, m_TargetPoaition, diffTime);
        float dis = Vector3.Distance(transform.position, m_TargetPoaition);
        if (dis <= 0.01f) {
            m_State = STATE_TYPE.NONE;
            Debug.Log("cardPos = " + transform.position);
        }
    }

    public void ResetPosition()
    {
        const float CARD_WIDTH = 2.1f;
        Vector3 firstPosition = new Vector3(CARD_WIDTH, 0.0f, -1.4f);
        m_TargetPoaition = m_DeckPoaition - firstPosition;
        m_DeckPoaition = transform.position;
        m_MoveStartTime = Time.realtimeSinceStartup;
        m_State = STATE_TYPE.MOVE;
    }

    public void OutOfScreen()
    {
        // どんな感じで画面外に出すのかわからないけど
        // とりあえずで・・・

        Vector3 outPosition = new Vector3(5.0f, -15.0f, 0.0f);
        m_TargetPoaition = m_DeckPoaition + outPosition;
        m_DeckPoaition = transform.position;
        m_MoveStartTime = Time.realtimeSinceStartup;
        m_Speed = Vector3.Distance(m_TargetPoaition, m_DeckPoaition) / 30;
        Invoke("CardDestroy", 1.0f);
        m_State = STATE_TYPE.MOVE;
    }

    private void CardDestroy()
    {
        Destroy(gameObject);
    }

    public void SetTargetPosition(Vector3 position)
    {
        m_TargetPoaition = position;
    }
    
    public void SetCardParam(Sprite sprite, int index)
    {
        m_Sprite = sprite;
        m_Index = index;
    }

    public int GetCardIndex()
    {
        return m_Index;
    }

    private void ChangeSprite()
    {
        this.GetComponent<SpriteRenderer>().sprite = m_Sprite;
    }
}
