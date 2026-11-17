# CU Gamepad

🇬🇧 English

Gamepad support for **Casualties: Unknown**. A BepInEx 5 (Unity Mono) plugin using Harmony.
Unofficial fan-made mod, not affiliated with the developers of the game.

## Features
- Left stick moves, right stick controls the cursor in the world, inventory, menus and minigames.
- Buttons trigger the game's own actions through `KeyBinds.GetBind`, so your rebound keys are respected.
- Medical minigames (bandage, syringe, shrapnel, dislocation, fractures): hold **L2** to grab and move the right stick to pull or rotate. **Circle/B** exits (same as Esc). *(experimental)*
- **Share/View** short press cycles game speed 1 → 2 → 3 → 1. Share + button is a second layer for less common actions.
- D-Pad jumps the cursor between inventory slots.
- Rumble on hits and fractures, and a heartbeat pulse while bleeding. *(experimental)*
- Gamepad button hints; press **F9** for a layout cheat sheet.
- Every button and setting can be changed in `BepInEx/config/com.local.casualtiesunknown.gamepad.cfg`.

## Requirements
- Windows, Casualties: Unknown (tested on v1.0.2)
- [BepInEx 5.4.23.4 x64](https://github.com/BepInEx/BepInEx/releases)
- An XInput controller. PS4/PS5 controllers work through Steam Input or DS4Windows.

## Installation
1. Install BepInEx 5 next to `CasualtiesUnknown.exe` and run the game once.
2. In `BepInEx/config/BepInEx.cfg` set `HideManagerGameObject = true` (otherwise the plugin may not load).
3. Put `CUGamepad.dll` into `BepInEx/plugins`.
4. PlayStation controllers: enable Steam Input for PlayStation controllers in Steam settings, set "Enable Steam Input" in the game's properties → Controller, and launch the game through Steam.
5. Start the game and press **F9**. You should see "Gamepad: found (XInput #0)".

## Building from source
You need the .NET SDK and BepInEx installed in the game folder:

```
dotnet build -c Release -p:GamePath="D:\path\to\Casualties Unknown"
```

Output: `bin/Release/netstandard2.1/CUGamepad.dll`.

## Default layout
| Input | Action |
|---|---|
| Left stick | Move |
| Right stick | Cursor |
| R2 / L2 | Left click / right click (in a minigame L2 = grab) |
| Cross | Jump |
| Circle | Switch hands (in a minigame: exit) |
| Square | Wound view |
| Triangle | Inventory |
| L1 / R1 | Shift / Ctrl |
| L3 / R3 | Throw / ragdoll |
| Options | Pause |
| Share (short press) | Game speed 1 → 2 → 3 |
| Share + D-Pad | Craft / favourite / bark / customize item |
| Share + Square / Triangle | Rack / unload (marksman) |
| D-Pad | Jump to a neighbouring inventory slot |

Xbox naming: Cross/Circle/Square/Triangle = A/B/X/Y, L1/R1 = LB/RB, L2/R2 = LT/RT, Share = View, Options = Menu.

## Configuration
The config file is created after the first launch. Notable options:
- `Cursor/Speed`, `Cursor/Curve`, `Sticks/Deadzone` — cursor feel.
- `Minigames/GrabButton`, `Minigames/ExitButton`, `Minigames/CursorScale`.
- `Buttons/*` — every action can be rebound (`A`, `B`, `X`, `Y`, `LB`, `RB`, `LT`, `RT`, `LS`, `RS`, `DUp`, `DDown`, `DLeft`, `DRight`, `Start`, `Back`, `None`; prefix `Back+` for the Share layer).
- `Compat/NativeMouseClicks = true` — if clicks don't register, send real mouse events instead.
- `Rumble/*` — strength and thresholds. `Debug/LogValues = true` prints pain, bleeding and damage values to the log for tuning.
- `Hints/Style` — `PS4` or `Xbox` button names.

## Troubleshooting
- **F9 shows nothing:** the plugin isn't loaded. Check `BepInEx/LogOutput.log` and make sure `HideManagerGameObject = true`.
- **"Gamepad: NOT FOUND":** the game isn't seeing an XInput controller. For PlayStation controllers launch the game through Steam with Steam Input enabled, or use DS4Windows.
- **No rumble over Bluetooth:** a common limitation of DualShock 4 with Steam Input; try a USB cable.

## Known limitations
- Tested on game version 1.0.2.
- Minigame controls and rumble are experimental and may need tuning.
- Some tutorial text still shows keyboard prompts.

## License
See `LICENSE`.



# CU Gamepad

Мод для **Casualties: Unknown**: управление геймпадом (XInput; PS4/PS5 — через Steam Input или DS4Windows).
Плагин BepInEx 5 (Unity Mono) + Harmony. Неофициальный, не связан с разработчиками игры.

## Возможности
- Левый стик — движение, правый стик — курсор.
- Кнопки геймпада вызывают игровые действия через `KeyBinds.GetBind`, поэтому учитываются твои переназначенные клавиши.
- Миниигры (бинт, шприц, осколки, вывих, переломы): зажми **L2** и двигай правый стик. **Circle/B** — выход (как Esc).
- **Share/View** (короткое нажатие): скорость 1 → 2 → 3 → 1. Share + кнопка — второй слой действий.
- D-Pad: прыжок курсора по слотам инвентаря.
- Вибрация при ударах, переломах и кровотечении.
- Подсказки с кнопками геймпада; **F9** — шпаргалка по раскладке.
- Все привязки и параметры — в `BepInEx/config/com.local.casualtiesunknown.gamepad.cfg`.

## Установка
1. Установи [BepInEx 5.4.23.4 x64](https://github.com/BepInEx/BepInEx/releases) в папку с игрой (рядом с `CasualtiesUnknown.exe`) и один раз запусти игру.
2. В `BepInEx/config/BepInEx.cfg` поставь `HideManagerGameObject = true` (иначе плагин может не загрузиться).
3. Положи `CUGamepad.dll` в `BepInEx/plugins`.
4. Для PS4/PS5: в Steam включи поддержку Steam Input для контроллеров PlayStation и запускай игру через Steam
   (в свойствах игры → Контроллер → «Включить систему ввода Steam»). Либо используй DS4Windows.

## Сборка из исходников
Нужен .NET SDK и установленный BepInEx в папке игры:

```
dotnet build -c Release -p:GamePath="D:\путь\к\Casualties Unknown"
```

Результат: `bin/Release/netstandard2.1/CUGamepad.dll`.

## Раскладка по умолчанию
| Управление | Действие |
|---|---|
| Левый стик | движение |
| Правый стик | курсор |
| R2 / L2 | ЛКМ / ПКМ (в миниигре L2 — захват) |
| Cross | прыжок |
| Circle | смена руки (в миниигре — выход) |
| Square | осмотр ран |
| Triangle | инвентарь |
| L1 / R1 | Shift / Ctrl |
| L3 / R3 | бросок / обмякнуть |
| Options | пауза |
| Share + D-Pad | крафт / избранное / bark / кастомизация |
| Share + Square / Triangle | передёрнуть затвор / разрядить |

## Известные ограничения
- Тестировался на версии игры 1.0.2.
- Вибрация зависит от того, отдаёт ли Steam/Windows вибрацию вашему геймпаду (по Bluetooth у DualShock часто не работает).
- Часть текстов туториала по-прежнему показывает клавиатурные подсказки.

## Лицензия
см. `LICENSE`.
