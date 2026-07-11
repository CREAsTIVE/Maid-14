inspect-part-status-line = {
  $isSelf ->
    [true] {
      $partType ->
        [Groin] Your
        [Tail] Your
       *[other] Your
    }
  *[false] { CAPITALIZE(POSS-ADJ($entity)) }
} { $part } { $status }.
inspect-part-status-title = You check yourself for injuries.
inspect-part-status-title-other = You check { $entity } for injuries.
inspect-part-status-fine = is fine
inspect-part-status-comma = ,{" "}
inspect-part-status-and = and{" "}

inspect-part-status-Head = head
inspect-part-status-Chest = chest
inspect-part-status-Groin = groin
inspect-part-status-Arm = arm
inspect-part-status-LeftArm = left arm
inspect-part-status-RightArm = right arm
inspect-part-status-Hand = hand
inspect-part-status-LeftHand = left hand
inspect-part-status-RightHand = right hand
inspect-part-status-Leg = leg
inspect-part-status-LeftLeg = left leg
inspect-part-status-RightLeg = right leg
inspect-part-status-Foot = foot
inspect-part-status-LeftFoot = left foot
inspect-part-status-RightFoot = right foot
inspect-part-status-Tail = tail
inspect-part-status-Other = body part
