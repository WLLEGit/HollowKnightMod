using System.Collections;
using System.Linq;
using UnityEngine;
using TranCore;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using static Modding.Logger;
using System.Collections.Generic;
using ModCommon.Util;

namespace HKHeroControl
{
    internal class ColliderGameObject
    {
        readonly GameObject gameObject = null;
        DecorationEffects effects = null;

        public ColliderGameObject(PlayMakerFSM control, string objName, int damage = 21, bool normalDE = false)
            : this(control.FsmVariables.FindFsmGameObject(objName).Value, damage, normalDE) { }

        public ColliderGameObject(GameObject gameObject, int damage = 21, bool normalDE = false)
        {
            this.gameObject = gameObject;
            if (gameObject == null)
                throw new System.Exception("ColliderGameObject: null reference");
            //gameObject.AddComponent<MPGetter>();
            gameObject.layer = (int)GlobalEnums.PhysLayers.HERO_ATTACK;
            gameObject.tag = "Nail Attack";
            if (normalDE)
                gameObject.TranNormalDE(AttackTypes.Nail, damage);
            else
                gameObject.TranHeroAttack(AttackTypes.Nail, damage);
        }

        public void SetDecorations(GameObject[] gos)
        {
            effects = new DecorationEffects(gos);
        }
        public void AttackAntic()
        {
            effects?.SetActive();
            gameObject.SetActive(true);
        }

        public void CancelAttack()
        {
            effects?.DeActivate();
            gameObject.SetActive(false);
        }

        public int SetAttack(int amount)
        {
            DamageEnemies damageEnemies = gameObject.AddComponent<DamageEnemies>();
            int tmp = damageEnemies.damageDealt;
            damageEnemies.damageDealt = amount;
            return tmp;
        }

    }

    internal class DecorationEffects
    {
        GameObject[] gos;
        public DecorationEffects(GameObject[] gos)
        {
            this.gos = gos;
        }

        public void SetActive()
        {
            foreach (GameObject g in this.gos)
                g.SetActive(true);
        }

        public void DeActivate()
        {
            foreach (GameObject g in this.gos)
                g.SetActive(false);
        }
    }
}
