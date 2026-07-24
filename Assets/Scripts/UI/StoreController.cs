using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreController : MonoBehaviour
{
    public static StoreController Instance { get; private set; }

    class Item
    {
        public int price;
        public System.Func<bool> soldOut;
        public System.Action buy;
        public Button btn;
        public TMP_Text label;
        public string name;
    }

    Canvas canvas;
    TMP_Text moneyLabel, toast;
    float toastTime;
    readonly List<Item> items = new();

    void Awake() => Instance = this;

    void Start()
    {
        Build();
        GameFlow.Instance.StateChanged += OnState;
    }

    void OnDestroy()
    {
        if (GameFlow.Instance != null)
        {
            GameFlow.Instance.StateChanged -= OnState;
        }
    }

    void OnState(GameState s)
    {
        if (s != GameState.MainMenu)
        {
            canvas.gameObject.SetActive(false);
        }
    }

    public void Open() => canvas.gameObject.SetActive(true);
    public void Close() => canvas.gameObject.SetActive(false);

    void Build()
    {
        canvas = UIFactory.CreateOverlayCanvas("StoreCanvas", 108, transform);
        UIFactory.FullScreenPanel(canvas.transform, new Color(0.09f, 0.06f, 0.05f, 1f));
        UIFactory.Label(canvas.transform, "GENERAL STORE", 88, UIFactory.Parchment, new Vector2(0, 380));
        moneyLabel = UIFactory.Label(canvas.transform, "$0", 60, new Color(0.9f, 0.75f, 0.2f), new Vector2(0, 290));

        AddItem("DAYLIGHT  +30s", 200, () => false,
            () => RunClock.Instance?.AddDaylight(30f), 170);
        AddItem("STEADY HAND", 400, () => Loadout.DrawAdvantage >= Loadout.MaxDrawAdvantage,
            () => Loadout.DrawAdvantage = Mathf.Min(Loadout.MaxDrawAdvantage, Loadout.DrawAdvantage + 0.12f), 70);
        AddItem("SECOND WIND  +60s", 320, () => false,
            () => RunClock.Instance?.AddDaylight(60f), -30);
        AddItem("SNAKE OIL  (?)", 100, () => false,
            () => ShowToast(DrinkSnakeOil()), -130);

        toast = UIFactory.Label(canvas.transform, "", 34, new Color(0.95f, 0.75f, 0.35f), new Vector2(0, -210));
        toast.fontStyle = FontStyles.Italic;
        UIFactory.MenuButton(canvas.transform, "BACK", new Vector2(0, -300), Close);
        canvas.gameObject.SetActive(false);
        Refresh();
    }

    void AddItem(string name, int price, System.Func<bool> soldOut, System.Action effect, float y)
    {
        var item = new Item { name = name, price = price, soldOut = soldOut, buy = effect };
        item.label = UIFactory.Label(canvas.transform, "", 40, UIFactory.Parchment, new Vector2(-140, y), TextAlignmentOptions.Left);
        item.label.rectTransform.sizeDelta = new Vector2(760, 60);
        item.btn = UIFactory.MenuButton(canvas.transform, "BUY", new Vector2(430, y), () => Purchase(item));
        item.btn.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 80);
        items.Add(item);
    }

    void Purchase(Item item)
    {
        if (item.soldOut() || !Wallet.TrySpend(item.price))
        {
            return;
        }
        item.buy();
        AudioManager.Instance?.PlaySfx("clunk");
        Refresh();
    }

    void Refresh()
    {
        moneyLabel.text = $"${Wallet.Money}";
        foreach (var it in items)
        {
            bool sold = it.soldOut();
            it.label.text = sold ? $"{it.name}   -   SOLD" : $"{it.name}   -   ${it.price}";
            it.btn.interactable = !sold && Wallet.Money >= it.price;
        }
    }

    string DrinkSnakeOil()
    {
        AudioManager.Instance?.PlaySfx("clunk", 0.6f);
        switch (Random.Range(0, 6))
        {
            case 0: RunClock.Instance?.AddDaylight(45f); return "sunlight in a bottle!   +45s";
            case 1: RunClock.Instance?.AddDaylight(-30f); return "bad batch...   -30s";
            case 2: Loadout.DrawAdvantage = Mathf.Min(Loadout.MaxDrawAdvantage, Loadout.DrawAdvantage + 0.12f); return "steady as a rock";
            case 3: Loadout.DrawAdvantage = Mathf.Max(0f, Loadout.DrawAdvantage - 0.08f); return "the shakes set in...";
            case 4: Wallet.Add(150); return "coins in the bottle!   +$150";
            default: return "tasted like turpentine. nothin'.";
        }
    }

    void ShowToast(string s) { toast.text = s; toastTime = 2.6f; }

    void Update()
    {
        if (!canvas.gameObject.activeSelf)
        {
            return;
        }
        Refresh();
        if (toastTime > 0f)
        {
            toastTime -= Time.unscaledDeltaTime;
            if (toastTime <= 0f)
            {
                toast.text = "";
            }
        }
    }
}
