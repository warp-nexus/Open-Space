markings-used = Используемые черты
markings-unused = Неиспользуемые черты
markings-add = Добавить черту
markings-remove = Убрать черту
markings-rank-up = Вверх
markings-rank-down = Вниз
markings-search = Поиск
marking-points-remaining = Черт осталось: { $points }
marking-used = { $marking-name }
marking-used-forced = { $marking-name } (Принудительно)
marking-slot-add = Добавить
marking-slot-remove = Удалить
marking-slot = Слот { $number }

humanoid-marking-modifier-force = Принудительно
humanoid-marking-modifier-ignore-species = Игнорировать вид
humanoid-marking-modifier-base-layers = Базовый слой
humanoid-marking-modifier-enable = Включить
humanoid-marking-modifier-prototype-id = ID прототипа:

# Categories

markings-category-Special = Специальное
markings-category-Hair = Причёска
markings-category-FacialHair = Лицевая растительность
markings-category-Head = Голова
markings-category-HeadTop = Голова (верх)
markings-category-HeadSide = Голова (бок)
markings-category-Snout = Морда
markings-category-SnoutCover = Морда (Внешний)
markings-category-UndergarmentTop = Нижнее бельё (Верх)
markings-category-UndergarmentBottom = Нижнее бельё (Низ)
markings-category-Chest = Грудь
markings-category-Arms = Руки
markings-category-Legs = Ноги
markings-category-Tail = Хвост
markings-category-Overlay = Наложение




# OpenSpace-Edit Start

-markings-selection = { $selectable ->
    [0] У вас не осталось доступных отметок.
    [one] Вы можете выбрать ещё одну отметку.
    [few] Вы можете выбрать ещё { $selectable } отметки.
   *[other] Вы можете выбрать ещё { $selectable } отметок.
}
markings-limits = { $required ->
    [true] { $count ->
        [-1] Выберите хотя бы одну отметку.
        [0] Вы не можете выбирать отметки, но каким-то образом должны? Это ошибка.
        [one] Выберите одну отметку.
       *[other] Выберите от {1} до {$count} отметок. { -markings-selection(selectable: $selectable) }
    }
   *[false] { $count ->
        [-1] Выберите любое количество отметок.
        [0] Вы не можете выбирать отметки.
        [one] Выберите не более одной отметки.
       *[other] Выберите до {$count} отметок. { -markings-selection(selectable: $selectable) }
    }
}
markings-reorder = Изменить порядок

humanoid-marking-modifier-respect-limits = Соблюдать лимиты
humanoid-marking-modifier-respect-group-sex = Учитывать ограничения пола и групп

markings-organ-Torso = Торс
markings-organ-Head = Голова
markings-organ-ArmLeft = Левая рука
markings-organ-ArmRight = Правая рука
markings-organ-HandRight = Правая кисть
markings-organ-HandLeft = Левая кисть
markings-organ-LegLeft = Левая нога
markings-organ-LegRight = Правая нога
markings-organ-FootLeft = Левая стопа
markings-organ-FootRight = Правая стопа
markings-organ-Eyes = Глаза

markings-layer-Special = Специальное
markings-layer-Tail = Хвост
markings-layer-Tail-Moth = Крылья
markings-layer-Hair = Причёска
markings-layer-FacialHair = Растительность на лице
markings-layer-UndergarmentTop = Майка
markings-layer-UndergarmentBottom = Трусы
markings-layer-Chest = Грудь
markings-layer-Head = Голова
markings-layer-Snout = Морда
markings-layer-SnoutCover = Морда (Покрытие)
markings-layer-HeadSide = Голова (Сбоку)
markings-layer-HeadTop = Голова (Сверху)
markings-layer-Eyes = Глаза
markings-layer-RArm = Правая рука
markings-layer-LArm = Левая рука
markings-layer-RHand = Правая кисть
markings-layer-LHand = Левая кисть
markings-layer-RLeg = Правая нога
markings-layer-LLeg = Левая нога
markings-layer-RFoot = Правая стопа
markings-layer-LFoot = Левая стопа
markings-layer-Overlay = Оверлей
markings-layer-TailOverlay = Оверлей хвоста

# OpenSpace-Edit End
