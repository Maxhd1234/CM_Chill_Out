using Verse;



public class ChillOutMod : Mod
{
    private static ChillOutMod _instance;

    public static ChillOutMod Instance => _instance;

    public ChillOutMod(ModContentPack content)
        : base(content)
    {
        _instance = this;
    }
}
