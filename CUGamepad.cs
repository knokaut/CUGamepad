// CU Gamepad v0.2 — поддержка геймпада (XInput / PS4 через Steam Input) для Casualties: Unknown.
// BepInEx 5 (Unity Mono) + Harmony.
//
// Что умеет:
//  * Кнопки/стики -> игровые действия через родной KeyBinds.GetBind (учитывает переназначенные клавиши).
//  * Правый стик = курсор ОС. Работает в инвентаре, меню и минииграх.
//  * Миниигры (бинт, шприц, осколки, вывихи, переломы...): держишь L2 (захват) + двигаешь правый стик.
//  * Share (Back) коротким нажатием: скорость 1 -> 2 -> 3 -> 1. Share + кнопка = "второй слой".
//  * D-pad: прыжок курсора по слотам инвентаря (InvButton) в нужную сторону.
//  * Вибрация: удары по игроку (Limb.ImpactDamage), переломы (Limb.BreakBone), пульс при кровотечении.
//  * Подсказки: названия кнопок геймпада вместо клавиатурных там, где игра зовёт KeyBinds.GetBindName,
//    плюс оверлей: в миниигре — контекстная подсказка, F9 — общая шпаргалка.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CUGamepad
{
    [BepInPlugin(Guid, "CU Gamepad", "0.2.0")]
    public class GamepadPlugin : BaseUnityPlugin
    {
        public const string Guid = "com.local.casualtiesunknown.gamepad";

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> CursorSpeed;
        internal static ConfigEntry<float> CursorCurve;
        internal static ConfigEntry<float> StickDeadzone;
        internal static ConfigEntry<float> MoveThreshold;
        internal static ConfigEntry<bool> NativeMouseClicks;

        internal static ConfigEntry<string> GrabButton;          // захват в миниигре
        internal static ConfigEntry<string> ExitButton;          // выход из миниигры (= Esc)
        internal static ConfigEntry<float> MinigameCursorScale;  // замедление курсора в миниигре
        internal static ConfigEntry<string> SpeedCycleButton;    // цикл скоростей 1-2-3
        internal static ConfigEntry<bool> DpadInventoryNav;      // D-pad прыгает по слотам

        internal static ConfigEntry<bool> RumbleEnabled;
        internal static ConfigEntry<float> RumbleStrength;
        internal static ConfigEntry<float> RumbleFullDamage;
        internal static ConfigEntry<bool> RumbleBleed;
        internal static ConfigEntry<float> BleedThreshold;
        internal static ConfigEntry<float> BleedScale;

        internal static ConfigEntry<bool> ShowHints;
        internal static ConfigEntry<string> HintStyle;           // PS4 или Xbox
        internal static ConfigEntry<bool> DebugValues;

        private Harmony harmony;
        private GUIStyle style;
        private bool legend;

        private void Awake()
        {
            Log = Logger;

            Enabled = Config.Bind("General", "Enabled", true, "Включить геймпад.");
            CursorSpeed = Config.Bind("Cursor", "Speed", 0.9f, "Скорость курсора (высот экрана в секунду при полном отклонении правого стика).");
            CursorCurve = Config.Bind("Cursor", "Curve", 2.0f, "Кривая отклика: 1 = линейно, 2 = плавный старт (точнее в центре).");
            StickDeadzone = Config.Bind("Sticks", "Deadzone", 0.20f, "Мёртвая зона стиков (0..0.9).");
            MoveThreshold = Config.Bind("Sticks", "MoveThreshold", 0.50f, "Насколько отклонить левый стик, чтобы считалось left/right/up/down.");
            NativeMouseClicks = Config.Bind("Compat", "NativeMouseClicks", false,
                "Если клики от геймпада не срабатывают - поставь true: клики пойдут настоящими событиями мыши (mouse_event) вместо Harmony-патча.");

            GrabButton = Config.Bind("Minigames", "GrabButton", "LT", "Кнопка 'схватить' в миниигре (бинт, шприц, осколки, вывихи, переломы). Внутри миниигры она работает как ЛКМ.");
            ExitButton = Config.Bind("Minigames", "ExitButton", "B", "Кнопка выхода из миниигры (нажимает действие 'pause' = Esc). Нужна для шприца и бинта; вывих и осколки закрываются сами. Start тоже работает.");
            MinigameCursorScale = Config.Bind("Minigames", "CursorScale", 0.6f, "Множитель скорости курсора внутри миниигры (меньше = точнее).");
            SpeedCycleButton = Config.Bind("Buttons", "SpeedCycle", "Back", "Кнопка цикла скоростей 1-2-3. 'Back' = Share/View: срабатывает при коротком нажатии, а Back+кнопка остаётся вторым слоем.");
            DpadInventoryNav = Config.Bind("Buttons", "DpadInventoryNav", true, "D-pad (без Back) перепрыгивает курсором на соседний слот инвентаря/тела.");

            RumbleEnabled = Config.Bind("Rumble", "Enabled", true, "Вибрация геймпада.");
            RumbleStrength = Config.Bind("Rumble", "Strength", 1.0f, "Общая сила вибрации 0..1.");
            RumbleFullDamage = Config.Bind("Rumble", "FullDamage", 20f, "Урон, при котором вибрация максимальная (подгони после теста, смотри DebugValues).");
            RumbleBleed = Config.Bind("Rumble", "BleedHeartbeat", true, "Пульсирующая вибрация в такт сердцу при кровотечении.");
            BleedThreshold = Config.Bind("Rumble", "BleedThreshold", 0.01f, "С какой скорости кровотечения (Body.totalBleedSpeed) начинается пульсация.");
            BleedScale = Config.Bind("Rumble", "BleedScale", 5f, "Насколько сильно кровотечение усиливает пульсацию.");

            ShowHints = Config.Bind("Hints", "Enabled", true, "Подсказки геймпада: контекстная в миниигре и шпаргалка по F9.");
            HintStyle = Config.Bind("Hints", "Style", "PS4", "Названия кнопок: PS4 или Xbox.");
            DebugValues = Config.Bind("Debug", "LogValues", false, "Писать в лог значения боли/кровотечения/урона раз в секунду (для подгонки вибрации).");

            Pad.InitBinds(Config);

            try
            {
                harmony = new Harmony(Guid);
                harmony.PatchAll(typeof(GamepadPlugin).Assembly);
                Log.LogInfo("Harmony-патчи Input применены.");

                Hooks.Apply(harmony);
            }
            catch (Exception e)
            {
                Log.LogError("Не удалось применить Harmony-патчи: " + e);
            }
        }

        private void Update()
        {
            Pad.Tick();
            Rumble.Tick();
            if (Input.GetKeyDown(KeyCode.F9)) legend = !legend;
        }

        private void OnGUI()
        {
            if (!ShowHints.Value) return;
            if (!Pad.Connected && !legend) return;

            string text = null;
            if (Pad.InMinigame && Pad.Connected) text = Hints.MinigameHint();
            else if (legend) text = Hints.Legend();
            if (text == null) return;

            if (style == null)
            {
                style = new GUIStyle(GUI.skin.box);
                style.fontSize = 14;
                style.alignment = TextAnchor.UpperLeft;
                style.wordWrap = false;
                style.padding = new RectOffset(10, 10, 8, 8);
                style.normal.textColor = Color.white;
            }

            var content = new GUIContent(text);
            Vector2 size = style.CalcSize(content);
            Rect r = Pad.InMinigame
                ? new Rect((Screen.width - size.x) / 2f, Screen.height - size.y - 14f, size.x, size.y)
                : new Rect(12f, 12f, size.x, size.y);
            GUI.Box(r, content, style);
        }

        private void OnDestroy()
        {
            Rumble.Stop();
            Pad.ReleaseNativeMouse();
            if (harmony != null) harmony.UnpatchSelf();
        }
    }

    // ------------------------------------------------------------------------------------------
    // Кнопки геймпада
    // ------------------------------------------------------------------------------------------
    [Flags]
    internal enum Btn : uint
    {
        None = 0,
        DUp = 0x0001, DDown = 0x0002, DLeft = 0x0004, DRight = 0x0008,
        Start = 0x0010, Back = 0x0020, LS = 0x0040, RS = 0x0080,
        LB = 0x0100, RB = 0x0200,
        A = 0x1000, B = 0x2000, X = 0x4000, Y = 0x8000,
        LT = 0x10000, RT = 0x20000,
    }

    internal class Bind
    {
        public string Action;
        public ConfigEntry<string> Cfg;
        public Btn Btn;
        public bool Layer; // "Back+X" — нужно держать Back (Share/View)
    }

    // ------------------------------------------------------------------------------------------
    // XInput
    // ------------------------------------------------------------------------------------------
    internal static class XInput
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Gamepad
        {
            public ushort Buttons;
            public byte LeftTrigger, RightTrigger;
            public short LX, LY, RX, RY;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct State
        {
            public uint Packet;
            public Gamepad Pad;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Vibration
        {
            public ushort Left, Right;
        }

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        private static extern uint GetState14(uint index, out State state);
        [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
        private static extern uint GetState910(uint index, out State state);
        [DllImport("xinput1_4.dll", EntryPoint = "XInputSetState")]
        private static extern uint SetState14(uint index, ref Vibration v);
        [DllImport("xinput9_1_0.dll", EntryPoint = "XInputSetState")]
        private static extern uint SetState910(uint index, ref Vibration v);

        private static int mode; // 0 = не проверяли, 1 = 1.4, 2 = 9.1.0, -1 = нет XInput

        public static bool TryGet(uint index, out State state)
        {
            state = default(State);
            if (mode == -1) return false;
            try
            {
                if (mode == 0 || mode == 1)
                {
                    uint r = GetState14(index, out state);
                    mode = 1;
                    return r == 0;
                }
            }
            catch (DllNotFoundException) { mode = 2; }
            catch (EntryPointNotFoundException) { mode = 2; }

            try
            {
                uint r = GetState910(index, out state);
                mode = 2;
                return r == 0;
            }
            catch (Exception)
            {
                mode = -1;
                GamepadPlugin.Log.LogWarning("XInput недоступен — геймпад работать не будет.");
                return false;
            }
        }

        public static void SetVibration(uint index, ushort left, ushort right)
        {
            var v = new Vibration { Left = left, Right = right };
            try
            {
                if (mode == 1) SetState14(index, ref v);
                else if (mode == 2) SetState910(index, ref v);
            }
            catch (Exception) { /* вибрация — не критично */ }
        }
    }

    // ------------------------------------------------------------------------------------------
    // Win32
    // ------------------------------------------------------------------------------------------
    internal static class Win
    {
        [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT p);
        [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")] public static extern IntPtr GetActiveWindow();
        [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hWnd, out RECT rect);
        [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hWnd, ref POINT p);
        [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);

        public const uint LEFTDOWN = 0x0002, LEFTUP = 0x0004;
        public const uint RIGHTDOWN = 0x0008, RIGHTUP = 0x0010;
        public const uint MIDDLEDOWN = 0x0020, MIDDLEUP = 0x0040;
    }

    // ------------------------------------------------------------------------------------------
    // Рефлексия по игровым классам (без ссылки на Assembly-CSharp)
    // ------------------------------------------------------------------------------------------
    internal static class Game
    {
        private static bool init;
        private static FieldInfo mgMain, mgCurrent;     // MinigameBase.main / currentMinigame
        private static FieldInfo pcMain, pcBody;        // PlayerCamera.main / body
        private static FieldInfo limbBody;              // Limb.body

        private static void Init()
        {
            if (init) return;
            init = true;
            try
            {
                Type mg = AccessTools.TypeByName("MinigameBase");
                if (mg != null)
                {
                    mgMain = AccessTools.Field(mg, "main");
                    mgCurrent = AccessTools.Field(mg, "currentMinigame");
                }
                Type pc = AccessTools.TypeByName("PlayerCamera");
                if (pc != null)
                {
                    pcMain = AccessTools.Field(pc, "main");
                    pcBody = AccessTools.Field(pc, "body");
                }
                Type limb = AccessTools.TypeByName("Limb");
                if (limb != null) limbBody = AccessTools.Field(limb, "body");

                if (mgMain == null || mgCurrent == null) GamepadPlugin.Log.LogWarning("MinigameBase.main/currentMinigame не найдены — режим миниигры (L2-захват) недоступен.");
                if (pcMain == null || pcBody == null) GamepadPlugin.Log.LogWarning("PlayerCamera.main/body не найдены — вибрация при кровотечении недоступна.");
            }
            catch (Exception e)
            {
                GamepadPlugin.Log.LogWarning("Game.Init: " + e.Message);
            }
        }

        public static bool Dead(object o)
        {
            if (o == null) return true;
            var u = o as UnityEngine.Object;
            return !ReferenceEquals(u, null) && !u; // уничтоженный Unity-объект
        }

        public static bool InMinigame()
        {
            Init();
            if (mgMain == null || mgCurrent == null) return false;
            try
            {
                object inst = mgMain.GetValue(null);
                if (Dead(inst)) return false;
                return mgCurrent.GetValue(inst) != null;
            }
            catch (Exception) { return false; }
        }

        public static object PlayerBody()
        {
            Init();
            if (pcMain == null || pcBody == null) return null;
            try
            {
                object pc = pcMain.GetValue(null);
                if (Dead(pc)) return null;
                object body = pcBody.GetValue(pc);
                return Dead(body) ? null : body;
            }
            catch (Exception) { return null; }
        }

        public static bool IsPlayerLimb(object limb)
        {
            Init();
            if (limbBody == null || limb == null) return false;
            try
            {
                object body = PlayerBody();
                return body != null && ReferenceEquals(limbBody.GetValue(limb), body);
            }
            catch (Exception) { return false; }
        }

        public static float Float(object o, string field)
        {
            if (o == null) return 0f;
            try
            {
                FieldInfo f = AccessTools.Field(o.GetType(), field);
                return f == null ? 0f : Convert.ToSingle(f.GetValue(o));
            }
            catch (Exception) { return 0f; }
        }
    }

    // ------------------------------------------------------------------------------------------
    // Состояние "виртуальной клавиатуры" от геймпада
    // ------------------------------------------------------------------------------------------
    internal static class Pad
    {
        private const int KeyCount = 512;

        private static bool[] cur = new bool[KeyCount];
        private static bool[] prev = new bool[KeyCount];
        private static int lastFrame = -1;

        private static Bind[] binds;
        private static Func<string, KeyCode> getBind;
        private static bool getBindTried;

        private static int padIndex = -1;
        private static float nextScan;
        private static bool wasConnected;

        private static uint prevMask;
        private static bool backChord;
        private static int speedStep = 1;

        private static float cx, cy;
        private static Win.POINT lastSet;
        private static bool haveCursor;

        private static readonly bool[] nativeDown = new bool[3];

        public static bool Connected;
        public static bool InMinigame;
        public static int PadIndex { get { return padIndex; } }

        // Значения: Кнопка  или  Back+Кнопка
        private static readonly string[,] Defaults =
        {
            { "attack",          "RT"          },  // ЛКМ
            { "iteminteract",    "LT"          },  // ПКМ (в миниигре LT = захват, см. GrabButton)
            { "jump",            "A"           },
            { "switchhands",     "B"           },
            { "woundview",       "X"           },
            { "toggleinventory", "Y"           },
            { "expanddesc",      "LB"          },  // Shift
            { "altview",         "RB"          },  // Ctrl
            { "throw",           "LS"          },  // L3
            { "ragdoll",         "RS"          },  // R3
            { "pause",           "Start"       },
            { "craft",           "Back+DUp"    },
            { "favourite",       "Back+DDown"  },
            { "bark",            "Back+DLeft"  },
            { "customizeitem",   "Back+DRight" },
            { "marksmanrack",    "Back+X"      },
            { "marksmanunload",  "Back+Y"      },
            { "console",         "None"        },
        };

        private static KeyCode DefaultKey(string action)
        {
            switch (action)
            {
                case "jump": return KeyCode.Space;
                case "left": return KeyCode.A;
                case "right": return KeyCode.D;
                case "down": return KeyCode.S;
                case "up": return KeyCode.W;
                case "attack": return KeyCode.Mouse0;
                case "iteminteract": return KeyCode.Mouse1;
                case "throw": return KeyCode.T;
                case "switchhands": return KeyCode.Q;
                case "ragdoll": return KeyCode.X;
                case "woundview": return KeyCode.R;
                case "craft": return KeyCode.C;
                case "favourite": return KeyCode.F;
                case "customizeitem": return KeyCode.N;
                case "toggleinventory": return KeyCode.Tab;
                case "speed1": return KeyCode.Alpha1;
                case "speed2": return KeyCode.Alpha2;
                case "speed3": return KeyCode.Alpha3;
                case "expanddesc": return KeyCode.LeftShift;
                case "altview": return KeyCode.LeftControl;
                case "bark": return KeyCode.B;
                case "console": return KeyCode.BackQuote;
                case "pause": return KeyCode.Escape;
                case "marksmanrack": return KeyCode.E;
                case "marksmanunload": return KeyCode.G;
            }
            return KeyCode.None;
        }

        public static Bind[] Binds { get { return binds; } }

        public static Bind FindBind(string action)
        {
            if (binds == null) return null;
            for (int i = 0; i < binds.Length; i++)
                if (binds[i].Action == action && binds[i].Btn != Btn.None) return binds[i];
            return null;
        }

        public static void InitBinds(ConfigFile config)
        {
            int n = Defaults.GetLength(0);
            binds = new Bind[n];
            for (int i = 0; i < n; i++)
            {
                var b = new Bind { Action = Defaults[i, 0] };
                b.Cfg = config.Bind("Buttons", b.Action, Defaults[i, 1],
                    "Кнопка геймпада для действия '" + b.Action + "'. Значения: A B X Y LB RB LT RT LS RS DUp DDown DLeft DRight Start Back None. " +
                    "Можно 'Back+X' — нужно держать Back (Share/View).");
                Parse(b);
                var captured = b;
                b.Cfg.SettingChanged += (s, e) => Parse(captured);
                binds[i] = b;
            }
        }

        public static Btn ParseBtn(string s, out bool layer)
        {
            layer = false;
            s = (s ?? "").Trim();
            if (s.StartsWith("Back+", StringComparison.OrdinalIgnoreCase))
            {
                layer = true;
                s = s.Substring(5);
            }
            Btn r;
            return Enum.TryParse(s, true, out r) ? r : Btn.None;
        }

        private static void Parse(Bind b)
        {
            bool layer;
            b.Btn = ParseBtn(b.Cfg.Value, out layer);
            b.Layer = layer;
            if (b.Btn == Btn.None && !string.Equals((b.Cfg.Value ?? "").Trim(), "None", StringComparison.OrdinalIgnoreCase))
                GamepadPlugin.Log.LogWarning("Не понял кнопку '" + b.Cfg.Value + "' для действия " + b.Action);
        }

        // ---------------- действие -> KeyCode ----------------
        public static KeyCode Resolve(string action)
        {
            if (!getBindTried)
            {
                getBindTried = true;
                try
                {
                    Type t = AccessTools.TypeByName("KeyBinds");
                    MethodInfo m = t != null ? AccessTools.Method(t, "GetBind", new[] { typeof(string) }) : null;
                    if (m != null && m.ReturnType == typeof(KeyCode))
                    {
                        getBind = (Func<string, KeyCode>)Delegate.CreateDelegate(typeof(Func<string, KeyCode>), m);
                        GamepadPlugin.Log.LogInfo("KeyBinds.GetBind найден — используем клавиши из настроек игры.");
                    }
                    else GamepadPlugin.Log.LogWarning("KeyBinds.GetBind не найден — используем клавиши по умолчанию.");
                }
                catch (Exception e)
                {
                    GamepadPlugin.Log.LogWarning("Ошибка при поиске KeyBinds.GetBind: " + e.Message);
                }
            }

            if (getBind != null)
            {
                try { return getBind(action); }
                catch (Exception) { /* настройки ещё не загружены */ }
            }
            return DefaultKey(action);
        }

        private static void Press(KeyCode k)
        {
            int i = (int)k;
            if (i > 0 && i < KeyCount) cur[i] = true;
        }

        // ---------------- API для патчей ----------------
        public static void Tick()
        {
            int f = Time.frameCount;
            if (f == lastFrame) return;
            lastFrame = f;
            Poll();
        }

        public static bool Held(KeyCode k)
        {
            Tick();
            int i = (int)k;
            return i > 0 && i < KeyCount && cur[i];
        }

        public static bool Down(KeyCode k)
        {
            Tick();
            int i = (int)k;
            return i > 0 && i < KeyCount && cur[i] && !prev[i];
        }

        public static bool Up(KeyCode k)
        {
            Tick();
            int i = (int)k;
            return i > 0 && i < KeyCount && !cur[i] && prev[i];
        }

        // ---------------- опрос ----------------
        private static void Poll()
        {
            var t = prev; prev = cur; cur = t;
            Array.Clear(cur, 0, KeyCount);

            if (GamepadPlugin.Enabled == null || !GamepadPlugin.Enabled.Value || !Application.isFocused)
            {
                Connected = false;
                InMinigame = false;
                prevMask = 0;
                SyncNativeMouse();
                return;
            }

            XInput.State s;
            if (!ReadPad(out s))
            {
                if (wasConnected) GamepadPlugin.Log.LogInfo("Геймпад отключён.");
                wasConnected = false;
                Connected = false;
                InMinigame = false;
                prevMask = 0;
                SyncNativeMouse();
                return;
            }
            if (!wasConnected)
            {
                wasConnected = true;
                GamepadPlugin.Log.LogInfo("Геймпад подключён (XInput #" + padIndex + ").");
            }
            Connected = true;
            InMinigame = Game.InMinigame();

            uint mask = s.Pad.Buttons;
            if (s.Pad.LeftTrigger >= 100) mask |= (uint)Btn.LT;
            if (s.Pad.RightTrigger >= 100) mask |= (uint)Btn.RT;
            bool layer = (mask & (uint)Btn.Back) != 0;
            uint edge = mask & ~prevMask;

            // захват в миниигре: GrabButton работает как "attack" (ЛКМ)
            bool glayer;
            Btn grab = ParseBtn(GamepadPlugin.GrabButton.Value, out glayer);
            if (InMinigame && grab != Btn.None && glayer == layer && (mask & (uint)grab) != 0)
                Press(Resolve("attack"));

            // выход из миниигры: ExitButton нажимает "pause" (Esc) один раз
            bool elayer;
            Btn exit = ParseBtn(GamepadPlugin.ExitButton.Value, out elayer);
            if (InMinigame && exit != Btn.None && elayer == layer && (edge & (uint)exit) != 0)
                Press(Resolve("pause"));

            // кнопки -> действия
            for (int i = 0; i < binds.Length; i++)
            {
                Bind b = binds[i];
                if (b.Btn == Btn.None) continue;
                if (b.Layer != layer) continue;
                if ((mask & (uint)b.Btn) == 0) continue;
                if (InMinigame && b.Btn == grab && glayer == b.Layer) continue; // кнопка занята под захват
                if (InMinigame && b.Btn == exit && elayer == b.Layer) continue; // кнопка занята под выход
                Press(Resolve(b.Action));
            }

            // цикл скоростей
            bool slayer;
            Btn cycle = ParseBtn(GamepadPlugin.SpeedCycleButton.Value, out slayer);
            if (cycle == Btn.Back)
            {
                bool backNow = layer;
                bool backPrev = (prevMask & (uint)Btn.Back) != 0;
                if (backNow && !backPrev) backChord = false;
                if (backNow && (mask & ~(uint)Btn.Back) != 0) backChord = true;
                if (!backNow && backPrev && !backChord) CycleSpeed();
            }
            else if (cycle != Btn.None && !layer && (edge & (uint)cycle) != 0)
            {
                CycleSpeed();
            }

            // D-pad -> слоты инвентаря
            if (GamepadPlugin.DpadInventoryNav.Value && !layer && !InMinigame)
            {
                if ((edge & (uint)Btn.DUp) != 0) Nav.Snap(0, 1);
                else if ((edge & (uint)Btn.DDown) != 0) Nav.Snap(0, -1);
                else if ((edge & (uint)Btn.DLeft) != 0) Nav.Snap(-1, 0);
                else if ((edge & (uint)Btn.DRight) != 0) Nav.Snap(1, 0);
            }

            // левый стик -> движение
            float dz = Mathf.Clamp(GamepadPlugin.StickDeadzone.Value, 0f, 0.9f);
            float lx = s.Pad.LX / 32768f, ly = s.Pad.LY / 32768f;
            float th = Mathf.Max(GamepadPlugin.MoveThreshold.Value, dz);
            if (lx < -th) Press(Resolve("left"));
            if (lx > th) Press(Resolve("right"));
            if (ly > th) Press(Resolve("up"));
            if (ly < -th) Press(Resolve("down"));

            // правый стик -> курсор
            MoveCursor(s.Pad.RX / 32768f, s.Pad.RY / 32768f, dz);

            prevMask = mask;
            SyncNativeMouse();
        }

        private static void CycleSpeed()
        {
            speedStep = speedStep % 3 + 1;
            Press(Resolve("speed" + speedStep));
            GamepadPlugin.Log.LogInfo("Скорость -> " + speedStep);
        }

        private static bool ReadPad(out XInput.State s)
        {
            s = default(XInput.State);

            if (padIndex >= 0)
            {
                if (XInput.TryGet((uint)padIndex, out s)) return true;
                padIndex = -1;
                nextScan = Time.unscaledTime + 1f;
                return false;
            }

            // XInputGetState на пустых слотах медленный — сканируем раз в секунду.
            if (Time.unscaledTime < nextScan) return false;
            nextScan = Time.unscaledTime + 1f;
            for (uint i = 0; i < 4; i++)
            {
                if (XInput.TryGet(i, out s))
                {
                    padIndex = (int)i;
                    return true;
                }
            }
            return false;
        }

        // ---------------- курсор ----------------
        private static bool ClientOrigin(out Win.POINT origin)
        {
            origin = new Win.POINT();
            IntPtr hwnd = Win.GetActiveWindow();
            if (hwnd == IntPtr.Zero) return false;
            return Win.ClientToScreen(hwnd, ref origin);
        }

        private static void MoveCursor(float rx, float ry, float dz)
        {
            float mag = Mathf.Sqrt(rx * rx + ry * ry);
            if (mag <= dz) return;

            float scaled = Mathf.Clamp01((mag - dz) / (1f - dz));
            scaled = Mathf.Pow(scaled, Mathf.Max(0.5f, GamepadPlugin.CursorCurve.Value));
            float dx = rx / mag * scaled;
            float dy = ry / mag * scaled;

            Win.POINT p;
            if (!Win.GetCursorPos(out p)) return;

            float fx = p.X, fy = p.Y;
            if (haveCursor && Math.Abs(p.X - lastSet.X) <= 1 && Math.Abs(p.Y - lastSet.Y) <= 1)
            {
                fx = cx; fy = cy;
            }

            float speed = GamepadPlugin.CursorSpeed.Value * Screen.height;
            if (InMinigame) speed *= Mathf.Clamp(GamepadPlugin.MinigameCursorScale.Value, 0.05f, 2f);
            float dt = Time.unscaledDeltaTime;
            fx += dx * speed * dt;
            fy -= dy * speed * dt; // XInput Y вверх, экран вниз

            IntPtr hwnd = Win.GetActiveWindow();
            if (hwnd != IntPtr.Zero)
            {
                Win.RECT rc;
                Win.POINT origin = new Win.POINT();
                if (Win.GetClientRect(hwnd, out rc) && Win.ClientToScreen(hwnd, ref origin))
                {
                    fx = Mathf.Clamp(fx, origin.X, origin.X + rc.Right - 1);
                    fy = Mathf.Clamp(fy, origin.Y, origin.Y + rc.Bottom - 1);
                }
            }

            SetCursor(fx, fy);
        }

        private static void SetCursor(float fx, float fy)
        {
            int ix = (int)Math.Round(fx), iy = (int)Math.Round(fy);
            Win.SetCursorPos(ix, iy);
            cx = fx; cy = fy;
            lastSet.X = ix; lastSet.Y = iy;
            haveCursor = true;
        }

        // Переместить курсор в точку экрана Unity (начало координат внизу слева)
        public static void WarpTo(Vector2 unityScreen)
        {
            Win.POINT origin;
            if (!ClientOrigin(out origin)) return;
            SetCursor(origin.X + unityScreen.x, origin.Y + (Screen.height - unityScreen.y));
        }

        // ---------------- запасной режим: настоящие клики мыши ----------------
        private static void SyncNativeMouse()
        {
            bool native = GamepadPlugin.NativeMouseClicks != null && GamepadPlugin.NativeMouseClicks.Value;
            for (int b = 0; b < 3; b++)
            {
                int key = (int)KeyCode.Mouse0 + b;
                bool want = false;
                if (native)
                {
                    want = cur[key];
                    cur[key] = false;
                    prev[key] = false;
                }

                if (want != nativeDown[b])
                {
                    nativeDown[b] = want;
                    uint flag;
                    if (b == 0) flag = want ? Win.LEFTDOWN : Win.LEFTUP;
                    else if (b == 1) flag = want ? Win.RIGHTDOWN : Win.RIGHTUP;
                    else flag = want ? Win.MIDDLEDOWN : Win.MIDDLEUP;
                    Win.mouse_event(flag, 0, 0, 0, UIntPtr.Zero);
                }
            }
        }

        public static void ReleaseNativeMouse()
        {
            for (int b = 0; b < 3; b++)
            {
                if (!nativeDown[b]) continue;
                nativeDown[b] = false;
                uint flag = b == 0 ? Win.LEFTUP : (b == 1 ? Win.RIGHTUP : Win.MIDDLEUP);
                Win.mouse_event(flag, 0, 0, 0, UIntPtr.Zero);
            }
        }
    }

    // ------------------------------------------------------------------------------------------
    // D-pad: прыжок курсора по слотам инвентаря
    // ------------------------------------------------------------------------------------------
    internal static class Nav
    {
        private static Type invType;

        public static bool Snap(int dx, int dy)
        {
            if (invType == null) invType = AccessTools.TypeByName("InvButton");
            if (invType == null) return false;

            UnityEngine.Object[] objs = UnityEngine.Object.FindObjectsOfType(invType);
            Vector2 cur = Input.mousePosition;
            Vector2 dir = new Vector2(dx, dy);

            float best = float.MaxValue;
            Vector2 bestP = Vector2.zero;
            bool found = false;

            float nearestD = float.MaxValue;
            Vector2 nearestP = Vector2.zero;

            for (int i = 0; i < objs.Length; i++)
            {
                Component c = objs[i] as Component;
                if (c == null || !c.gameObject.activeInHierarchy) continue;
                RectTransform rt = c.transform as RectTransform;
                if (rt == null) continue;

                Canvas cv = c.GetComponentInParent<Canvas>();
                Camera cam = (cv != null && cv.renderMode != RenderMode.ScreenSpaceOverlay) ? cv.worldCamera : null;
                Vector2 p = RectTransformUtility.WorldToScreenPoint(cam, rt.TransformPoint(rt.rect.center));
                if (p.x < 0f || p.y < 0f || p.x > Screen.width || p.y > Screen.height) continue;

                Vector2 v = p - cur;
                float dist = v.magnitude;
                if (dist < nearestD) { nearestD = dist; nearestP = p; }
                if (dist < 8f) continue; // курсор уже на этом слоте

                float along = Vector2.Dot(v, dir);
                if (along <= 0f) continue;
                float perp = Mathf.Abs(v.x * dir.y - v.y * dir.x);
                if (perp > along * 1.5f) continue; // слишком сбоку

                float score = along + perp * 2f;
                if (score < best) { best = score; bestP = p; found = true; }
            }

            // курсор не стоит ни на одном слоте -> прыгаем на ближайший
            if (!found && nearestD < float.MaxValue && nearestD >= 8f)
            {
                bestP = nearestP;
                found = true;
            }

            if (!found) return false;
            Pad.WarpTo(bestP);
            return true;
        }
    }

    // ------------------------------------------------------------------------------------------
    // Вибрация
    // ------------------------------------------------------------------------------------------
    internal static class Rumble
    {
        private static float hitLeft, hitRight, hitStart, hitEnd;
        private static float beatStrength, beatEnd;
        private static float lastHeart;
        private static ushort sentL, sentR;
        private static float nextDebug;

        public static bool IsPlayerLimb(object limb) { return Game.IsPlayerLimb(limb); }

        public static void Hit(float amount)
        {
            float full = Mathf.Max(0.01f, GamepadPlugin.RumbleFullDamage.Value);
            float s = Mathf.Max(0.25f, Mathf.Clamp01(Mathf.Abs(amount) / full));
            Trigger(s, s * 0.8f, 0.15f + 0.30f * s);
        }

        public static void Big()
        {
            Trigger(1f, 1f, 0.6f);
        }

        private static void Trigger(float left, float right, float duration)
        {
            float now = Time.unscaledTime;
            float curL = now < hitEnd ? hitLeft * (hitEnd - now) / Mathf.Max(0.001f, hitEnd - hitStart) : 0f;
            if (left >= curL)
            {
                hitLeft = left; hitRight = right;
                hitStart = now; hitEnd = now + duration;
            }
        }

        public static void Tick()
        {
            bool on = GamepadPlugin.RumbleEnabled != null && GamepadPlugin.RumbleEnabled.Value
                      && Pad.Connected && Pad.PadIndex >= 0 && Time.timeScale > 0f;

            float l = 0f, r = 0f;
            if (on)
            {
                float now = Time.unscaledTime;
                if (now < hitEnd)
                {
                    float k = (hitEnd - now) / Mathf.Max(0.001f, hitEnd - hitStart);
                    l = hitLeft * k;
                    r = hitRight * k;
                }

                object body = Game.PlayerBody();
                if (body != null)
                {
                    float bleed = Game.Float(body, "totalBleedSpeed");
                    float heart = Game.Float(body, "heartProg");

                    if (GamepadPlugin.RumbleBleed.Value && bleed > GamepadPlugin.BleedThreshold.Value)
                    {
                        bool beat = heart < lastHeart - 0.5f || Mathf.FloorToInt(heart) != Mathf.FloorToInt(lastHeart);
                        if (beat)
                        {
                            beatStrength = Mathf.Clamp01(0.2f + bleed * GamepadPlugin.BleedScale.Value) * 0.6f;
                            beatEnd = now + 0.12f;
                        }
                    }
                    lastHeart = heart;
                    if (now < beatEnd) l = Mathf.Max(l, beatStrength);

                    if (GamepadPlugin.DebugValues.Value && now >= nextDebug)
                    {
                        nextDebug = now + 1f;
                        GamepadPlugin.Log.LogInfo(string.Format(
                            "[debug] bleed={0:F4} heartProg={1:F3} pain={2:F2} painShock={3:F2} consciousness={4:F2}",
                            bleed, heart, Game.Float(body, "averagePain"), Game.Float(body, "painShock"), Game.Float(body, "consciousness")));
                    }
                }
            }

            float strength = Mathf.Clamp01(GamepadPlugin.RumbleStrength != null ? GamepadPlugin.RumbleStrength.Value : 1f);
            ushort ul = (ushort)(Mathf.Clamp01(l * strength) * 65535f);
            ushort ur = (ushort)(Mathf.Clamp01(r * strength) * 65535f);
            if (ul != sentL || ur != sentR)
            {
                sentL = ul; sentR = ur;
                if (Pad.PadIndex >= 0) XInput.SetVibration((uint)Pad.PadIndex, ul, ur);
            }
        }

        public static void Stop()
        {
            sentL = 0; sentR = 0;
            if (Pad.PadIndex >= 0) XInput.SetVibration((uint)Pad.PadIndex, 0, 0);
        }
    }

    // ------------------------------------------------------------------------------------------
    // Подсказки
    // ------------------------------------------------------------------------------------------
    internal static class Hints
    {
        private static bool PS4 { get { return !string.Equals(GamepadPlugin.HintStyle.Value, "Xbox", StringComparison.OrdinalIgnoreCase); } }

        public static string BtnName(Btn b)
        {
            bool ps = PS4;
            switch (b)
            {
                case Btn.A: return ps ? "Cross" : "A";
                case Btn.B: return ps ? "Circle" : "B";
                case Btn.X: return ps ? "Square" : "X";
                case Btn.Y: return ps ? "Triangle" : "Y";
                case Btn.LB: return ps ? "L1" : "LB";
                case Btn.RB: return ps ? "R1" : "RB";
                case Btn.LT: return ps ? "L2" : "LT";
                case Btn.RT: return ps ? "R2" : "RT";
                case Btn.LS: return ps ? "L3" : "LS click";
                case Btn.RS: return ps ? "R3" : "RS click";
                case Btn.DUp: return "D-Pad Up";
                case Btn.DDown: return "D-Pad Down";
                case Btn.DLeft: return "D-Pad Left";
                case Btn.DRight: return "D-Pad Right";
                case Btn.Start: return ps ? "Options" : "Menu";
                case Btn.Back: return ps ? "Share" : "View";
            }
            return "?";
        }

        public static string Label(Bind b)
        {
            return (b.Layer ? BtnName(Btn.Back) + " + " : "") + BtnName(b.Btn);
        }

        // Для KeyBinds.GetBindName: возвращает название кнопки геймпада или null
        public static string ActionLabel(string action)
        {
            switch (action)
            {
                case "left":
                case "right":
                case "up":
                case "down":
                    return "Left Stick";
            }
            Bind b = Pad.FindBind(action);
            return b == null ? null : Label(b);
        }

        private static string Describe(string action)
        {
            switch (action)
            {
                case "attack": return "ЛКМ / действие";
                case "iteminteract": return "ПКМ / использовать";
                case "jump": return "прыжок";
                case "switchhands": return "сменить руку";
                case "woundview": return "осмотр ран";
                case "toggleinventory": return "инвентарь";
                case "expanddesc": return "описание (Shift)";
                case "altview": return "доп. вид (Ctrl)";
                case "throw": return "бросить";
                case "ragdoll": return "обмякнуть";
                case "craft": return "крафт";
                case "favourite": return "избранное";
                case "bark": return "крик";
                case "customizeitem": return "настроить предмет";
                case "pause": return "пауза";
                case "marksmanrack": return "передёрнуть затвор";
                case "marksmanunload": return "разрядить";
                case "console": return "консоль";
            }
            return action;
        }

        public static string MinigameHint()
        {
            bool glayer;
            Btn grab = Pad.ParseBtn(GamepadPlugin.GrabButton.Value, out glayer);
            string g = (glayer ? BtnName(Btn.Back) + " + " : "") + BtnName(grab);
            bool el;
            Btn ex = Pad.ParseBtn(GamepadPlugin.ExitButton.Value, out el);
            string e = ex == Btn.None ? BtnName(Btn.Start) : (el ? BtnName(Btn.Back) + " + " : "") + BtnName(ex);
            return "Держи " + g + " - схватить,  правый стик - тянуть / крутить,  " + e + " - выйти";
        }

        public static string Legend()
        {
            var sb = new StringBuilder();
            sb.AppendLine("CU Gamepad 0.2 - мод загружен.   F9 - скрыть");
            sb.AppendLine(Pad.Connected
                ? "Геймпад: найден (XInput #" + Pad.PadIndex + ")"
                : "Геймпад: НЕ НАЙДЕН (Steam Input / DS4Windows должен выдавать его как Xbox-контроллер)");
            sb.AppendLine("Левый стик - движение");
            sb.AppendLine("Правый стик - курсор");
            sb.AppendLine(BtnName(Btn.Back) + " (короткое нажатие) - скорость 1 > 2 > 3 > 1");
            sb.AppendLine("D-Pad - перейти на соседний слот");
            sb.AppendLine(Grab() + " в миниигре - схватить (бинт, шприц, осколки, вывих)");
            sb.AppendLine("B в миниигре - выйти (Esc); Start тоже работает");
            Bind[] bs = Pad.Binds;
            for (int i = 0; i < bs.Length; i++)
            {
                if (bs[i].Btn == Btn.None) continue;
                sb.AppendLine(Label(bs[i]) + " - " + Describe(bs[i].Action));
            }
            return sb.ToString().TrimEnd();
        }

        private static string Grab()
        {
            bool gl;
            Btn g = Pad.ParseBtn(GamepadPlugin.GrabButton.Value, out gl);
            return (gl ? BtnName(Btn.Back) + " + " : "") + BtnName(g);
        }
    }

    // ------------------------------------------------------------------------------------------
    // Патчи на классы игры (по именам, через рефлексию)
    // ------------------------------------------------------------------------------------------
    internal static class Hooks
    {
        public static void Apply(Harmony h)
        {
            Hook(h, "Limb", "ImpactDamage", "LimbImpact");
            Hook(h, "Limb", "BreakBone", "LimbBreak");
            Hook(h, "KeyBinds", "GetBindName", "BindName");
        }

        private static void Hook(Harmony h, string typeName, string method, string postfixName)
        {
            try
            {
                Type t = AccessTools.TypeByName(typeName);
                MethodInfo m = t != null ? AccessTools.Method(t, method) : null;
                if (m == null)
                {
                    GamepadPlugin.Log.LogWarning("Не нашёл " + typeName + "." + method + " — эта функция работать не будет.");
                    return;
                }
                MethodInfo post = typeof(Hooks).GetMethod(postfixName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                h.Patch(m, postfix: new HarmonyMethod(post));
                GamepadPlugin.Log.LogInfo("Патч " + typeName + "." + method + " применён.");
            }
            catch (Exception e)
            {
                GamepadPlugin.Log.LogWarning("Патч " + typeName + "." + method + " не удался: " + e.Message);
            }
        }

        // Limb.ImpactDamage(float amount, <something>)
        public static void LimbImpact(object __instance, float __0)
        {
            try
            {
                bool player = Rumble.IsPlayerLimb(__instance);
                if (GamepadPlugin.DebugValues.Value) GamepadPlugin.Log.LogInfo("[debug] Limb.ImpactDamage amount=" + __0 + " player=" + player);
                if (player) Rumble.Hit(__0);
            }
            catch (Exception) { }
        }

        public static void LimbBreak(object __instance)
        {
            try
            {
                if (Rumble.IsPlayerLimb(__instance)) Rumble.Big();
            }
            catch (Exception) { }
        }

        // KeyBinds.GetBindName(string action) — подставляем название кнопки геймпада
        public static void BindName(string __0, ref string __result)
        {
            try
            {
                if (!GamepadPlugin.Enabled.Value || !GamepadPlugin.ShowHints.Value) return;
                Pad.Tick();
                if (!Pad.Connected) return;
                string label = Hints.ActionLabel(__0);
                if (label != null) __result = label;
            }
            catch (Exception) { }
        }
    }

    // ------------------------------------------------------------------------------------------
    // Harmony-патчи: подмешиваем виртуальные нажатия в UnityEngine.Input
    // ------------------------------------------------------------------------------------------
    [HarmonyPatch(typeof(Input), "GetKey", typeof(KeyCode))]
    internal static class Patch_GetKey
    {
        private static void Postfix(KeyCode key, ref bool __result)
        {
            if (!__result && Pad.Held(key)) __result = true;
        }
    }

    [HarmonyPatch(typeof(Input), "GetKeyDown", typeof(KeyCode))]
    internal static class Patch_GetKeyDown
    {
        private static void Postfix(KeyCode key, ref bool __result)
        {
            if (!__result && Pad.Down(key)) __result = true;
        }
    }

    [HarmonyPatch(typeof(Input), "GetKeyUp", typeof(KeyCode))]
    internal static class Patch_GetKeyUp
    {
        private static void Postfix(KeyCode key, ref bool __result)
        {
            if (!__result && Pad.Up(key)) __result = true;
        }
    }

    [HarmonyPatch(typeof(Input), "GetMouseButton", typeof(int))]
    internal static class Patch_GetMouseButton
    {
        private static void Postfix(int button, ref bool __result)
        {
            if (!__result && button >= 0 && button < 3 && Pad.Held((KeyCode)((int)KeyCode.Mouse0 + button))) __result = true;
        }
    }

    [HarmonyPatch(typeof(Input), "GetMouseButtonDown", typeof(int))]
    internal static class Patch_GetMouseButtonDown
    {
        private static void Postfix(int button, ref bool __result)
        {
            if (!__result && button >= 0 && button < 3 && Pad.Down((KeyCode)((int)KeyCode.Mouse0 + button))) __result = true;
        }
    }

    [HarmonyPatch(typeof(Input), "GetMouseButtonUp", typeof(int))]
    internal static class Patch_GetMouseButtonUp
    {
        private static void Postfix(int button, ref bool __result)
        {
            if (!__result && button >= 0 && button < 3 && Pad.Up((KeyCode)((int)KeyCode.Mouse0 + button))) __result = true;
        }
    }
}
