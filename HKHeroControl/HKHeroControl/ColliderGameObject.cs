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

        public ColliderGameObject(PlayMakerFSM control, string objName, int damage=21)
        {
            gameObject = control.FsmVariables.FindFsmGameObject(objName).Value;
            if(gameObject == null)
                throw new System.Exception("ColliderGameObject: null reference for " + objName);
            gameObject.AddComponent<MPGetter>();
            gameObject.layer = (int)GlobalEnums.PhysLayers.HERO_ATTACK;
            gameObject.TranHeroAttack(AttackTypes.Nail, damage);
        }

        public void AttackAntic()
        {
            gameObject.tag = "Nail Attack";
            //gameObject.GetComponent<DE>().hit.DamageDealt = PlayerData.instance.nailDamage;
            gameObject.SetActive(true);
            //gameObject.GetComponent<tk2dSpriteAnimator>().Play("Overhead Slash");
        }

        public void CancelAttack()
        {
            gameObject.GetComponent<tk2dSpriteAnimator>().StopAndResetFrame();
            gameObject.SetActive(false);
        }
    }
}
