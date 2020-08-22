using BepInEx;
using UnityEngine;
using System;
using System.Globalization;
using System.IO;
using HarmonyLib;
using R2API.Utils;
using UnityEngine.UI;
using RoR2;
using System.Reflection;
using UnityEngine.Video;
using UnityEngine.Audio;

namespace x753
{
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync)]
    [BepInPlugin("com.x753.MP4Player", "MP4 Player", "1.0.0")]
    public class MP4Player : BaseUnityPlugin
    {
        public static GameObject _mp4Prefab;
        public static float _volumeMaster = 100f;
        public static float _volumeSFX = 100f;
        public void Awake()
        {
            // Load in our assets
            Assembly execAssembly = Assembly.GetExecutingAssembly();
            using (Stream stream = execAssembly.GetManifestResourceStream("MP4Player.mp4player"))
            {
                AssetBundle bundle = AssetBundle.LoadFromStream(stream);
                _mp4Prefab = bundle.LoadAsset<GameObject>("Assets/mp4player.prefab");
            }

            // Intercept people typing URLs in chat so we can put a video player on their head
            On.RoR2.Chat.UserChatMessage.ConstructChatString += (orig, self) =>
            {
                if (self.sender)
                {
                    NetworkUser component = self.sender.GetComponent<NetworkUser>();
                    if (component != null && self.text != null && self.sender != null && component.GetCurrentBody() != null)
                    {
                        if (self.text.StartsWith("http")) // when a user sends a message containing a link, load it into the videoplayer
                        {
                            GameObject mp4Player;
                            if (component.GetCurrentBody().GetComponentInChildren<VideoPlayer>() == null)
                            {
                                mp4Player = UnityEngine.Object.Instantiate<GameObject>(_mp4Prefab, component.GetCurrentBody().transform.position + new Vector3(0f, 0.4f, 0f), Quaternion.identity);
                                mp4Player.transform.parent = component.GetCurrentBody().transform;
                            }
                            else
                            {
                                mp4Player = component.GetCurrentBody().GetComponentInChildren<VideoPlayer>().gameObject;
                            }
                            mp4Player.GetComponent<VideoPlayer>().enabled = true;
                            mp4Player.GetComponent<VideoPlayer>().url = self.text;
                        }
                        else // if they send any other message disable the current videoplayer
                        {
                            if (component.GetCurrentBody().GetComponentInChildren<VideoPlayer>() != null)
                            {
                                component.GetCurrentBody().GetComponentInChildren<VideoPlayer>().enabled = false;
                            }
                        }
                    }
                }

                return orig(self);
            };

            On.RoR2.AudioManager.VolumeConVar.SetString += (orig, self, newValue) =>
            {
                orig(self, newValue);

                if (self.name == "volume_master")
                {
                    _volumeMaster = float.Parse(newValue);
                }
                else if (self.name == "volume_sfx")
                {
                    _volumeSFX = float.Parse(newValue);
                }

                var dbVolume = Mathf.Log10(_volumeSFX*_volumeMaster/100) * 20 - 40f; // convert % volume to decibels -80 dB to 0 dB 
                if (_volumeSFX == 0.0f || _volumeMaster == 0.0f)
                {
                    dbVolume = -80.0f;
                }
                _mp4Prefab.GetComponent<AudioSource>().outputAudioMixerGroup.audioMixer.SetFloat("sfx_volume", dbVolume);
            };
        }
    }
}
