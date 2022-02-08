using System.Collections.Generic;
using UnityEngine;
using static Modding.Logger;



namespace TranCore
{
    public class DENoFrameDamage : MonoBehaviour
    {
        public AttackTypes attackType = AttackTypes.Generic;

        public bool circleDirection;

        public int damageDealt;

        public float direction;

        public bool ignoreInvuln = true;

        public float magnitudeMult;

        public bool moveDirection;

        public SpecialTypes specialType;

        private HashSet<GameObject> dmgTargets = new HashSet<GameObject>();

        private void Reset()
        {
            PlayMakerFSM[] components = GetComponents<PlayMakerFSM>();
            foreach (PlayMakerFSM playMakerFSM in components)
            {
                if (playMakerFSM.FsmName == "damages_enemy")
                {
                    attackType = (AttackTypes)playMakerFSM.FsmVariables.GetFsmInt("attackType").Value;
                    circleDirection = playMakerFSM.FsmVariables.GetFsmBool("circleDirection").Value;
                    damageDealt = playMakerFSM.FsmVariables.GetFsmInt("damageDealt").Value;
                    direction = playMakerFSM.FsmVariables.GetFsmFloat("direction").Value;
                    ignoreInvuln = playMakerFSM.FsmVariables.GetFsmBool("Ignore Invuln").Value;
                    magnitudeMult = playMakerFSM.FsmVariables.GetFsmFloat("magnitudeMult").Value;
                    moveDirection = playMakerFSM.FsmVariables.GetFsmBool("moveDirection").Value;
                    specialType = (SpecialTypes)playMakerFSM.FsmVariables.GetFsmInt("Special Type").Value;
                    break;
                }
            }
            dmgTargets.Clear();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            DoDamage(collision.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (base.enabled)
            {
                int layer = collision.gameObject.layer;
                if (layer != 20 && layer != 9 && layer != 26 && layer != 31 && !collision.CompareTag("Geo"))
                {
                    DoDamage(collision.gameObject);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
        }

        private void OnDisable()
        {
            dmgTargets.Clear();
        }

        private void OnEnable()
        {
            dmgTargets.Clear();
        }

        private void FixedUpdate()
        {
        }

        private void DoDamage(GameObject target)
        {
            if (damageDealt > 0 && !dmgTargets.Contains(target))
            {
                FSMUtility.SendEventToGameObject(target, "TAKE DAMAGE");
                dmgTargets.Add(target);
                HitTaker.Hit(target, new HitInstance
                {
                    Source = base.gameObject,
                    AttackType = attackType,
                    CircleDirection = circleDirection,
                    DamageDealt = damageDealt,
                    Direction = direction,
                    IgnoreInvulnerable = ignoreInvuln,
                    MagnitudeMultiplier = magnitudeMult,
                    MoveAngle = 0f,
                    MoveDirection = moveDirection,
                    Multiplier = 1f,
                    SpecialType = specialType,
                    IsExtraDamage = false
                });
            }
        }
    }
}
