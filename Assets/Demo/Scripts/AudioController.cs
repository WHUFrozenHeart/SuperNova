using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioController : SingletonController<AudioController>
{
    private AudioSource audioSource;

    public enum AudioEffct
    {
        Drown,
        PickUp,
        Fire,
        Electric,
        FireH2O,
        FireH2,
        ElectricH2O,
        Hunger
    }

    [System.Serializable]
    public class AudioEffectItem
    {
        public AudioEffct audioEffct;
        public AudioClip audioClip;
    }

    public List<AudioEffectItem> audioEffectItems;
    private Dictionary<AudioEffct, AudioClip> audioClips = new Dictionary<AudioEffct, AudioClip>();

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        foreach(AudioEffectItem audioEffectItem in audioEffectItems)
        {
            audioClips.Add(audioEffectItem.audioEffct, audioEffectItem.audioClip);
        }
    }

    public void PlayAudioEffect(AudioEffct audioEffct)
    {
        audioSource.PlayOneShot(audioClips[audioEffct]);
    }
}
