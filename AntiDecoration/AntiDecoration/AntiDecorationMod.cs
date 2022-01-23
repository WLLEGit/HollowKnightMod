using System.Collections;
using System.Linq;
using UnityEngine;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System.Collections.Generic;
using ModCommon.Util;
using System;
using ModCommon;
using Modding;
using GlobalEnums;

namespace AntiDecoration
{
    public class AntiDecorationMod : Mod, ITogglableMod
    {
        public override void Initialize()
        {
            base.Initialize();
            ModHooks.SlashHitHook += ModHooks_SlashHitHook;
            ModHooks.HeroUpdateHook += ModHooks_HeroUpdateHook;
        }

        readonly List<string> targets = new List<string> { "SPIKES", "WP_SAW", "THORN", "FLY" };

        private void RemoveAllNearby(GameObject gameObject, string target)
        {
           foreach(var obj in 
                (UnityEngine.Object.FindObjectsOfType(typeof(GameObject)) as GameObject[]).ToArray()
                .Where(t=>t.name.ToUpper().Contains(target) 
                        && (t.transform.position - gameObject.transform.position).magnitude<=25
                        && (t.transform.position - HeroController.instance.gameObject.transform.position).magnitude <= 25))
            {
                Log($"Remove: {obj.name}");
                GameObject.Destroy(obj);
            }
        }

        private void ModHooks_HeroUpdateHook()
        {
            //PlayerData.instance.hornet1Defeated = true;
            //PlayerData.instance.hasLantern = true;
            //PlayerData.instance.hasDash = true;
            //PlayerData.instance.hasSuperDash = true;
            //PlayerData.instance.hasShadowDash = true;
            //PlayerData.instance.hasSpell =  true;
            //PlayerData.instance.hasWalljump = true;
            //PlayerData.instance.hasQuill = true;
            //PlayerData.instance.hasDoubleJump = true;

            PlayerData.instance.hasDreamNail = true;
            PlayerData.instance.dreamNailConvo = true;
            PlayerData.instance.dreamNailUpgraded = true;
            PlayerData.instance.infiniteAirJump = true;
        }

        private void ModHooks_SlashHitHook(Collider2D otherCollider, GameObject slash)
        {
            string name = otherCollider.gameObject.name.ToUpper();
            Log("Hit: " + name);
            if (targets
                .Select((s) => name.Contains(s))
                .Any((b)=>b))
            {
                UnityEngine.Object.Destroy(otherCollider.gameObject);
                if (name.Contains("THORN"))
                {
                    RemoveAllNearby(otherCollider.gameObject, "THORN");
                    Log("Remove all");
                }
            }
        }

        public void Unload()
        {
            ModHooks.SlashHitHook -= ModHooks_SlashHitHook;
            ModHooks.HeroUpdateHook -= ModHooks_HeroUpdateHook;
        }
    }
}
