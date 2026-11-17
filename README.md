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



CU Gamepad

🇷🇺 Русский

Поддержка геймпада для Casualties: Unknown. Плагин BepInEx 5 (Unity Mono) на Harmony. Неофициальный фанатский мод, не связан с разработчиками игры.

Возможности
Левый стик: движение. Правый стик: курсор в мире, инвентаре, меню и минииграх.
Кнопки вызывают родные действия игры через KeyBinds.GetBind, поэтому учитываются твои переназначенные клавиши.
Медицинские миниигры (бинт, шприц, осколки, вывих, переломы): зажми L2, чтобы схватить, и двигай правый стик, чтобы тянуть или крутить. Circle/B выходит (как Esc). (экспериментально)
Короткое нажатие Share/View переключает скорость игры 1 → 2 → 3 → 1. Share + кнопка даёт второй слой для редких действий.
D-Pad перепрыгивает курсором между слотами инвентаря.
Вибрация при ударах и переломах и пульсация в такт сердцу при кровотечении. (экспериментально)
Подсказки с кнопками геймпада; F9 показывает шпаргалку по раскладке.
Все кнопки и параметры меняются в BepInEx/config/com.local.casualtiesunknown.gamepad.cfg.
Требования
Windows, Casualties: Unknown (проверено на версии 1.0.2)
BepInEx 5.4.23.4 x64
Геймпад с поддержкой XInput. Контроллеры PS4/PS5 работают через Steam Input или DS4Windows.
Установка
Установи BepInEx 5 рядом с CasualtiesUnknown.exe и один раз запусти игру.
В BepInEx/config/BepInEx.cfg поставь HideManagerGameObject = true (иначе плагин может не загрузиться).
Положи CUGamepad.dll в BepInEx/plugins.
Для контроллеров PlayStation: в настройках Steam включи Steam Input для PlayStation, в свойствах игры → Контроллер выбери «Включить систему ввода Steam» и запускай игру через Steam.
Запусти игру и нажми F9. Должно быть написано «Геймпад: найден (XInput #0)».
Сборка из исходников

Нужен .NET SDK и установленный BepInEx в папке игры:

dotnet build -c Release -p:GamePath="D:\путь\к\Casualties Unknown"

Результат: bin/Release/netstandard2.1/CUGamepad.dll.

Раскладка по умолчанию
Управление	Действие
Левый стик	Движение
Правый стик	Курсор
R2 / L2	ЛКМ / ПКМ (в миниигре L2 = захват)
Cross	Прыжок
Circle	Смена руки (в миниигре: выход)
Square	Осмотр ран
Triangle	Инвентарь
L1 / R1	Shift / Ctrl
L3 / R3	Бросок / обмякнуть
Options	Пауза
Share (короткое нажатие)	Скорость игры 1 → 2 → 3
Share + D-Pad	Крафт / избранное / bark / кастомизация предмета
Share + Square / Triangle	Передёрнуть затвор / разрядить
D-Pad	Перейти на соседний слот инвентаря

Названия кнопок Xbox: Cross/Circle/Square/Triangle = A/B/X/Y, L1/R1 = LB/RB, L2/R2 = LT/RT, Share = View, Options = Menu.

Настройка

Файл конфига создаётся после первого запуска. Главные параметры:

Cursor/Speed, Cursor/Curve, Sticks/Deadzone — отклик курсора.
Minigames/GrabButton, Minigames/ExitButton, Minigames/CursorScale.
Buttons/* — любое действие можно переназначить (A, B, X, Y, LB, RB, LT, RT, LS, RS, DUp, DDown, DLeft, DRight, Start, Back, None; префикс Back+ для слоя Share).
Compat/NativeMouseClicks = true — если клики не срабатывают, отправлять настоящие события мыши.
Rumble/* — сила и пороги вибрации. Debug/LogValues = true пишет в лог значения боли, кровотечения и урона для подгонки.
Hints/Style — названия кнопок PS4 или Xbox.
Если что-то не работает
F9 ничего не показывает: плагин не загружен. Проверь BepInEx/LogOutput.log и убедись, что HideManagerGameObject = true.
«Геймпад: НЕ НАЙДЕН»: игра не видит XInput-контроллер. Для PlayStation запускай игру через Steam с включённым Steam Input либо используй DS4Windows.
Нет вибрации по Bluetooth: частое ограничение DualShock 4 в Steam Input, попробуй USB-кабель.
Известные ограничения
Проверено на версии игры 1.0.2.
Управление минииграми и вибрация экспериментальные, их может потребоваться подстроить.
Часть текстов туториала по-прежнему показывает клавиатурные подсказки.
Лицензия

См. файл LICENSE.
