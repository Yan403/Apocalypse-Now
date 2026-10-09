using UnityEngine;

public enum BodyPart
{
    Head,
    Torso,
    Feet
}

public class Hitbox : MonoBehaviour
{
    public BodyPart bodyPart;
    public IDamageable damageable;
    void Awake()
    {
        damageable = GetComponentInParent<IDamageable>();
    }
}