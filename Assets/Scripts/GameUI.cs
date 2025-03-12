using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// Handles the main in-game UI
public class GameUI : MonoBehaviour
{
    public static GameUI Instance;

    private Animation _anim;

    [SerializeField]
    private TMP_Text Beads;

    [SerializeField]
    private TMP_Text ScaleText;
    [SerializeField]
    private Color[] ScaleTextColors;
    [SerializeField]
    private Sprite[] ScaleSprites;
    [SerializeField]
    private Image ScaleImage;

    [SerializeField]
    private GameObject DialogueBox;
    [SerializeField]
    private TMP_Text DialogueName;
    [SerializeField]
    private TMP_Text DialogueText;

    [SerializeField]
    private GameObject BossBarContainer;
    [SerializeField]
    private Image Bossbar;

    [SerializeField]
    private GameObject PotionContainer;
    [SerializeField]
    private Image[] PotionComponents;
    [SerializeField]
    private TMP_Text PotionText;
    [SerializeField]
    private Sprite[] PotionSprites;

    [SerializeField]
    private Image AnnouncementBG;
    [SerializeField]
    private TMP_Text AnnouncementText;

    [SerializeField]
    private RectTransform HealthContainer;
    [SerializeField]
    private Image[] Health;
    [SerializeField]
    private Sprite[] HeartSprites;

    [SerializeField]
    private RectTransform CardSelection;
    [SerializeField]
    private Card CardPrefab;
    [SerializeField]
    private MinimapTile TilePrefab;
    [SerializeField]
    private RectTransform MinimapRect;

    [SerializeField]
    private Image ScaleImage2;
    [SerializeField]
    private TMP_Text ScaleText2;

    [SerializeField]
    private TMP_Text[] StatText;

    private bool HasPickedItem = false;
    private Item ChosenItem;

    private Dictionary<PlayerStat, Coroutine> RunningHighlights;
    private Dictionary<Vector2Int, MinimapTile> MapTiles;

    private float MinimapIconSize;
    public bool InDialogue = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        RunningHighlights = new Dictionary<PlayerStat, Coroutine>();
        _anim = GetComponent<Animation>();
        UpdateScale();
    }

    void OnEnable()
    {
        GameManager.OnStatChanged += HighlightStat;
        RoomManager.OnRoomSwitch += UpdateMinimap;
        FloorManager.OnFloorGenerated += OnFloorGenerated;
    }

    void OnDisable()
    {
        GameManager.OnStatChanged -= HighlightStat;
        RoomManager.OnRoomSwitch -= UpdateMinimap;
        FloorManager.OnFloorGenerated -= OnFloorGenerated;
    }

    // Set player stats
    void Update()
    {
        Beads.text = "x " + GameManager.Instance.Beads + "/" + GameManager.Instance.MaxBeads;

        StatText[0].text = GameManager.Instance.GetSpeed.ToString("F2");
        StatText[1].text = GameManager.Instance.GetDamage.ToString("F2");
        StatText[2].text = GameManager.Instance.CurrentStats.FireRate.ToString("F2");
        StatText[3].text = GameManager.Instance.CurrentStats.BaseBulletSpeed.ToString("F2");
        StatText[4].text = GameManager.Instance.CurrentStats.BaseBulletRange.ToString("F2");

        var luck = GameManager.Instance.CurrentStats.Luck;

        StatText[5].text = (luck >= 0 ? "+" : "") + luck;
    }

    void OnFloorGenerated(Vector2Int floorSize)
    {
        MinimapIconSize = Mathf.Min(MinimapRect.sizeDelta.x / floorSize.x, MinimapRect.sizeDelta.y / floorSize.y);
        MapTiles = new Dictionary<Vector2Int, MinimapTile>();

        UpdateMinimap(Vector2Int.zero, Vector2Int.zero);
    }

    // Update the minimap when entering a new room
    public void UpdateMinimap(Vector2Int prevPosition, Vector2Int newPosition)
    {
        if (!MapTiles.ContainsKey(newPosition))
        {
            SpawnTile(newPosition);
        }

        for (int i = 0; i < 4; i++)
        {
            if (FloorManager.Instance.GetRoomAtPosition(newPosition).Neighbors[i])
            {
                var neighborPosition = newPosition;
                if (i == 0) neighborPosition += new Vector2Int(-1, 0);
                else if (i == 1) neighborPosition += new Vector2Int(0, 1);
                else if (i == 2) neighborPosition += new Vector2Int(1, 0);
                else neighborPosition += new Vector2Int(0, -1);

                if (!MapTiles.ContainsKey(neighborPosition)) SpawnTile(neighborPosition);
            }
        }

        MapTiles[prevPosition].SetPlayerPresence(false);
        MapTiles[newPosition].SetPlayerPresence(true);
        MapTiles[newPosition].SetVisited();
    }

    // Create a new minimap tile as needed
    void SpawnTile(Vector2Int position)
    {
        var tile = Instantiate(TilePrefab, MinimapRect);
        var rect = tile.GetComponent<RectTransform>();
        tile.SetRoomType(FloorManager.Instance.GetRoomAtPosition(position).Type);
        rect.anchoredPosition = Vector2.Scale(position, new Vector2(MinimapIconSize, MinimapIconSize));
        rect.sizeDelta = new Vector2(MinimapIconSize, MinimapIconSize);

        MapTiles[position] = tile;
    }

    public void UpdateHealthDisplay(int health)
    {
        for(int i = 0; i < Health.Length; i++)
        {
            if (i < health) Health[i].sprite = HeartSprites[0];
            else Health[i].sprite = HeartSprites[1];

            Health[i].enabled = i < GameManager.Instance.PlayerMaxHealth;
        }
    }

    public void PresentOptions(Item[] options)
    {
        StartCoroutine(ShowItemSequence(options));
    }

    void HighlightStat(PlayerStat stat, int delta)
    {
        if (RunningHighlights.ContainsKey(stat)) StopCoroutine(RunningHighlights[stat]);
        RunningHighlights[stat] = StartCoroutine(HighlightStatCoroutine(stat, delta));
    }

    public void ShowAnnouncement(string text)
    {
        if (RunningAnnouncement != null) StopCoroutine(RunningAnnouncement);
        RunningAnnouncement = StartCoroutine(ShowAnnouncementCoroutine(text));
    }

    private Coroutine RunningAnnouncement;

    // Shows the text when picking up some items or drinking potions
    IEnumerator ShowAnnouncementCoroutine(string text)
    {
        var interpolator = new Interpolator<float>(0.5f, 0, 1f);

        AnnouncementText.text = text;
        AnnouncementBG.gameObject.SetActive(true);

        while (!interpolator.HasFinished)
        {
            var t = interpolator.Update(Time.deltaTime);

            AnnouncementBG.color = Color.Lerp(Color.clear, new Color(0,0,0,0.8f), t);
            AnnouncementText.color = Color.Lerp(new Color(1,1,1,0), Color.white, t);

            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(2f + text.Length * 0.08f);

        interpolator = new Interpolator<float>(0.5f, 1, 0f);
        while (!interpolator.HasFinished)
        {
            var t = interpolator.Update(Time.deltaTime);

            AnnouncementBG.color = Color.Lerp(Color.clear, new Color(0, 0, 0, 0.8f), t);
            AnnouncementText.color = Color.Lerp(new Color(1, 1, 1, 0), Color.white, t);

            yield return new WaitForEndOfFrame();
        }
        AnnouncementBG.gameObject.SetActive(false);
        RunningAnnouncement = null;
    }

    // Highlights increased or decreased stats
    IEnumerator HighlightStatCoroutine(PlayerStat stat, int delta)
    {
        var startColor = new Color(1, 1, 1, 0.196f);
        var endColor = delta > 0 ? new Color(0, 1, 0, 1) : new Color(1, 0, 0, 1);

        var interpolator = new Interpolator<Color>(0.5f, startColor, endColor);
        while (!interpolator.HasFinished)
        {
            StatText[(int)stat].color = interpolator.Update(Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(1.25f);

        interpolator = new Interpolator<Color>(0.5f, endColor, startColor);
        while (!interpolator.HasFinished)
        {
            StatText[(int)stat].color = interpolator.Update(Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }

        RunningHighlights.Remove(stat);
    }

    IEnumerator ShowItemSequence(Item[] options)
    {
        HasPickedItem = false;
        List<Card> cards = new List<Card>();

        _anim.Play("anim_cards_in");
        foreach (var item in options)
        {
            var card = Instantiate(CardPrefab, CardSelection);
            card.Setup(item.Pool);
            cards.Add(card);
        }
        yield return new WaitForSeconds(1.5f);

        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].SetItem(options[i]);
            AudioManager.Instance.PlaySFX(SFX.CARD_TURN, PlayType.UNRESTRICTED);
            yield return new WaitForSeconds(0.4f);
        }

        yield return new WaitUntil(() => HasPickedItem);

        AudioManager.Instance.PlaySFX(SFX.ITEM_GET, PlayType.UNRESTRICTED);

        for (int i = 0; i < cards.Count; i++)
        {
            if (options[i] == ChosenItem) continue;
            cards[i].Fade();
        }

        yield return new WaitForSeconds(0.7f);

        _anim.Play("anim_cards_out");

        yield return new WaitForSeconds(1f);

        for (int i = 0; i < cards.Count; i++)
        {
            Destroy(cards[i].gameObject);
        }

        GameManager.Instance.OnItemPicked(ChosenItem);
        if (ChosenItem.Announcement != "") ShowAnnouncement(ChosenItem.Announcement);
    }

    public void PickedItem(Item item)
    {
        ChosenItem = item;
        HasPickedItem = true;
    }

    public void SetBossBarActive(bool active)
    {
        BossBarContainer.SetActive(active);
    }

    public void SetBossBarValue(float value)
    {
        Bossbar.fillAmount = value;
    }

    // Hard coded mess to change the scale sprite and text
    public void UpdateScale()
    {
        if (GameManager.Instance.Evil == 0) ScaleImage.sprite = ScaleSprites[3];
        else if (GameManager.Instance.Evil > 0 && GameManager.Instance.Evil < 3) ScaleImage.sprite = ScaleSprites[2];
        else if (GameManager.Instance.Evil >= 3 && GameManager.Instance.Evil < 5) ScaleImage.sprite = ScaleSprites[1];
        else if (GameManager.Instance.Evil >= 5) ScaleImage.sprite = ScaleSprites[0];
        else if (GameManager.Instance.Evil < 0 && GameManager.Instance.Evil > -3) ScaleImage.sprite = ScaleSprites[4];
        else if (GameManager.Instance.Evil <= -3 && GameManager.Instance.Evil > -5) ScaleImage.sprite = ScaleSprites[5];
        else if (GameManager.Instance.Evil <= -5) ScaleImage.sprite = ScaleSprites[6];

        if (GameManager.Instance.Evil == 0)
        {
            ScaleText.color = ScaleTextColors[0];
            ScaleText.text = "0/0";
        }
        else if (GameManager.Instance.Evil > 0)
        {
            ScaleText.color = ScaleTextColors[1];
            if (GameManager.Instance.Evil < 5)
                ScaleText.text = "-" + GameManager.Instance.Evil.ToString() + "/5";
            else
                ScaleText.text = "Tipped";
        }
        else
        {
            ScaleText.color = ScaleTextColors[2];
            if (GameManager.Instance.Evil > -5)
                ScaleText.text = (-GameManager.Instance.Evil).ToString() + "/5";
            else
                ScaleText.text = "Tipped";
        }

        ScaleImage2.sprite = ScaleImage.sprite;
        ScaleText2.text = ScaleText.text;
        ScaleText2.color = ScaleText.color;
    }

    // Show potion you have
    public void UpdatePotion(ConsumableData data)
    {
        if (data == null)
        {
            PotionContainer.SetActive(false);
            return;
        }
        else
            PotionContainer.SetActive(true);

        PotionComponents[0].sprite = PotionSprites[data.SpriteIndex];
        PotionComponents[1].sprite = PotionSprites[data.SpriteIndex + 3];
        PotionComponents[1].color = data.PotionColor;

        PotionText.text = data.GetDisplayName();
    }

    public void ClearMinimap()
    {
        if (MapTiles != null)
        {
            foreach (var tile in MapTiles.Keys)
            {
                Destroy(MapTiles[tile].gameObject);
            }
        }
        MapTiles = new Dictionary<Vector2Int, MinimapTile>();
    }

    public void ShowDialogue(Dialogue data)
    {
        InDialogue = true;
        StartCoroutine(ShowDialogueCoroutine(data));
    }

    IEnumerator ShowDialogueCoroutine(Dialogue data)
    {
        DialogueBox.SetActive(true);

        for (int i = 0; i < data.Data.Length; i++)
        {
            var entry = data.Data[i];
            DialogueName.text =entry.Name;
            DialogueText.text = "";

            for (int j = 0; j < entry.Text.Length; j++)
            {
                DialogueText.text = entry.Text.Substring(0, j + 1);
                yield return new WaitForFixedUpdate();
            }

            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Return));
        }

        DialogueBox.SetActive(false);
        InDialogue = false;
    }
}
