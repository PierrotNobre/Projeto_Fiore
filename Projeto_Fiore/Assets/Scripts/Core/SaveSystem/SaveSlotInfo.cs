using System;

[Serializable]
public class SaveSlotInfo
{
    public int SlotID;

    public bool HasSave;

    public string CharacterName =
        "Hero";

    public string CurrentCityID =
        "city_lunaris";

    public int Year = 235;

    public int Month = 1;

    public int Day = 1;

    public int Hour = 8;

    public string LastSavedAt;

    public string GetWorldDateLabel()
    {
        return
            $"Dia {Day} / Mes {Month} / Ano {Year}";
    }
}
