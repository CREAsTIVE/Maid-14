extraction-notify-popup = Клан на связи. Проверьте чат.
extraction-unlocked = Задания выполнены. Используйте «Выполнить запрос на досрочную эвакуацию», чтобы получить точку отхода, затем вызовите в ней десантную капсулу. Код капсулы: { $code }.
extraction-unlocked-forced = Клан вышел на связь досрочно. Используйте «Выполнить запрос на досрочную эвакуацию», чтобы получить точку отхода, затем вызовите в ней десантную капсулу. Код капсулы: { $code }.
extraction-zone-designated = Точка отхода назначена у голопада: { $location }. Встаньте в зону и вызовите десантную капсулу. Осталось переносов: { $left }.
extraction-no-requests-zone = Клан больше не перенесёт точку. Она всё ещё у голопада: { $location }.
extraction-no-requests = Клан больше не выходит на связь.
extraction-no-zone = Подходящая точка отхода на станции не найдена.
extraction-zone-lost = Точка отхода уничтожена. Клан назначил новую у голопада: { $location }.
extraction-zone-lost-none = Точка отхода уничтожена, а новую найти не удалось. Обратитесь к администрации.
extraction-zone-reset = Клан снова на связи: можно запросить новую точку отхода.
extraction-reminder = Клан ждёт вас. Используйте «Выполнить запрос на досрочную эвакуацию», чтобы получить точку отхода. Код капсулы: { $code }.
extraction-reminder-zone = Клан ждёт вас у голопада: { $location }. Код капсулы: { $code }.
extraction-not-in-zone = Нужно находиться в зоне отхода.
extraction-announcement-sender = Сенсоры станции
extraction-zone-marker-desc = Маркер отхода «{ $name }».
extraction-round-end = [color=gold]{ $name }[/color] эвакуирован через { $minutes } { $minutes ->
    [one] минуту
    [few] минуты
   *[other] минут
} после прибытия на станцию.

extraction-pod-announcement = Неизвестный малый объект движется на большой скорости в сторону станции, столкновение неизбежно.
extraction-pod-called = Капсула вызвана и приземлится через { $seconds } { $seconds ->
    [one] секунду
    [few] секунды
   *[other] секунд
} на клетку, где вы стоите. Всё на этой клетке будет уничтожено, отойдите в сторону!
extraction-pod-already-called = Капсула уже в пути.
extraction-pod-already-landed = Капсула уже на станции.
extraction-pod-landed = Капсула приземлилась. Откройте её кодом { $code }, заберитесь внутрь, закройте и нажмите «СТАРТ».
extraction-pod-lost = Капсула не смогла найти точку посадки. Вызвать её снова можно через { $seconds } { $seconds ->
    [one] секунду
    [few] секунды
   *[other] секунд
}.
extraction-pod-destroyed = Капсула уничтожена. Другой не будет.
extraction-pod-inside = Нажмите «СТАРТ», чтобы взлететь.
extraction-pod-not-inside = Нужно находиться внутри своей капсулы.
extraction-pod-launching = Капсула загерметизирована. Взлёт через { $seconds } { $seconds ->
    [one] секунду
    [few] секунды
   *[other] секунд
}.
extraction-pod-launch-cancelled = Вы покинули капсулу, взлёт отменён.
extraction-pod-sealed = Капсула загерметизирована.
extraction-pod-ui-title = Десантная капсула
extraction-pod-ui-locked = Введите код
extraction-pod-ui-wrong-code = Неверный код
extraction-pod-ui-unlocked = Капсула открыта
extraction-pod-ui-launching = Идёт взлёт

command-description-extraction-reset = Удаляет зону отхода у переданных антагонистов и позволяет запросить её заново.
command-description-extraction-unlock = Открывает отход переданным антагонистам независимо от их целей.

extraction-command-no-entity = Сущность не найдена.
extraction-command-no-component = У { $target } нет компонента AntagExtractable.
extraction-command-already-unlocked = { $target }: отход уже открыт.
extraction-command-unlocked = { $target }: отход открыт.
extraction-command-unlocked-added = { $target }: добавлен AntagExtractable (всё, что сейчас при себе, считается своим снаряжением), отход открыт.
extraction-command-reset = { $target }: зона отхода сброшена.
