using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

// UI class used in the card picking screen
public class Card : MonoBehaviour
{
    private Animation _anim;

    [SerializeField]
    private Image _image;
    [SerializeField]
    private float TurnDuration = 0.7f;

    [SerializeField]
    private Sprite[] CardbackSprites;
    [SerializeField]
    private Sprite[] CardfrontSprites;
    [SerializeField]
    private TMP_Text Description;
    [SerializeField]
    private Image ItemImage;
    [SerializeField]
    private Image HoverImage;

    private Item CurrentItem;
    private bool LockedControls;
    private bool IsHovered;

    public void Setup(ItemPoolType pool)
    {
        _anim = GetComponent<Animation>();
        _image.sprite = CardbackSprites[(int)pool];
        Description.text = "";
        LockedControls = true;
        IsHovered = false;
        ItemImage.enabled = false;

        HoverImage.color = Color.white;
        ItemImage.color = Color.white;
        _image.color = Color.white;
        Description.color = Color.white;
    }

    public void SetItem(Item item)
    {
        CurrentItem = item;
        StartCoroutine(RevealItem());
    }

    void Update()
    {
        HoverImage.color = IsHovered && !LockedControls ? Color.white : Color.clear;
    }

    // Play fancy turning animation
    IEnumerator RevealItem()
    {
        StartCoroutine(PrintText());

        var interpolator = new Interpolator<float>(TurnDuration, 1, 0);
        while (!interpolator.HasFinished)
        {
            _image.transform.localScale = new Vector3(interpolator.Update(Time.deltaTime), 1, 1);
            yield return new WaitForEndOfFrame();
        }

        _image.sprite = CardfrontSprites[(int)CurrentItem.Pool];
        ItemImage.enabled = true;
        ItemImage.sprite = CurrentItem.CardSprite;
        ItemImage.rectTransform.sizeDelta = CurrentItem.CardSpriteSize;

        interpolator = new Interpolator<float>(TurnDuration, 0, 1);
        while (!interpolator.HasFinished)
        {
            _image.transform.localScale = new Vector3(interpolator.Update(Time.deltaTime), 1, 1);
            yield return new WaitForEndOfFrame();
        }

        LockedControls = false;
    }

    // Shitty code to print the text with sprites and boldo outlines one by one
    IEnumerator PrintText()
    {
        for (int i = 0; i < CurrentItem.Name.Length + 1; i++)
        {
            Description.text = "<b>" + CurrentItem.Name.Substring(0, i) + "</b> ";
            yield return new WaitForFixedUpdate();
        }
        string[] text = new string[] {
            "-",
            " ",
            "<sprite=0>",
            "<sprite=" + (CurrentItem.Quality >= 1 ? "0" : "1") + ">",
            "<sprite=" + (CurrentItem.Quality >= 2 ? "0" : "1") + ">\n"};

        foreach (var component in text)
        {
            Description.text += component;
            yield return new WaitForFixedUpdate();
        }

        foreach (var c in CurrentItem.Description)
        {
            Description.text += c;
            yield return new WaitForFixedUpdate();
        }
    }

    public void Fade()
    {
        LockedControls = true;
        _anim.Play("anim_card_fade_out");
    }

    public void OnMouseEnter()
    {
        IsHovered = true;
    }

    public void OnMouseExit()
    {
        IsHovered = false;
    }

    public void OnMouseUp()
    {
        if (LockedControls) return;

        LockedControls = true;
        GameUI.Instance.PickedItem(CurrentItem);
    }
}
