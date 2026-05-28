public class CharacterProgressionResult
{
    public int ExperienceGained;

    public int StartingLevel;

    public int FinalLevel;

    public int LevelsGained =>
        FinalLevel - StartingLevel;

    public int AttributePointsGained;

    public string LearnedSkillSummary;

    public bool LeveledUp =>
        LevelsGained > 0;
}
