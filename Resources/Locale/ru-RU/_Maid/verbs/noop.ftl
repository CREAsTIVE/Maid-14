interaction-LookAt-name = Смотреть
interaction-LookAt-description = Смотри в пустоту и смотри, как она смотрит в ответ..
interaction-LookAt-success-self-popup =
    Ты смотришь на { $selfTarget ->
    [true] себя
    *[false] {$target}
        }.
interaction-LookAt-success-target-popup = Ты чувствуешь, что  {$user} смотрит на вас...
interaction-LookAt-success-others-popup =
    {$user} смотрит на { $selfTarget ->
    [true] себя
    *[false] {$target}
        }.

interaction-Hug-name = Обнять
interaction-Hug-description = Одно объятие в день избавляет от психологических ужасов, недоступных вашему пониманию.
interaction-Hug-success-self-popup = Ты обнимаешь {THE($target)}.
interaction-Hug-success-target-popup = {THE($user)} обнимает тебя.
interaction-Hug-success-others-popup = {THE($user)} обнимает {THE($target)}.

interaction-KnockOn-name = Стучать
interaction-KnockOn-description = Постучите по цели, чтобы привлечь внимание.
interaction-KnockOn-success-self-popup = Ты стучишь по {THE($target)}.
interaction-KnockOn-success-target-popup = {THE($user)} стучит по вам.
interaction-KnockOn-success-others-popup = {THE($user)} стучит по {THE($target)}.

# The below includes conditionals for if the user is holding an item
interaction-WaveAt-name = Помахать
interaction-WaveAt-description = Помашите цели. Если у вас в руках какой-либо предмет, вы будете им махать.
interaction-WaveAt-success-self-popup = Ты машешь {$hasUsed ->
    [false] {THE($target)} рукой .
    *[true] {THE($target)} {$used} .
}
interaction-WaveAt-success-target-popup = {THE($user)} машет {$hasUsed ->
    [false] тебе рукой.
    *[true] тебе {$used} .
}
interaction-WaveAt-success-others-popup = {THE($user)} машет {$hasUsed ->
    [false] {THE($target)} рукой.
    *[true] {$used} {THE($target)}.
}
