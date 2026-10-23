# Prefab Brush

Editor tool for painting prefabs onto scene surfaces with the mouse. Surfaces do not need colliders: they are detected by how meshes are rendered in the Scene View.

Requires Unity 2022.3 or newer. Editor-only: nothing is added to builds.

## Quick start

1. Create a palette: `Assets > Create > Prefab Brush > Palette` (or press **New Palette** in the tool overlay).
2. Enable the tool:
   - the brush button in the Scene View **Tools** overlay;
   - menu `Tools > Prefab Brush`;
   - shortcut `Ctrl+Shift+B` (rebind in `Edit > Shortcuts`).
3. Pick the palette in the **Prefab Brush** overlay and add prefabs to `Entries`.
4. Click or drag in the Scene View.

| Input | Action |
|---|---|
| LMB / LMB drag | Place prefabs |
| Shift + LMB / drag | Erase prefabs inside the erase radius |
| Alt + LMB | Orbit camera as usual |
| Esc, or any other tool | Exit the tool |
| Ctrl+Z | Undo the whole stroke in one step |

The brush circle shows the next object's footprint: green = free, orange = occupied, red = erase radius.

## Palette settings

| Setting | Description |
|---|---|
| Entries | Prefabs to pick from randomly. |
| Weight (0..1) | Relative pick chance. 1 = most frequent, 0 = rarest but still possible (minimum effective weight is 0.01). Remove the entry to exclude it. |
| Use Tag Filter / Tag | Place only on objects with the selected tag (and their child renderers). |
| Random Y Rotation | Random rotation around the object's local Y axis. |
| Normal Alignment | 0 = always world up (e.g. trees on slopes), 1 = local Y follows the surface normal (walls, ceilings). Values in between blend. |
| Scale Min-Max | Random uniform scale multiplier on top of the prefab's own scale. |
| Spacing | Multiplier of the minimum distance between objects. 1 = sizes just touch, >1 = gaps, 0 = overlap allowed. |
| Erase Radius | World-unit radius for Shift erasing (objects whose pivot is inside are removed). |

## How it works

- **Surface detection:** the Scene View picking finds the visible object under the cursor; the exact hit point and normal come from a ray-vs-mesh test. Objects without a `MeshFilter` or `SkinnedMeshRenderer` (UI, Terrain, sprites) are skipped.
- **No overlaps:** the footprint radius is computed from renderer bounds and the random scale. Placement is checked against a spatial hash of all brush objects, including those from earlier sessions, so passing over the same spot never stacks objects.
- **Hierarchy:** objects become children of an empty `PrefabBrush_Placed` object created inside the surface object. World position, rotation and scale are preserved.
- **Performance:** no per-event allocations in the hot path; mouse-drag picking is throttled to a few pixels of movement.

## Limitations

- The overlap sphere is centered on the object's pivot and uses its horizontal (X/Z) size.
- Surfaces with non-uniform scale may skew rotated children (a Unity limitation).
- Objects moved or added manually inside `PrefabBrush_Placed` are noticed after the tool is re-enabled, an undo/redo, or a scene open.

## Export

`Assets > Export Package...` on the `Assets/PrefabBrush` folder.

---

# Prefab Brush (Русский)

Editor-инструмент для расстановки префабов на поверхностях сцены мышкой. Коллайдеры на поверхностях не нужны: поверхность определяется по тому, как меши отрисованы в Scene View.

Требуется Unity 2022.3 или новее. Только для редактора: в билд ничего не попадает.

## Быстрый старт

1. Создайте палитру: `Assets > Create > Prefab Brush > Palette` (или кнопка **New Palette** в оверлее инструмента).
2. Включите инструмент:
   - кнопка кисти в оверлее **Tools** в Scene View;
   - меню `Tools > Prefab Brush`;
   - шорткат `Ctrl+Shift+B` (переназначается в `Edit > Shortcuts`).
3. В оверлее **Prefab Brush** выберите палитру и добавьте префабы в `Entries`.
4. Кликайте или ведите мышкой с зажатой кнопкой в Scene View.

| Ввод | Действие |
|---|---|
| ЛКМ / ЛКМ с движением | Расставить префабы |
| Shift + ЛКМ / движение | Стереть префабы в радиусе стирания |
| Alt + ЛКМ | Вращение камеры как обычно |
| Esc или любой другой инструмент | Выйти из инструмента |
| Ctrl+Z | Откатить весь штрих одним шагом |

Круг кисти показывает след следующего объекта: зелёный — место свободно, оранжевый — занято, красный — радиус стирания.

## Настройки палитры

| Настройка | Описание |
|---|---|
| Entries | Префабы, из которых выбирается случайный. |
| Weight (0..1) | Относительная вероятность выбора. 1 — самый частый, 0 — самый редкий, но всё же возможный (минимальный эффективный вес 0.01). Чтобы исключить префаб, уберите его из списка. |
| Use Tag Filter / Tag | Ставить только на объекты с выбранным тегом (и их дочерние рендереры). |
| Random Y Rotation | Случайный поворот вокруг локальной оси Y объекта. |
| Normal Alignment | 0 — всегда вверх по миру (например, деревья на склоне), 1 — локальная ось Y по нормали поверхности (стены, потолки). Промежуточные значения смешивают. |
| Scale Min-Max | Случайный равномерный множитель масштаба поверх масштаба самого префаба. |
| Spacing | Множитель минимальной дистанции между объектами. 1 — размеры касаются, >1 — зазоры, 0 — наложение разрешено. |
| Erase Radius | Радиус стирания с Shift в мировых единицах (удаляются объекты, чей пивот внутри). |

## Как это работает

- **Детект поверхности:** пикинг Scene View находит видимый объект под курсором; точная точка и нормаль считаются по пересечению луча с мешем. Объекты без `MeshFilter` или `SkinnedMeshRenderer` (UI, Terrain, спрайты) пропускаются.
- **Без наложений:** радиус следа считается по bounds рендереров и случайному масштабу. Расстановка проверяется по spatial hash всех объектов кисти, включая созданные в прошлых сессиях, поэтому повторный проход по тому же месту ничего не наслаивает.
- **Иерархия:** объекты становятся детьми пустого объекта `PrefabBrush_Placed`, который создаётся внутри объекта-поверхности. Мировые позиция, поворот и масштаб сохраняются.
- **Производительность:** в горячем пути нет аллокаций на каждое событие; пикинг при движении мыши ограничен порогом в несколько пикселей.

## Ограничения

- Сфера наложения центрируется на пивоте объекта и использует его горизонтальный (X/Z) размер.
- На поверхностях с неравномерным масштабом повёрнутые дети могут получить skew (ограничение Unity).
- Объекты, перемещённые или добавленные вручную в `PrefabBrush_Placed`, учитываются после повторного включения инструмента, undo/redo или открытия сцены.

## Экспорт

`Assets > Export Package...` для папки `Assets/PrefabBrush`.
