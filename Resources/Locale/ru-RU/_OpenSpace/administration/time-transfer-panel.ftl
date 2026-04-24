admin-player-actions-window-time-transfer = Перенос времени

cmd-timetransferpanel-desc = Открывает панель переноса времени.
cmd-timetransferpanel-help = Использование: timetransferpanel

time-transfer-panel-title = Перенос времени
time-transfer-panel-player = Игрок
time-transfer-panel-player-placeholder = Ник или ID игрока
time-transfer-panel-minutes = Минуты по умолчанию
time-transfer-panel-overall = Добавить общее время
time-transfer-panel-group = Группировать
time-transfer-panel-select-visible = Выбрать видимые
time-transfer-panel-clear = Очистить
time-transfer-panel-tab-roles = По ролям
time-transfer-panel-tab-departments = По отделам
time-transfer-panel-role-search = Поиск ролей
time-transfer-panel-department-search = Поиск отделов
time-transfer-panel-add = Добавить время
time-transfer-panel-close = Закрыть
time-transfer-panel-ungrouped = Без отдела
time-transfer-panel-role-entry = {$role} ({$id})
time-transfer-panel-role-tooltip = Таймер: {$tracker}; отдел: {$department}
time-transfer-panel-department-entry = {$department} ({$count ->
    [one] {$count} роль
    [few] {$count} роли
    [many] {$count} ролей
   *[other] {$count} роли
})
time-transfer-panel-overall-included = да
time-transfer-panel-overall-skipped = нет
time-transfer-panel-summary-roles = Выбрано {$count ->
    [one] {$count} роль
    [few] {$count} роли
    [many] {$count} ролей
   *[other] {$count} роли
}; всего {$minutes ->
    [one] {$minutes} минута
    [few] {$minutes} минуты
    [many] {$minutes} минут
   *[other] {$minutes} минуты
}; общее время: {$overall}
time-transfer-panel-summary-departments = Выбрано {$departments ->
    [one] {$departments} отдел
    [few] {$departments} отдела
    [many] {$departments} отделов
   *[other] {$departments} отдела
}; затронуто {$roles ->
    [one] {$roles} роль
    [few] {$roles} роли
    [many] {$roles} ролей
   *[other] {$roles} роли
}; всего {$minutes ->
    [one] {$minutes} минута
    [few] {$minutes} минуты
    [many] {$minutes} минут
   *[other] {$minutes} минуты
}; общее время: {$overall}
time-transfer-panel-status-ready = Выберите игрока, минуты и роли, отделы или общее время.
time-transfer-panel-status-applying = Применяю перенос времени...
time-transfer-panel-status-in-progress = Перенос времени уже выполняется...
time-transfer-panel-error-no-access = У вас нет прав на перенос времени.
time-transfer-panel-error-no-player = Сначала выберите или введите игрока.
time-transfer-panel-error-no-minutes = Минуты должны быть больше нуля.
time-transfer-panel-error-no-roles = Выберите хотя бы одну роль, отдел или общее время.
time-transfer-panel-error-invalid-payload = Запрос переноса времени слишком большой или поврежден.
time-transfer-panel-error-invalid-tracker = Неизвестный таймер времени: {$tracker}
time-transfer-panel-error-player-not-found = Игрок '{$player}' не найден.
time-transfer-panel-error-playtime-loading = Время игрока {$player} еще загружается. Попробуйте через пару секунд.
time-transfer-panel-error-unhandled = Не удалось перенести время. Проверьте лог сервера.
time-transfer-panel-success-add = Добавлено {$minutes ->
    [one] {$minutes} минута
    [few] {$minutes} минуты
    [many] {$minutes} минут
   *[other] {$minutes} минуты
} суммарно в {$count ->
    [one] {$count} таймер
    [few] {$count} таймера
    [many] {$count} таймеров
   *[other] {$count} таймера
} для {$player}.
time-transfer-panel-admin-announcement = {$admin} перенес время игроку {$player}: {$summary}
