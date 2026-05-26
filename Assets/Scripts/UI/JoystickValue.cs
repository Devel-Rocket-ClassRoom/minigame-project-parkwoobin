using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "joystick")]
public class JoystickValue : ScriptableObject
{
    public Vector2 joyTouch;
    public bool isAttacking;
    public bool isJumping;
    public bool isDash;
    public bool isTurning;
}
