using System;
using System.Collections.Generic;
using ADOFAI;
using HarmonyLib;
using UnityEngine;

namespace AdofaiPro.Patching
{
    /// <summary>
    /// 事件的实际效果：把指定砖（以及后续若干砖）染成指定颜色。
    ///
    /// 挂在 <c>scnGame.ApplyEventsToFloors</c> 之后：
    ///   - 编辑器每次改属性、加事件都会调用它（配合 PropertyInfo.affectsFloors），所以编辑器里即时可见；
    ///   - 游戏加载/重置关卡时也调用它，所以游玩时同样生效。
    /// 原版的 <c>switch (eventType)</c> 不认识 900，会静默忽略；这里补上行为。
    /// </summary>
    [HarmonyPatch(
        typeof(scnGame),
        nameof(scnGame.ApplyEventsToFloors),
        new Type[] { typeof(List<scrFloor>), typeof(LevelData), typeof(scrLevelMaker), typeof(List<LevelEvent>) })]
    internal static class ApplyEventsToFloorsPatch
    {
        internal static void Postfix(List<LevelEvent> events, List<scrFloor> floors)
        {
            try
            {
                if (events == null || floors == null)
                {
                    return;
                }

                for (int i = 0; i < events.Count; i++)
                {
                    LevelEvent ev = events[i];
                    if (ev == null || (int)ev.eventType != CustomEvent.TypeId)
                    {
                        continue;
                    }

                    try
                    {
                        ApplyTint(ev, floors);
                    }
                    catch (Exception e)
                    {
                        // 单个事件失败不影响其它事件 / 关卡其它部分
                        Main.Error("ApplyEventsToFloors: event at tile " + ev.floor + " failed: " + e);
                    }
                }
            }
            catch (Exception e)
            {
                Main.Error("ApplyEventsToFloors patch failed: " + e);
            }
        }

        private static void ApplyTint(LevelEvent ev, List<scrFloor> floors)
        {
            int floorIndex = ev.floor;
            if (floorIndex < 0 || floorIndex >= floors.Count)
            {
                return;
            }

            Color color = HexColor.Parse(ev.GetString("tintColor"), Color.white);
            float opacity = ev.ContainsKey("tintOpacity") ? ev.GetFloat("tintOpacity") : 100f;
            color.a *= Mathf.Clamp01(opacity / 100f);

            int ahead = ev.ContainsKey("tilesAhead") ? ev.GetInt("tilesAhead") : 0;
            if (ahead < 0)
            {
                ahead = 0;
            }

            int last = Math.Min(floors.Count - 1, floorIndex + ahead);
            for (int i = floorIndex; i <= last; i++)
            {
                scrFloor floor = floors[i];
                if (floor == null || floor.floorRenderer == null)
                {
                    continue;
                }

                // SetColor 同时更新 color 与 deselectedColor，编辑器的选中高亮仍能正常叠加。
                floor.SetColor(color);
            }
        }
    }
}
