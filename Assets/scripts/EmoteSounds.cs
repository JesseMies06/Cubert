using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmoteSounds : MonoBehaviour
{
    public AudioSource audio;

    public AudioClip axeSpinSound;
    public AudioClip cornerSpinSound;
    public AudioClip spinSound;
    public AudioClip smallJumpSound;
    public AudioClip bigJumpSound;
    public AudioClip shakeSound;

    void PlayAxeSound() => audio.PlayOneShot(axeSpinSound);
    void PlaySpinSound() => audio.PlayOneShot(spinSound);
    void PlayCornerSound() => audio.PlayOneShot(cornerSpinSound);
    void PlaySmallJumpSound() => audio.PlayOneShot(smallJumpSound);
    void PlayBigJumpSound() => audio.PlayOneShot(bigJumpSound);
    void PlayShakeSound() => audio.PlayOneShot(shakeSound);
        
}
