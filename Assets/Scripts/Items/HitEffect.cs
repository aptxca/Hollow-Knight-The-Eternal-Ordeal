using UnityEngine;

public class HitEffect : MonoBehaviour
{
    private Animator _animator;
    private AudioSource _audioPlayer;

    public void Start()
    {
        _animator = GetComponent<Animator>();
        _audioPlayer = GetComponent<AudioSource>();
    }

    public void Play()
    {
        _animator.Play("Hit",0,0);
        _audioPlayer.Play();
    }
}
