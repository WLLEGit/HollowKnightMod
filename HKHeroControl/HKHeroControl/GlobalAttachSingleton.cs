using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using Modding;
using static Modding.Logger;

namespace HKHeroControl
{
    public class GlobalAttachSingleton : MonoBehaviour
    {
        public static GlobalAttachSingleton Instance = null;
        public static GameObject AudioPlayerActor = null;
        public static GameObject RoarEmitter = null;

        void Awake()
        {
            Instance = this;
            GameObject[] objs = (GameObject[])Resources.FindObjectsOfTypeAll(typeof(GameObject));
            foreach(var obj in objs)
            {
                if(obj.name == "Roar Wave Emitter")
                    RoarEmitter = obj;
                else if(obj.name == "Audio Player Actor")
                    AudioPlayerActor = obj;
            }
        }

        public static void PlayOneShot(AudioClip clip, GameObject spawnPoint)
        {
            AudioSource audioSrc = AudioPlayerActor.GetComponent<AudioSource>();
            audioSrc.clip = clip;
            audioSrc.volume = 1;
            audioSrc.loop = false;
            audioSrc.Spawn(spawnPoint.transform.position);
        }

        void OnDisable()
        { 
            Instance = null;
            AudioPlayerActor = null;
            RoarEmitter = null;
        }
    }
}
