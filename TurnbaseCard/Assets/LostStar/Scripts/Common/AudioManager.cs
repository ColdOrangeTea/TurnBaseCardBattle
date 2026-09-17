using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public Slider slider;               //滑动条
    public AudioMixer audioMixer; //声音混合器

    //控制主音量
    public void ControlMainVolume(float v)
    {
        audioMixer.SetFloat("Main", v);
    }
    //控制背景音量
    public void ControlBgmVolume(float v)
    {
        audioMixer.SetFloat("Bgm", v);
    }
    //控制特效音量
    public void ControlTexiaoVolume(float v)
    {
        audioMixer.SetFloat("Texiao", v);
    }
}