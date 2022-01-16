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
        public GameObject GrimmGO = null;
        public GameObject HKGO = null;
        public override List<(string, string)> GetPreloadNames()
        {
            var res = new List<(string, string)>();
            foreach (var val in configs.Values)
                res.Add((val.HeroScene, val.HeroAssertPath));
            return res;
        }
        public override void Initialize(Dictionary<string, Dictionary<string, UnityEngine.GameObject>> preloadedObjects)
        {
            InitGameObject<HollowKnightCtrl>(in preloadedObjects, "Hollow Knight", out HKGO);
            //InitGameObject<GrimmCtrl>(in preloadedObjects, "Grimm", out GrimmGO);
            ModHooks.HeroUpdateHook += ModHooks_HeroUpdateHook;
        }

        private void ModHooks_HeroUpdateHook()
        {
            PlayerData.instance.health = 11;
            if(Input.GetKeyUp(KeyCode.F2))
            {
                HKGO.SetActive(!HKGO.activeSelf);
            }
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
            { "Hollow Knight", new ConfigType("GG_Hollow_Knight", "Battle Scene/HK Prime")},
            {"Grimm", new ConfigType("GG_Grimm", "Grimm Scene/Grimm Boss") }
        };
    }
}
