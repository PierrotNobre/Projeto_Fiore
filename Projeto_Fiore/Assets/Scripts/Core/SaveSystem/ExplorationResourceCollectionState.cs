using System;

[Serializable]
public class ExplorationResourceCollectionState
{
    public string AreaID;

    public string ResourceNodeID;

    public int Year;

    public int Month;

    public int Day;

    public bool Matches(
        string areaID,
        string resourceNodeID,
        TimeData time)
    {
        return AreaID == areaID &&
            ResourceNodeID == resourceNodeID &&
            time != null &&
            Year == time.Year &&
            Month == time.Month &&
            Day == time.Day;
    }
}
