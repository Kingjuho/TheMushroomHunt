/// <summary>
/// TitleScene에서 선택한 시작 모드를 MainScene 첫 진입 시점까지 전달하는 런타임 전용 홀더
/// </summary>
public static class GameStartContext
{
    private static bool? _shouldApplyInitialLoadOverride;

    public static void StartNewGame()
    {
        _shouldApplyInitialLoadOverride = false;
    }

    public static void StartContinue()
    {
        _shouldApplyInitialLoadOverride = true;
    }

    public static bool ConsumeShouldApplyInitialLoad(bool fallbackValue = true)
    {
        bool shouldApplyInitialLoad = _shouldApplyInitialLoadOverride ?? fallbackValue;
        _shouldApplyInitialLoadOverride = null;
        return shouldApplyInitialLoad;
    }
}
