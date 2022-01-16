using System.Collections;
using System.Linq;
using UnityEngine;
using TranCore;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using static Modding.Logger;
using System.Collections.Generic;
using ModCommon.Util;
using System;

namespace HKHeroControl
{
    public class HollowKnightCtrl : MonoBehaviour
    {
        public TranAttach TranAttach => gameObject.GetTranAttach();
        tk2dSpriteAnimator animator = null;
        Rigidbody2D rig = null;
        DefaultActions defaultActions = null;

        void Awake()
        {
            TranAttach.AutoDis = false;
            animator = gameObject.GetComponent<tk2dSpriteAnimator>();
            rig = gameObject.GetComponent<Rigidbody2D>();
            defaultActions = new DefaultActions(animator, rig);


            foreach (var v in GetComponents<PlayMakerFSM>()) Destroy(v);
            Destroy(GetComponent<EnemyDeathEffects>());


            TranAttach.RegisterAction("M1", defaultActions.MoveHeroTo);
            TranAttach.InvokeActionOn("M1", defaultActions.MoveHeroToTest);
            TranAttach.RegisterAction("M2", defaultActions.MoveTranToHero);
            TranAttach.InvokeActionOn("M2", defaultActions.MoveTranToHeroTest);
            //TranAttach.RegisterAction("M3", defaultActions.SetTranScale);
            //TranAttach.InvokeActionOn("M3", DefaultActions.AlwaysTrue);
            TranAttach.RegisterAction("TURN", defaultActions.Turn);
            TranAttach.InvokeActionOn("TURN", DefaultActions.TurnTest);

            TranAttach.RegisterAction("JUMP", ActionJump,
                TranAttach.InvokeWithout("JUMP")
                );
            TranAttach.InvokeActionOn("JUMP", DefaultActions.JumpTest);

            TranAttach.RegisterAction("FALL", ActionFall,
                TranAttach.InvokeWithout("FALL"),
                TranAttach.InvokeWithout("DASH")
                );
            TranAttach.InvokeActionOn("FALL", defaultActions.FallTest);

            TranAttach.RegisterAction("STOP", defaultActions.Stop,
                TranAttach.InvokeWithout("STOP"),
                TranAttach.InvokeWithout("RUN"),
                TranAttach.InvokeWithout("DASH")
                );
            TranAttach.InvokeActionOn("STOP", DefaultActions.AlwaysTrue);

            TranAttach.RegisterAction("RUN", ActionRun,
                TranAttach.InvokeWithout("DASH"),
                TranAttach.InvokeWithout("RUN"));
            TranAttach.InvokeActionOn("RUN", DefaultActions.RunTest);

            TranAttach.RegisterAction("IDLE", defaultActions.Idle,
                TranAttach.InvokeWithout("IDLE"),
                TranAttach.Or(
                    TranAttach.And(
                        () => TranAttach.InvokeCount == 2,
                        TranAttach.InvokeWith("STOP")
                        ),
                    () => TranAttach.InvokeCount == 1
                ));
            TranAttach.InvokeActionOn("IDLE", DefaultActions.AlwaysTrue);

            TranAttach.RegisterAction("ATTACK", ActionSlash,
                    TranAttach.InvokeWithout("ATTACK"));
            TranAttach.InvokeActionOn("ATTACK", DefaultActions.AttackTest);
        }

        void Update()
        {
            if(Input.GetKey(KeyCode.Alpha1))
            {
                Log($"TranAttach.InvokeCount: {TranAttach.InvokeCount}");
            }
        }

        private IEnumerator ActionRun()
        {
            animator.Play("Run");
            rig.SetVX(
                HeroController.instance.cState.facingRight ?
                HeroController.instance.RUN_SPEED_CH_COMBO :
                -HeroController.instance.RUN_SPEED_CH_COMBO
                );
            yield return null;
        }

        IEnumerator ActionSlash()
        {
            yield return animator.PlayAnimWait("Slash1 Antic");
            yield return animator.PlayAnimWait("Slash1");
            yield return animator.PlayAnimWait("Slash1 Recover");
            yield return animator.PlayAnimWait("Slash2");
            yield return animator.PlayAnimWait("Slash2 Recover");
            yield return animator.PlayAnimWait("Slash3");
            yield return animator.PlayAnimWait("Recover");
        }

        IEnumerator ActionJump()
        {
            yield return animator.PlayAnimWait("Antic");
            rig.SetVY(25);
            animator.Play("Jump");
            yield return new WaitForSeconds(0.25f);
            yield return animator.PlayAnimWait("Recover");
        }

        private IEnumerator ActionFall()
        {
            Log("Action Fall start");
            yield return defaultActions.Fall();
            Log("Action Fall End");
        }

        private bool HeroController_CanNailCharge(On.HeroController.orig_CanNailCharge orig, HeroController self)
            => false;

        private bool HeroController_CanQuickMap(On.HeroController.orig_CanQuickMap orig, HeroController self)
            => !HeroController.instance.cState.dead;

        private bool HeroController_CanFocus(On.HeroController.orig_CanFocus orig, HeroController self)
            => !HeroController.instance.cState.dead;

        private bool HeroController_CanTalk(On.HeroController.orig_CanTalk orig, HeroController self)
            => !HeroController.instance.cState.dead;

        private void _NoDamage(On.HeroController.orig_TakeDamage orig, HeroController self, GameObject go,
                        GlobalEnums.CollisionSide damageSide, int damageAmount, int hazardType)
        {
        }

        void OnEnable()
        {
            if (GetComponent<MeshRenderer>() != null)
            {
                GetComponent<MeshRenderer>().enabled = true;    //使实体可见
            }
            rig.isKinematic = false;
            rig.gravityScale = 1;
            foreach (var v in GetComponents<Collider2D>())
            {
                v.enabled = true;
                v.isTrigger = false;
            }
            On.HeroController.CanTalk += HeroController_CanTalk;
            On.HeroController.CanFocus += HeroController_CanFocus;
            On.HeroController.CanQuickMap += HeroController_CanQuickMap;
            On.HeroController.CanNailCharge += HeroController_CanNailCharge;
        }

        void OnDisable()
        {
            On.HeroController.CanTalk -= HeroController_CanTalk;
            On.HeroController.CanFocus -= HeroController_CanFocus;
            On.HeroController.CanQuickMap -= HeroController_CanQuickMap;
            On.HeroController.CanNailCharge -= HeroController_CanNailCharge;

            On.HeroController.TakeDamage -= _NoDamage;
        }

        private List<string> collisionActions = new List<string> { "Dash" };
    }
}
