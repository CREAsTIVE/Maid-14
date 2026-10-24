server-stalin-toggle = Включить/выключить СТАЛИНА
ui-lobby-stalin-link-button = Привязать Discord
cmd-stalinbunker-desc = Включает бункер Сталина: в раунд могут зайти только игроки с привязанным Discord.
cmd-stalinbunker-help = Использование: stalinbunker [true/false]
stalin-link-window-title = Привязка Discord
stalin-link-window-hint = Скопируйте содержимое поля или нажмите на кнопку «Открыть в браузере»
stalin-link-window-open = Открыть в браузере
stalin-discord-doesnt-link = К вашему аккаунту не привязан Discord, нажмите на кнопку «Привязать Discord» в лобби
stalin-discord-age-check-fail = Возраст вашего Discord-аккаунта слишком маленький, пожалуйста, подождите { $needed } мин.
stalin-request-failed = Невозможно проверить привязку аккаунта! Код ошибки: { $error }
stalin-timeout = Таймаут, следующая попытка будет доступна через { $timeoutTime } с.
stalin-panic-bunker =
    { $enabled ->
        [True] Внимание, был включен бункер дедушки Сталина, только пользователи с подтверждённым Discord-аккаунтом могут зайти в раунд. Чтобы подтвердить свой аккаунт, нажмите на кнопку «Привязать Discord» в лобби.
        [False] Внимание, был выключен бункер дедушки Сталина.
       *[other] Бункер дедушки Сталина переключён.
    }
