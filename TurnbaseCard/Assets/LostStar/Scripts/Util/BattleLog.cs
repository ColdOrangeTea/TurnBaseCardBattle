using UnityEngine;

/// <summary>
/// 可編譯期關閉的除錯 log 包裝，由 A_Good_Ink 使用 AI 生成。
///
/// 原理：方法標了 [System.Diagnostics.Conditional("BATTLE_LOG")]。
///   - 沒定義 BATTLE_LOG 這個編譯符號時，C# 編譯器會把「所有對這些方法的呼叫」整句移除，
///     連同傳進去的參數（例如 $"..." 字串組字）一起不編譯 → 執行期零成本、也不會洗 Console。
///   - 想看 log 時：Player Settings → Player → Scripting Define Symbols 加入 BATTLE_LOG，
///     不必改任何程式碼，全部 BattleLog.Log 就會恢復輸出。
///
/// 用法：把熱路徑（戰鬥/特效/骰子/地圖）的 Debug.Log 換成 BattleLog.Log。
/// 註：真正的錯誤請繼續用 Debug.LogError（永遠顯示，不要包）。
/// </summary>
public static class BattleLog
{
    /// <summary>控制是否編譯進 log 呼叫的符號。加到 Scripting Define Symbols 才會輸出。</summary>
    public const string Symbol = "BATTLE_LOG";

    [System.Diagnostics.Conditional(Symbol)]
    public static void Log(object message) => Debug.Log(message);

    [System.Diagnostics.Conditional(Symbol)]
    public static void Warn(object message) => Debug.LogWarning(message);
}
