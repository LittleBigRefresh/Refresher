namespace Refresher.Core.Verification;

public static class GameNames
{
    public static readonly string[] LBPWhitelist = ["littlebigplanet", "lbp", "リトルビッグプラネット", "리틀 빅 플래닛"];
    // TODO blacklist certain words for LBP too, to prevent users from using the LBP-specific patches
    // on non-LBP games which have LBP in their name (e.g. Karting and the LBP3 digital manual)
}