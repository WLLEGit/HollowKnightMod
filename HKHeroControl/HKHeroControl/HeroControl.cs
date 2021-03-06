using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modding;
using UnityEngine;
using ModCommon.Util;

#region Acknowledgement
/// <summary>
/// TranCore        作者：空洞实验室    项目地址：https://github.com/HKLab/TranCore   
/// Hornet mod      作者：空洞实验室    项目地址：https://github.com/HKLab/Hornet
/// </summary>
#endregion

namespace HKHeroControl
{
    public class HeroControl : Mod
    {
        public static Dictionary<string, Dictionary<string, UnityEngine.GameObject>> PreloadGameObjects;
        public GameObject GrimmGO = null;
        public GameObject HKGO = null;
        public GameObject SlyGO = null;
        public GameObject RadianceGO = null;

        GameObject curGO = null;
        GameObject nextGO = null;
        public override List<(string, string)> GetPreloadNames()
        {
            var res = new List<(string, string)>();
            foreach (var val in configs.Values)
                res.Add((val.HeroScene, val.HeroAssertPath));
            return res;
        }
        public override void Initialize(Dictionary<string, Dictionary<string, UnityEngine.GameObject>> preloadedObjects)
        {
            PreloadGameObjects = preloadedObjects;
            InitGameObject<HollowKnightCtrl>(in preloadedObjects, "Hollow Knight", out HKGO);
            InitGameObject<GrimmCtrl>(in preloadedObjects, "Grimm", out GrimmGO);
            InitGameObject<SlyCtrl>(in preloadedObjects, "Sly", out SlyGO);
            InitGameObject<RadianceCtrl>(in preloadedObjects, "Radiance", out RadianceGO);
            InitChoices();

            ModHooks.HeroUpdateHook += ModHooks_HeroUpdateHook;
        }

        bool isDbgLockHealth = false;
        private void ModHooks_HeroUpdateHook()
        {
            foreach (var (key, val) in switchChoices)
            {
                if (Input.GetKeyDown(key))
                {
                    nextGO = val;
                    break;
                }
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                GameObject knight = HeroController.instance.gameObject;
                if (knight.GetComponent<GlobalAttachSingleton>() != null)
                    foreach (var c in knight.GetComponents<GlobalAttachSingleton>())
                        UnityEngine.Object.Destroy(c);
                knight.AddComponent<GlobalAttachSingleton>();

                if (nextGO != null)
                {
                    curGO?.SetActive(false);
                    curGO = nextGO;
                    nextGO = null;
                    curGO.SetActive(true);
                }
                else
                {
                    curGO?.SetActive(false);
                    nextGO = curGO;
                    curGO = null;
                }
            }

            if (Input.GetKeyDown(KeyCode.F11))
            {
                isDbgLockHealth = !isDbgLockHealth;
                Log($"Lock Health: {isDbgLockHealth}");
            }
            if (isDbgLockHealth)
                PlayerData.instance.health = 11;
        }

        private void InitChoices()
        {
            switchChoices = new Dictionary<KeyCode, GameObject>
            {
                { KeyCode.F1, HKGO},
                { KeyCode.F2, GrimmGO },
                { KeyCode.F3, SlyGO },
                { KeyCode.F4, RadianceGO },
            };
        }
        private void InitGameObject<T>(in Dictionary<string, Dictionary<string, GameObject>> preloadedObjects, string name, out GameObject go) where T : Component
        {
            var config = configs[name];
            go = UnityEngine.Object.Instantiate(
                preloadedObjects[config.HeroScene][config.HeroAssertPath]
                );

            go.transform.parent = null;
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<T>();
        }
        private struct ConfigType
        {
            public string HeroScene;
            public string HeroAssertPath;
            public ConfigType(string scene, string assert)
            {
                HeroScene = scene;
                HeroAssertPath = assert;
            }
        }
        private readonly Dictionary<string, ConfigType> configs = new Dictionary<string, ConfigType>
        {
            //{ "HK Prime", new ConfigType("GG_Hollow_Knight", "Battle Scene/HK Prime")},
            {"Grimm", new ConfigType("GG_Grimm", "Grimm Scene/Grimm Boss") },
            {"Grimm Spike Holder", new ConfigType("GG_Grimm","Grimm Spike Holder") },
            {"Hollow Knight", new ConfigType("Room_Final_Boss_Core", "Boss Control/Hollow Knight Boss") },
            {"Sly", new ConfigType("GG_Sly", "Battle Scene/Sly Boss") },
            {"Radiance", new ConfigType("GG_Radiance", "Boss Control/Absolute Radiance") },
            {"Radiance Beam Sweeper", new ConfigType("GG_Radiance","Boss Control/Beam Sweeper") },
        };

        private Dictionary<KeyCode, GameObject> switchChoices;
    }
}
