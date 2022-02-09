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

        ColliderGameObject bouncer = null;

        GameObject roarPoint = null;
        AudioClip roarClip = null;

        GameObject grimmSpikeHolder = null;

        GameObject grimmUpBall = null;

        void Awake()
        {
            TranAttach.AutoDis = false;
            animator = gameObject.GetComponent<tk2dSpriteAnimator>();
            rig = gameObject.GetComponent<Rigidbody2D>();
            defaultActions = new DefaultActions(animator, rig);

            bouncer = new ColliderGameObject(gameObject.FindGameObjectInChildren("Bouncer"));

            roarPoint = gameObject.FindGameObjectInChildren("Roar Point");
            roarClip = (AudioClip)gameObject.GetFSMActionOnState<AudioPlayerOneShotSingle>("Roar").audioClip.Value;

            grimmSpikeHolder = HeroControl.PreloadGameObjects["GG_Grimm"]["Grimm Spike Holder"].Clone();
            grimmSpikeHolder.SetActive(false);
            DontDestroyOnLoad(grimmSpikeHolder);
            var spikes = grimmSpikeHolder.TravelFindRecursively(go => go.GetComponent<DamageHero>() != null);
            foreach(var (_, go) in spikes)
                go.TranHeroAttack(AttackTypes.Nail, 21);

            grimmUpBall = gameObject.GetFSMActionsOnState<SpawnObjectFromGlobalPool>("UP Explode")[0].gameObject.Value;
            grimmUpBall.SetActive(false);
            grimmUpBall.TranHeroAttack(AttackTypes.Spell, 21);

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
                TranAttach.InvokeWithout("DASH"),
                TranAttach.InvokeWithout("ATTACK"),
                TranAttach.InvokeWithout("ROAR")
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
            TranAttach.RegisterAction("ATTACK", ActionSlash,
                TranAttach.InvokeWithout("JUMP"),
                TranAttach.InvokeWithout("FALL"),
                TranAttach.InvokeWithout("DASH"),
                TranAttach.InvokeWithout("ATTACK"));
            TranAttach.InvokeActionOn("ATTACK", DefaultActions.AttackTest);

            //冲刺攻击
            TranAttach.RegisterAction("DASH", ActionGDash,
                TranAttach.InvokeWithout("DASH")
                );
            TranAttach.InvokeActionOn("DASH", DefaultActions.DashTest);

            //上吼攻击
            TranAttach.RegisterAction("ROAR", ActionUppercut,
                TranAttach.InvokeWithout("FIREBALL"),
                TranAttach.InvokeWithout("ROAR"),
                TranAttach.InvokeWithout("QUAKE"),
                TranAttach.InvokeWithout("DASH")
                //TranAttach.InvokeWithout("JUMP"),
                //TranAttach.InvokeWithout("FALL"),
                //TranAttach.InvokeWithout("RUN")
                );
            TranAttach.InvokeActionOn("ROAR", TranAttach.And(
                DefaultActions.CastDownTest,
                DefaultActions.UpTest,
                TranAttach.Not(
                    TranAttach.Or(
                        DefaultActions.DownTest
                        )
                    )
                ));

            //下砸攻击
            TranAttach.RegisterAction("QUAKE", ActionSpikeAttack,
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

            //向下看
            TranAttach.RegisterAction("LOOKDOWN", ActionBow,
                TranAttach.InvokeWithout("LOOKDOWN")
                );
            TranAttach.InvokeActionOn("LOOKDOWN", DefaultActions.RSDownTest);

            //水晶冲刺
            TranAttach.RegisterAction("SUPERDASH", ActionRoar,
                TranAttach.InvokeWithout("SUPERDASH")
                );
            TranAttach.InvokeActionOn("SUPERDASH", DefaultActions.SuperDashTest);
        }

        bool faceRight = false;
        private IEnumerator ActionTurn()
        {
            faceRight = DefaultActions.RightTest();
            if (faceRight)
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
            //animator.Play("Idle");
            //yield return new WaitForSeconds(0.25f);
            yield return null;
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
            if (TranAttach.IsActionInvoking("FALL") || TranAttach.IsActionInvoking("JUMP"))
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
                rig.SetVX((
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

            animator.Play("Slash Antic");
            yield return new WaitForSeconds(0.5f);
            rig.SetVX(HeroController.instance.DASH_SPEED * (faceRight ? 1 : -1));
            bouncer.AttackAntic();
            yield return animator.PlayAnimWait("Slash 1");
            yield return animator.PlayAnimWait("Slash 2");
            yield return animator.PlayAnimWait("Slash 3");
            bouncer.CancelAttack();
            rig.SetVX(0);
            HeroController.instance.SetDamageMode(0);
            rig.gravityScale = 1;
            yield return animator.PlayAnimWait("Slash Recover");
        }
        
        IEnumerator ActionGDash()
        {
            rig.gravityScale = 0;
            rig.velocity = new Vector2(0, 0);
            HeroController.instance.SetDamageMode(1);

            animator.Play("G Dash Antic");
            yield return new WaitForSeconds(0.5f);
            bouncer.AttackAntic();
            rig.SetVX(HeroController.instance.DASH_SPEED * (faceRight ? 1 : -1));
            yield return animator.PlayAnimWait("G Dash");
            rig.SetVX(0);
            yield return new WaitForSeconds(0.25f);
            yield return animator.PlayAnimWait("G Dash Antic");
            yield return new WaitForSeconds(0.35f);

            bouncer.CancelAttack();
            HeroController.instance.SetDamageMode(0);
            rig.gravityScale = 1;
        }
        IEnumerator ActionUppercut()
        {
            bouncer.AttackAntic();
            yield return animator.PlayAnimWait("Uppercut Antic");
            animator.Play("Uppercut");
            HeroController.instance.SetDamageMode(1);
            rig.velocity = new Vector2(HeroController.instance.DASH_SPEED * (faceRight ? 1 : -1), 45);
            yield return new WaitForSeconds(0.25f);

            int[] vOffsets = { 0, -9, 9, -20, 20 };
            GameObject[] attackObjs = new GameObject[vOffsets.Length];
            for (int i = 0; i < vOffsets.Length; i++)
            {
                attackObjs[i] = grimmUpBall.Clone();
                attackObjs[i].SetActive(true);
                attackObjs[i].transform.position = transform.position;
                attackObjs[i].GetComponent<Rigidbody2D>().velocity = new Vector2(vOffsets[i], 1);
            }
            yield return new WaitForSeconds(0.1f);
            HeroController.instance.SetDamageMode(0);
            bouncer.CancelAttack();
            rig.velocity = Vector2.zero;
        }

        IEnumerator ActionBow()
        {
            animator.Play("Bow");
            yield return new WaitForSeconds(1);
            HeroController.instance.AddHealth(1);
            yield return animator.PlayAnimWait("Bow Return");
        }

        IEnumerator ActionRoar()
        {
            GameObject roarEmitter = GlobalAttachSingleton.RoarEmitter.Clone();
            roarEmitter.transform.position = roarPoint.transform.position;
            roarEmitter.SetActive(false);

            yield return animator.PlayAnimWait("Roar Antic");
            roarEmitter.SetActive(true);
            GlobalAttachSingleton.PlayOneShot(roarClip, roarPoint);
            animator.Play("Roar");

            //对靠近Grimm的5个敌人施加AOE
            List<GameObject> objs = ((GameObject[])Resources.FindObjectsOfTypeAll(typeof(GameObject)))
                                    .Where(go => go.layer == (int)GlobalEnums.PhysLayers.ENEMIES
                                                && go.GetComponent<HealthManager>() != null)
                                    .ToList();
            objs.Sort((lhs, rhs) =>
                        (rhs.transform.position - transform.position).magnitude.CompareTo(
                        (lhs.transform.position - transform.position).magnitude));

            objs.Take(5).ToList().ForEach(go =>
                {
                    HitTaker.Hit(go, new HitInstance()
                    {
                        AttackType = AttackTypes.Spell,
                        Source = gameObject,
                        DamageDealt = 21,
                        Multiplier = 1,
                        MagnitudeMultiplier = 1,
                        CircleDirection = true,
                        IgnoreInvulnerable = false 
                    });
                    FSMUtility.SendEventToGameObject(go, "TOOK DAMAGE");
                });

            yield return new WaitForSeconds(1f);
            yield return animator.PlayAnimWait("Roar Antic");
            Destroy(roarEmitter);
        }

        IEnumerator ActionSpikeAttack()
        {
            animator.Play("Capespike Cast");
            yield return new WaitForSeconds(0.5f);

            grimmSpikeHolder.SetActive(true);
            FSMUtility.SendEventToGameObject(grimmSpikeHolder, "SPIKE ATTACK");

            yield return new WaitForSeconds(1f);
        }
        //IEnumerator ActionGenPlume()
        //{
        //    rig.SetVY(50);
        //    animator.Play("Jump");
        //    yield return new WaitForSeconds(0.3f);
        //    rig.SetVY(-25);
        //    animator.Play("Dstab Stomp");

        //    int[] off = new int[] { -10, -5, 0, 5, 10 };
        //    GameObject[] gos = new GameObject[off.Length];
        //    PlayMakerFSM[] fsms = new PlayMakerFSM[off.Length];
        //    for (int i = 0; i < off.Length; i++)
        //    {
        //        gos[i] = plume.Clone();
        //        new ColliderGameObject(gos[i], 21);
        //        gos[i].SetActive(true);
        //        gos[i].transform.position += new Vector3(off[i], 0, 0);
        //        fsms[i] = gos[i].LocateMyFSM("Control");
        //    }


        //    foreach (var fsm in fsms) fsm.SetState("Plume 1");
        //    yield return new WaitForSeconds(0.1f);
        //    foreach (var fsm in fsms) fsm.SetState("Plume 2");
        //    yield return new WaitForSeconds(0.1f);
        //    foreach (var fsm in fsms) fsm.SetState("Plume 3");
        //    yield return new WaitForSeconds(0.1f);
        //    foreach (var fsm in fsms) fsm.SetState("Plume 4");
        //    yield return new WaitForSeconds(0.1f);
        //    foreach (var fsm in fsms) fsm.SetState("Scale Up");
        //    yield return new WaitForSeconds(0.5f);
        //    foreach (var fsm in fsms) fsm.SetState("End");
        //    yield return new WaitForSeconds(0.1f);

        //    animator.Play("DStab Land");
        //    yield break;
        //}

        IEnumerator ActionJump()
        {
            rig.SetVY(30);
            yield return new WaitForSeconds(0.25f);
        }

        private IEnumerator ActionFall()
        {
            yield return null;
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
    }
}
