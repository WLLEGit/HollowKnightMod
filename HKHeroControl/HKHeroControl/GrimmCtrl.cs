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
using ModCommon;
using static UnityEngine.Random;
using Modding;

namespace HKHeroControl
{
    public class GrimmCtrl : MonoBehaviour
    {
        public TranAttach TranAttach => gameObject.GetTranAttach();
        tk2dSpriteAnimator animator = null;
        Rigidbody2D rig = null;
        DefaultActions defaultActions = null;

        GameObject smallShotBullet = null;
        GameObject hkCollider = null;
        ColliderGameObject slashCollider = null;

        GameObject plume = null;

        void Awake()
        {
            TranAttach.AutoDis = false;
            animator = gameObject.GetComponent<tk2dSpriteAnimator>();
            rig = gameObject.GetComponent<Rigidbody2D>();
            defaultActions = new DefaultActions(animator, rig);

            smallShotBullet = gameObject.GetFSMActionsOnState<FlingObjectsFromGlobalPoolTime>("SmallShot HighLow")[0].gameObject.Value;

            plume = gameObject.GetFSMActionsOnState<SpawnObjectFromGlobalPool>("Plume Gen")[0].gameObject.Value;


            //借用Stun碰撞箱作为Hollow Knight的整体碰撞箱
            hkCollider = gameObject.FindGameObjectInChildren("Counter");
            Destroy(hkCollider.GetComponent<DamageHero>());
            hkCollider.SetActive(true);

            slashCollider = new ColliderGameObject(gameObject.FindGameObjectInChildren("Slash"));   //攻击时的碰撞箱

            foreach (var v in GetComponents<PlayMakerFSM>()) Destroy(v);
            Destroy(GetComponent<EnemyDeathEffects>());


            TranAttach.RegisterAction("M1", ActionMoveHeroTo);
            TranAttach.InvokeActionOn("M1", defaultActions.MoveHeroToTest);
            TranAttach.RegisterAction("M2", ActionMoveTranToHero);
            TranAttach.InvokeActionOn("M2", defaultActions.MoveTranToHeroTest);
            TranAttach.RegisterAction("TURN", ActionTurn);
            TranAttach.InvokeActionOn("TURN", DefaultActions.TurnTest);

            TranAttach.RegisterAction("JUMP", ActionJump,
                TranAttach.InvokeWithout("JUMP")
                );
            TranAttach.InvokeActionOn("JUMP", DefaultActions.JumpTest);

            TranAttach.RegisterAction("FALL", ActionFall,
                TranAttach.InvokeWithout("FALL"),
                TranAttach.InvokeWithout("QUAKE"),
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
                TranAttach.InvokeWithout("FIREBALL"),
                TranAttach.InvokeWithout("QUAKE"),
                TranAttach.InvokeWithout("ATTACK"),
                TranAttach.InvokeWithout("RUN"));
            TranAttach.InvokeActionOn("RUN", DefaultActions.RunTest);

            TranAttach.RegisterAction("IDLE", ActionIdle,
                TranAttach.InvokeWithout("IDLE"),
                TranAttach.Or(
                    TranAttach.And(
                        () => TranAttach.InvokeCount == 3,
                        TranAttach.InvokeWith("STOP")
                        ),
                    () => TranAttach.InvokeCount == 2
                ));
            TranAttach.InvokeActionOn("IDLE", DefaultActions.AlwaysTrue);

            //普通攻击
            TranAttach.RegisterAction("C", ActionCounter,
                TranAttach.InvokeWithout("JUMP"),
                TranAttach.InvokeWithout("FALL"),
                TranAttach.InvokeWithout("DASH"),
                //TranAttach.InvokeWithout("RUN"),
                TranAttach.InvokeWithout("C"),
                TranAttach.InvokeWithout("ATTACK")
                );
            TranAttach.InvokeActionOn("C", DefaultActions.AttackTest);
            TranAttach.RegisterAction("ATTACK", ActionAttack,
                TranAttach.InvokeWithout("JUMP"),
                TranAttach.InvokeWithout("FALL"),
                TranAttach.InvokeWithout("DASH"),
                TranAttach.Or(
                    TranAttach.InvokeWith("RUN"),
                    TranAttach.InvokeWith("C")
                ),
                TranAttach.InvokeWithout("ATTACK"));

            //冲刺攻击
            TranAttach.RegisterAction("DASH", ActionSlash,
                TranAttach.InvokeWithout("DASH")
                );
            TranAttach.InvokeActionOn("DASH", DefaultActions.DashTest);

            //法球攻击
            TranAttach.RegisterAction("FIREBALL", ActionSmallShoot,
                TranAttach.InvokeWithout("FIREBALL"),
                TranAttach.InvokeWithout("ROAR"),
                TranAttach.InvokeWithout("QUAKE"),
                TranAttach.InvokeWithout("DASH")
                //TranAttach.InvokeWithout("JUMP"),
                //TranAttach.InvokeWithout("FALL"),
                //TranAttach.InvokeWithout("RUN")
                );
            TranAttach.InvokeActionOn("FIREBALL", TranAttach.And(
                DefaultActions.CastDownTest,
                TranAttach.Not(
                    TranAttach.Or(
                        DefaultActions.DownTest,
                        DefaultActions.UpTest
                        )
                    )
                ));

            ////上吼攻击
            //TranAttach.RegisterAction("ROAR", ActionChestShoot,
            //    TranAttach.InvokeWithout("FIREBALL"),
            //    TranAttach.InvokeWithout("ROAR"),
            //    TranAttach.InvokeWithout("QUAKE"),
            //    TranAttach.InvokeWithout("DASH")
            //    //TranAttach.InvokeWithout("JUMP"),
            //    //TranAttach.InvokeWithout("FALL"),
            //    //TranAttach.InvokeWithout("RUN")
            //    );
            //TranAttach.InvokeActionOn("ROAR", TranAttach.And(
            //    DefaultActions.CastDownTest,
            //    DefaultActions.UpTest,
            //    TranAttach.Not(
            //        TranAttach.Or(
            //            DefaultActions.DownTest
            //            )
            //        )
            //    ));

            //下砸攻击
            TranAttach.RegisterAction("QUAKE", ActionGenPlume,
                TranAttach.InvokeWithout("FIREBALL"),
                TranAttach.InvokeWithout("ROAR"),
                TranAttach.InvokeWithout("QUAKE"),
                TranAttach.InvokeWithout("DASH")
                //TranAttach.InvokeWithout("JUMP"),
                //TranAttach.InvokeWithout("FALL"),
                //TranAttach.InvokeWithout("RUN")
                );
            TranAttach.InvokeActionOn("QUAKE", TranAttach.And(
                DefaultActions.CastDownTest,
                DefaultActions.DownTest,
                TranAttach.Not(
                    TranAttach.Or(
                        DefaultActions.UpTest
                        )
                    )
                ));

        }

        bool faceRight = false;
        private IEnumerator ActionTurn()
        {
            faceRight = DefaultActions.RightTest();
            if (!faceRight)
                HeroController.instance.FaceRight();
            else
                HeroController.instance.FaceLeft();
            yield break;
        }

        private IEnumerator ActionMoveTranToHero()
        {
            rig.transform.localScale = HeroController.instance.transform.localScale;
            rig.transform.position = HeroController.instance.transform.position;
            yield break;
        }

        private IEnumerator ActionMoveHeroTo()
        {
            rig.transform.localScale = HeroController.instance.transform.localScale;
            HeroController.instance.transform.position = rig.transform.position;
            yield break;
        }

        private IEnumerator ActionIdle()
        {
            animator.Play("Idle");
            yield return new WaitForSeconds(0.25f);
        }

        void Update()
        {
            if (Input.GetKey(KeyCode.Alpha1))
            {
                Log($"Actions cnt {TranAttach.InvokeCount}: {string.Join(" ", TranAttach.curActs)}");
                Log($"is falling: {defaultActions.FallTest()}");
            }
        }

        private IEnumerator ActionRun()
        {
            if (TranAttach.IsActionInvoking("FALL") || TranAttach.IsActionInvoking("JUMP")
                || TranAttach.IsActionInvoking("FIREBALL") || TranAttach.IsActionInvoking("C"))
            {
                rig.SetVX(
                    faceRight ?
                    HeroController.instance.RUN_SPEED_CH_COMBO :
                    -HeroController.instance.RUN_SPEED_CH_COMBO
                    );
                yield return null;
            }
            else
            {
                animator.Play("Walk");
                rig.SetVX(0.5f * (
                    faceRight ?
                    HeroController.instance.RUN_SPEED_CH_COMBO :
                    -HeroController.instance.RUN_SPEED_CH_COMBO
                    ));
                yield return null;
            }
        }

        IEnumerator ActionSlash()
        {
            rig.gravityScale = 0;
            rig.velocity = new Vector2(0, 0);
            HeroController.instance.SetDamageMode(1);

            yield return animator.PlayAnimWait("Slash1 Antic");
            slashCollider.AttackAntic();
            rig.SetVX(HeroController.instance.DASH_SPEED * (faceRight ? 1 : -1));
            yield return animator.PlayAnimWait("Slash1");
            yield return animator.PlayAnimWait("Slash1 Recover");
            yield return animator.PlayAnimWait("Slash2");
            yield return animator.PlayAnimWait("Slash2 Recover");
            yield return animator.PlayAnimWait("Slash3");

            rig.SetVX(0);
            HeroController.instance.SetDamageMode(0);
            rig.gravityScale = 1;
            yield return animator.PlayAnimWait("Recover");
            slashCollider.CancelAttack();
        }

        IEnumerator ActionSmallShoot()
        {
            HeroController.instance.AddHealth(1);
            const float duration = 1.5f;
            const int repeat = 7;
            const float speed = 25f;
            //HeroController.instance.TakeMP(24);
            HeroController.instance.SetDamageMode(2);
            yield return animator.PlayAnimWait("SmallShot Antic");
            animator.Play("SmallShot");


            for (int i = 0; i < repeat; ++i)
            {
                float angle = (float)Range(10, 50) / 180f * (float)Math.PI;
                float vx = (float)(speed * Math.Cos(angle)) * (faceRight ? 1 : -1);
                float vy = (float)(speed * Math.Sin(angle));
                smallShotBullet.Clone().SetPos(transform.position + new Vector3(2f * (faceRight ? 1 : -1), 0, 0)).TranHeroAttack(AttackTypes.Spell, 40 / repeat)
                    .GetComponent<Rigidbody2D>().velocity = new Vector2(vx, vy);
                yield return new WaitForSeconds(duration / repeat);
            }

            HeroController.instance.SetDamageMode(0);
            HeroController.instance.QuakeInvuln();
            yield return animator.PlayAnimWait("SmallShot Recover");
        }

        IEnumerator ActionChestShoot()
        {
            throw new NotImplementedException();
        }

        IEnumerator ActionGenPlume()
        {
            rig.SetVY(50);
            animator.Play("Jump");
            yield return new WaitForSeconds(0.3f);
            rig.SetVY(-25);
            animator.Play("Dstab Stomp");

            int[] off = new int[] { -10, -5, 0, 5, 10 };
            GameObject[] gos = new GameObject[off.Length];
            PlayMakerFSM[] fsms = new PlayMakerFSM[off.Length];
            for (int i = 0; i < off.Length; i++)
            {
                gos[i] = plume.Clone();
                new ColliderGameObject(gos[i], 1);
                gos[i].SetActive(true);
                gos[i].transform.position += new Vector3(off[i], 0, 0);
                fsms[i] = gos[i].LocateMyFSM("Control");
            }


            foreach (var fsm in fsms) fsm.SetState("Plume 1");
            yield return new WaitForSeconds(0.1f);
            foreach (var fsm in fsms) fsm.SetState("Plume 2");
            yield return new WaitForSeconds(0.1f);
            foreach (var fsm in fsms) fsm.SetState("Plume 3");
            yield return new WaitForSeconds(0.1f);
            foreach (var fsm in fsms) fsm.SetState("Plume 4");
            yield return new WaitForSeconds(0.1f);
            foreach (var fsm in fsms) fsm.SetState("Scale Up");
            yield return new WaitForSeconds(0.5f);
            foreach (var fsm in fsms) fsm.SetState("End");
            yield return new WaitForSeconds(0.1f);

            animator.Play("DStab Land");
            yield break;
        }

        bool isCounter = false;
        IEnumerator ActionCounter()
        {
            isCounter = true;
            if (TranAttach.IsActionInvoking("RUN"))
            {
                TranAttach.InvokeAction("ATTACK");
            }
            On.HeroController.TakeDamage -= _NoDamage;
            On.HeroController.TakeDamage += _NoDamage;
            yield return animator.PlayAnimWait("Counter Antic");
            animator.Play("Counter Ready");
            while (InputHandler.Instance.inputActions.attack.IsPressed && isCounter) yield return null;
            if (isCounter)
            {
                TranAttach.InvokeAction("ATTACK");
            }
            On.HeroController.TakeDamage -= _NoDamage;
        }

        IEnumerator ActionAttack()
        {
            slashCollider.AttackAntic();
            //yield return animator.PlayAnimWait("Counter Block");
            yield return animator.PlayAnimWait("Slash2");
            yield return animator.PlayAnimWait("Slash2 Recover");
            slashCollider.CancelAttack();
        }

        IEnumerator ActionJump()
        {
            yield return animator.PlayAnimWait("Antic");
            animator.Play("Jump");
            rig.SetVY(30);
            yield return new WaitForSeconds(0.25f);
        }

        private IEnumerator ActionFall()
        {
            animator.Play("Jump");
            yield return new WaitForSeconds(0.25f);
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
