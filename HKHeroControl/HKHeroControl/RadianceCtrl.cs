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
    public class RadianceCtrl : MonoBehaviour
    {
        public TranAttach TranAttach => gameObject.GetTranAttach();
        tk2dSpriteAnimator animator = null;
        Rigidbody2D rig = null;
        DefaultActions defaultActions = null;

        GameObject radiantNailComb = null;  //光剑组
        GameObject radiantNail = null;      //单个光剑

        GameObject radiantOrb = null;       //光球

        GameObject eyeBeamGlow = null;                  //八向激光 
        GameObject[] eyeBeamBursts = new GameObject[3]; //三组

        const int nailAttack = 63;
        void Awake()
        {
            TranAttach.AutoDis = false;
            animator = gameObject.GetComponent<tk2dSpriteAnimator>();
            rig = gameObject.GetComponent<Rigidbody2D>();
            defaultActions = new DefaultActions(animator, rig);

            eyeBeamGlow = gameObject.FindGameObjectInChildren("Eye Beam Glow");
            for (int i = 0; i < 3; i++)
            {
                eyeBeamBursts[i] = eyeBeamGlow.FindGameObjectInChildren($"Burst {i + 1}");
                eyeBeamBursts[i].SetActive(false);
            }

            radiantNailComb = gameObject.GetFSMActionsOnState<SpawnObjectFromGlobalPool>("TL")[0].gameObject.Value.Clone();
            DontDestroyOnLoad(radiantNailComb);
            radiantNailComb.SetActive(false);
            
            radiantNail = gameObject.GetFSMActionsOnState<SpawnObjectFromGlobalPool>("CW Spawn")[0].gameObject.Value.Clone();
            DontDestroyOnLoad(radiantNail);
            radiantNail.SetActive(false);
            radiantNail.TranHeroAttack(AttackTypes.Nail, nailAttack);
            
            radiantOrb = gameObject.GetFSMActionsOnState<SpawnObjectFromGlobalPool>("Spawn Fireball")[0].gameObject.Value.Clone();
            DontDestroyOnLoad(radiantOrb);
            radiantOrb.SetActive(false);
            radiantOrb.FindGameObjectInChildren("Hero Hurter").TranHeroAttack(AttackTypes.Spell, nailAttack);

            foreach (var v in GetComponents<PlayMakerFSM>()) Destroy(v);
            Destroy(GetComponent<EnemyDeathEffects>());

            TranAttach.RegisterAction("M1", ActionMoveHeroTo);
            TranAttach.InvokeActionOn("M1", defaultActions.MoveHeroToTest);
            TranAttach.RegisterAction("M2", ActionMoveTranToHero);
            TranAttach.InvokeActionOn("M2", defaultActions.MoveTranToHeroTest);
            TranAttach.RegisterAction("TURN", ActionTurn);
            TranAttach.InvokeActionOn("TURN", DefaultActions.TurnTest);

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

            //普通攻击->单个光剑（八向控制）
            TranAttach.RegisterAction("ATTACK", ActionSingleNail,
                TranAttach.InvokeWithout("DASH"),
                TranAttach.InvokeWithout("ATTACK"),
                TranAttach.InvokeWithout("FIREBALL"),
                TranAttach.InvokeWithout("JUMP"),
                TranAttach.InvokeWithout("DREAMNAIL")
                );
            TranAttach.InvokeActionOn("ATTACK", DefaultActions.AttackTest);

            //冲刺攻击->八向激光*3
            TranAttach.RegisterAction("DASH", ActionEyeBeamBurst,
                TranAttach.InvokeWithout("DASH")
                );
            TranAttach.InvokeActionOn("DASH", DefaultActions.DashTest);

            //法球攻击->光剑组
            TranAttach.RegisterAction("FIREBALL", ActionNailComb,
                TranAttach.InvokeWithout("FIREBALL")
                );
            TranAttach.InvokeActionOn("FIREBALL", DefaultActions.CastDownTest);

            //跳跃->瞬移（八向）
            TranAttach.RegisterAction("JUMP", ActionTele,
                TranAttach.InvokeWithout("JUMP")
                );
            TranAttach.InvokeActionOn("JUMP", DefaultActions.JumpTest);

            //梦钉->法球
            TranAttach.RegisterAction("DREAMNAIL", ActionOrbAttack,
                TranAttach.InvokeWithout("DREAMNAIL")
                );
            TranAttach.InvokeActionOn("DREAMNAIL", DefaultActions.DreamNailTest);

            //水晶冲刺->回血
            TranAttach.RegisterAction("SUPERDASH", ActionHeal,
                TranAttach.InvokeWithout("SUPERDASH")
                );
            TranAttach.InvokeActionOn("SUPERDASH", DefaultActions.SuperDashTest);

            //向下看->扇形剑雨
            TranAttach.RegisterAction("LOOKDOWN", ActionFanAttack,
                TranAttach.InvokeWithout("LOOKDOWN")
                );
            TranAttach.InvokeActionOn("LOOKDOWN", DefaultActions.RSDownTest);
        }

        private IEnumerator ActionHeal()
        {
            animator.Play("Cast");
            HeroController.instance.AddHealth(1);
            yield return new WaitForSeconds(0.5f);
        }





        private IEnumerator ActionTele()
        {
            GetDXY(out int dx, out int dy);
            if (dx == 0 && dy == 0)
                yield break;
            float x = (float)(dx / Math.Sqrt(dx * dx + dy * dy)) * HeroController.instance.RUN_SPEED / 2;
            float y = (float)(dy / Math.Sqrt(dx * dx + dy * dy)) * HeroController.instance.RUN_SPEED / 2;
            yield return animator.PlayAnimWait("Tele Out");
            transform.position += new Vector3((float)x, (float)y, 0);
            yield return new WaitForSeconds(0.11f);
            yield return animator.PlayAnimWait("Tele In");
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

            //if (Input.GetKeyDown(KeyCode.Alpha0))
            //{
            //    BossSequenceDoor.Completion completion = new BossSequenceDoor.Completion()
            //    {
            //        canUnlock = true,
            //        unlocked = true,
            //        completed = true,
            //        allBindings = false,
            //        noHits = false,
            //        boundNail = false,
            //        boundShell = false,
            //        boundCharms = false,
            //        boundSoul = false,
            //        viewedBossSceneCompletions = new List<string>()
            //    };
            //    Log("set complete");
            //    PlayerData.instance.bossDoorStateTier1 = completion;
            //    PlayerData.instance.bossDoorStateTier2 = completion;
            //    PlayerData.instance.bossDoorStateTier3 = completion;
            //    PlayerData.instance.bossDoorStateTier4 = completion;
            //}
            //if (Input.GetKey(KeyCode.Alpha1))
            //{
            //    Log($"Actions cnt {TranAttach.InvokeCount}: {string.Join(" ", TranAttach.curActs)}");
            //    Log($"is falling: {defaultActions.FallTest()}");
            //}
        }

        IEnumerator ActionSingleNail()
        {
            GetDXY(out int dx, out int dy);
            float angle = (float)(Math.Atan2(dy, dx) / Math.PI * 180) - 90;

            GameObject attackObj = radiantNail.Clone();
            attackObj.transform.position = transform.position;
            attackObj.SetActive(true);
            attackObj.transform.Rotate(0, 0, angle);

            animator.Play("Cast");
            FSMUtility.SendEventToGameObject(attackObj, "FAN ANTIC");
            yield return new WaitForSeconds(0.3f);
            FSMUtility.SendEventToGameObject(attackObj, "FAN ATTACK CW");
            yield return null;
            FSMUtility.SendEventToGameObject(attackObj, "DOWN");
        }

        private static void GetDXY(out int dx, out int dy)
        {
            dx = DefaultActions.RightTest() ? 1 : (DefaultActions.LeftTest() ? -1 : 0);
            dy = DefaultActions.UpTest() ? 1 : (DefaultActions.DownTest() ? -1 : 0);
        }

        IEnumerator ActionNailComb()
        {
            CombDir dir;
            if (DefaultActions.LeftTest())
            {
                if (DefaultActions.DownTest())
                    dir = CombDir.COMBRT;
                else
                    dir = CombDir.COMBR;
            }
            else if (DefaultActions.RightTest())
            {
                if (DefaultActions.DownTest())
                    dir = CombDir.COMBLT;
                else
                    dir = CombDir.COMBL;
            }
            else
                dir = CombDir.COMBT;

            yield return animator.PlayAnimWait("Antic");
            yield return NailCombAttack(dir);
            animator.Play("Cast");
            yield return new WaitForSeconds(0.75f);
            //yield return animator.PlayAnimWait("Recover");
            //yield return new WaitForSeconds(0.35f);
        }

        IEnumerator ActionEyeBeamBurst()
        {
            yield return animator.PlayAnimWait("Antic");

            animator.Play("Cast");
            eyeBeamGlow.SetActive(true);
            yield return ActivateEyeBeamBurst(0, Range(0, 359));
            yield return ActivateEyeBeamBurst(1, Range(0, 359));
            yield return ActivateEyeBeamBurst(2, Range(0, 359));
            eyeBeamGlow.SetActive(false);

            yield return animator.PlayAnimWait("Recover");
            yield return new WaitForSeconds(0.35f);

        }

        enum CombDir { COMBLT = 1, COMBRT, COMBT, COMBL, COMBR, };
        IEnumerator NailCombAttack(CombDir dir)
        {
            int combType = (int)dir;
            GameObject attackGO = radiantNailComb.Clone();
            attackGO.SetActive(false);
            int dx, dy;
            switch (dir)
            {
                case CombDir.COMBLT:
                    dx = -10; dy = 10; break;
                case CombDir.COMBRT:
                    dx = 10; dy = 10; break;
                case CombDir.COMBT:
                    dx = 0; dy = 10; break;
                case CombDir.COMBR:
                    dx = 15; dy = 0; break;
                case CombDir.COMBL:
                    dx = -15; dy = 0; break;
                default:
                    dx = 0; dy = 0; break;
            }

            attackGO.SetActive(true);
            attackGO.LocateMyFSM("Control").FsmVariables.FindFsmInt("Type").Value = combType;

            yield return new WaitForSeconds(0.01f); //等待实际nail的生成
            attackGO.transform.position = transform.position + new Vector3(dx, dy, 0);

            GameObject nails = attackGO.FindGameObjectInChildren("Nails");
            foreach (var tran in nails.GetComponentsInChildren<Transform>())
            {
                tran.gameObject.TranHeroAttack(AttackTypes.Nail, nailAttack);
            }
        }

        IEnumerator ActivateEyeBeamBurst(int i, float rotation)
        {
            GameObject burst = eyeBeamBursts[i];
            burst.transform.Rotate(0, 0, rotation);
            burst.SetActive(true);

            List<GameObject> beams = new List<GameObject>();
            List<string> postfixs = new List<string> { "" };
            for (int j = 1; j <= 7; ++j) postfixs.Add($" ({j})");
            foreach (string postfix in postfixs)
            {
                GameObject go = eyeBeamBursts[i].FindGameObjectInChildren("Radiant Beam" + postfix);
                beams.Add(go);
                go.TranHeroAttack(AttackTypes.Nail, nailAttack);
            }

            foreach (var beam in beams) FSMUtility.SendEventToGameObject(beam, "ANTIC");
            yield return new WaitForSeconds(0.425f);
            foreach (var beam in beams) FSMUtility.SendEventToGameObject(beam, "FIRE");
            yield return new WaitForSeconds(0.3f);
            foreach (var beam in beams) FSMUtility.SendEventToGameObject(beam, "END");
            yield return new WaitForSeconds(0.1f);
        }

        IEnumerator ActionOrbAttack()
        {
            GameObject[] orbs = new GameObject[8];
            float baseA = Range(0, 45);
            for(int i = 0; i < 8; ++i)
            {
                orbs[i] = radiantOrb.Clone();
                orbs[i].SetActive(true);
                float angle = (float)(baseA + 45 * i * Math.PI / 180);
                float x = 8 * (float)Math.Cos(angle), y = 8 * (float)Math.Sin(angle);
                orbs[i].transform.position = transform.position + new Vector3(x, y, 0);
                FSMUtility.SendEventToGameObject(orbs[i], "FIRE");
            }
            yield return new WaitForSeconds(0.55f);
        }

        IEnumerator ActionFanAttack()
        {
            GameObject[] attackObjs = new GameObject[12];
            animator.Play("Cast");
            float baseA = Range(0, 30);
            for (int i = 0; i < 12; ++i)
            {
                GameObject attackObj = radiantNail.Clone();
                attackObj.SetActive(true);
                float angle = (float)(baseA + 30 * i);
                attackObj.transform.Rotate(0, 0, angle - 90);
                attackObj.transform.position = transform.position + new Vector3(0, 1, 0);
                    new Vector3((float)(2 * Math.Cos(angle * Math.PI / 180)), (float)(2 * Math.Sin(angle * Math.PI / 180)), 0);
                attackObjs[i] = attackObj;
                FSMUtility.SendEventToGameObject(attackObj, "FAN ANTIC");
                yield return new WaitForSeconds(0.075f);
            }
            yield return new WaitForSeconds(0.25f);
            for (int i = 0; i < 12; ++i)
            {
                FSMUtility.SendEventToGameObject(attackObjs[i], "FAN ATTACK CW");
            }
            yield return null;
            for (int i = 0; i < 12; ++i)
            {
                FSMUtility.SendEventToGameObject(attackObjs[i], "DOWN");
            }
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
            rig.gravityScale = 0;
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
