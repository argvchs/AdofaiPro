using System.Collections.Generic;
using ADOFAI;
using HarmonyLib;

namespace AdofaiPro.Patching
{
    /// <summary>
    /// 读档（.adofai → 内存）：把可读事件名 "SetTileTint" 翻译成内部键 "900"。
    ///
    /// 为什么不用改 dict：<c>LevelEvent.Decode(dict, explicitEventType, isGlobal)</c> 里
    /// <c>text = explicitEventType ?? dict["eventType"]</c>，随后用 <c>text</c> 查
    /// <c>GCS.levelEventsInfo</c>。所以只要把 explicitEventType 设成注册时用的键即可，
    /// 原版逻辑原样跑完（floor / active / visible / locked / 各属性解码）。
    /// </summary>
    [HarmonyPatch(typeof(LevelEvent), nameof(LevelEvent.Decode))]
    internal static class LevelEventDecodePatch
    {
        internal static void Prefix(Dictionary<string, object> dict, ref string explicitEventType)
        {
            if (explicitEventType != null || dict == null)
            {
                return;
            }

            object raw;
            if (dict.TryGetValue("eventType", out raw) && (raw as string) == CustomEvent.FileName)
            {
                explicitEventType = CustomEvent.DictKey;
            }
        }
    }

    /// <summary>
    /// 存档（内存 → .adofai）：把内部键 "900" 写回可读事件名 "SetTileTint"。
    /// 原版 <c>LevelEvent.Encode</c> 写的是 <c>eventType.ToString()</c>（即 "900"），
    /// 这里在后置补丁里替换，保证关卡文件可读、可分享。
    /// </summary>
    [HarmonyPatch(typeof(LevelEvent), nameof(LevelEvent.Encode))]
    internal static class LevelEventEncodePatch
    {
        internal static void Postfix(Dictionary<string, object> __result)
        {
            if (__result == null)
            {
                return;
            }

            object raw;
            if (__result.TryGetValue("eventType", out raw) && (raw as string) == CustomEvent.DictKey)
            {
                __result["eventType"] = CustomEvent.FileName;
            }
        }
    }
}