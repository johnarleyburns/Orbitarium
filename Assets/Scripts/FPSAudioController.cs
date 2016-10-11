using UnityEngine;
using System.Collections;

public class FPSAudioController : MonoBehaviour {

    public GameObject MainEngineAudio;
    public GameObject RCSAudio;
    public GameObject RCSCMGAudio;

    public enum AudioClipEnum
    {
        SPACESHIP_MAIN_ENGINE,
        SPACESHIP_RCS,
        SPACESHIP_RCSCMG
    };

    public bool IsPlaying(AudioClipEnum clip)
    {
        bool playing = false;
        switch (clip)
        {
            case AudioClipEnum.SPACESHIP_MAIN_ENGINE:
                playing = MainEngineAudio.GetComponent<AudioSource>().isPlaying;
                break;
            case AudioClipEnum.SPACESHIP_RCS:
                playing = RCSAudio.GetComponent<AudioSource>().isPlaying;
                break;
            case AudioClipEnum.SPACESHIP_RCSCMG:
                playing = RCSCMGAudio.GetComponent<AudioSource>().isPlaying;
                break;
        }
        return playing;
    }

    public void Play(AudioClipEnum clip)
    {
        switch (clip)
        {
            case AudioClipEnum.SPACESHIP_MAIN_ENGINE:
                MainEngineAudio.GetComponent<AudioSource>().Play();
                break;
            case AudioClipEnum.SPACESHIP_RCS:
                RCSAudio.GetComponent<AudioSource>().Play();
                break;
            case AudioClipEnum.SPACESHIP_RCSCMG:
                RCSCMGAudio.GetComponent<AudioSource>().Play();
                break;
        }
    }

    public void Stop(AudioClipEnum clip)
    {
        switch (clip)
        {
            case AudioClipEnum.SPACESHIP_MAIN_ENGINE:
                MainEngineAudio.GetComponent<AudioSource>().Stop();
                break;
            case AudioClipEnum.SPACESHIP_RCS:
                RCSAudio.GetComponent<AudioSource>().Stop();
                break;
            case AudioClipEnum.SPACESHIP_RCSCMG:
                RCSCMGAudio.GetComponent<AudioSource>().Stop();
                break;
        }
    }
}
