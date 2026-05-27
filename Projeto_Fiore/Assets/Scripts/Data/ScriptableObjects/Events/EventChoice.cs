using System;
using System.Collections.Generic;

[Serializable]
public class EventChoice
{
    public string ChoiceText;

    public List<RequirementData> GenericRequirements =
        new();

    public EventConsequence Consequence;
}
