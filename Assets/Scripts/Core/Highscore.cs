using UnityEngine;

public static class Highscore
{
    const string KRivals = "best_rivals";
    const string KBounty = "best_bounty";
    const string KDaylight = "best_daylight_left";

    public static int BestRivals => PlayerPrefs.GetInt(KRivals, 0);
    public static int BestBounty => PlayerPrefs.GetInt(KBounty, 0);
    public static int BestDaylightLeft => PlayerPrefs.GetInt(KDaylight, 0);

    public static bool Report(bool won, int rivals, int bounty, int daylightLeft)
    {
        bool record = false;
        if (rivals > BestRivals)
        {
            PlayerPrefs.SetInt(KRivals, rivals);
            record = true;
        }
        if (bounty > BestBounty)
        {
            PlayerPrefs.SetInt(KBounty, bounty);
            record = true;
        }
        if (won && daylightLeft > BestDaylightLeft)
        {
            PlayerPrefs.SetInt(KDaylight, daylightLeft);
            record = true;
        }
        PlayerPrefs.Save();
        return record;
    }
}
