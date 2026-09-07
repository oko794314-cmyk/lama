# 🗺️ Карта пустыни — DesertMap

Процедурная карта пустыни для Unity 6000 (URP). Внешних ассетов не нужно — всё строится из примитивов скриптом.

## Что внутри

- `Assets/Scenes/DesertMap.unity` — сцена карты (камера уже настроена на обзор)
- `Assets/Scripts/DesertMapGenerator.cs` — генератор карты
- `Docs/desert-map-design.html` — дизайн-документ / 2D-карта для референса

### Объекты на карте (генерируются)

- 🏜️ Земля 200×200 + 26 дюн (seed-рандом)
- 💧 Оазис: вода, трава, 7 пальм
- 🔺 Пирамида Сахар-Ра (-45, 42)
- 🏛️ Руины Зер-Тул (35, -25): колонны + упавшая колонна
- 🏕️ Лагерь Западный (-20, -30): 3 палатки + костер с PointLight
- 🌵 16 кактусов, 14 скал, кости левиафана
- 🐪 Дороги каравана (плоские полигоны)
- 🌫️ Туман + теплое солнце настроены автоматически
- 📍 PlayerSpawn (0, 2, -38)

## Как открыть

1. Склонируй репозиторий:
   ```
   git clone https://github.com/oko794314-cmyk/lama.git
   ```
2. Открой папку в Unity Hub → Unity **6000.0.66f1**
3. Открой сцену `Assets/Scenes/DesertMap.unity`
4. Нажми Play — карта сгенерируется автоматически (`autoGenerateOnStart`)

## Как перегенерировать

- Выбери объект `DesertMapGenerator` → в инспекторе меняй `Seed`, `Dune Count`, `Rock Count` и т.д.
- ПКМ по компоненту → `Generate Desert` / `Clear Desert`
- В Edit Mode работает тоже (ExecuteInEditMode)

## Структура

```
Assets/
  Scenes/
    SampleScene.unity
    DesertMap.unity        <- НОВОЕ
    DesertMap.unity.meta   <- НОВОЕ
  Scripts/
    DesertMapGenerator.cs  <- НОВОЕ
Docs/
  desert-map-design.html   <- 2D-референс карты
```

Сцена добавлена в `ProjectSettings/EditorBuildSettings.asset`, в билд включается автоматически.
