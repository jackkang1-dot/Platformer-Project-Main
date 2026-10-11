using System.Collections;
using UnityEngine;

/// <summary>
/// Switches the level music to the boss fight music when the player walks into
/// this trigger zone (fading one out and the other in). Optionally changes the
/// music again when the boss is defeated: plays a victory tune or fades out.
/// Put it on an invisible object with a Box Collider 2D at the entrance to the boss arena.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BossMusicTrigger : MonoBehaviour
{
    [Header("Music")]
    [Tooltip("The level's normal soundtrack: drag in the object that has its Audio Source (for example 'Soundtrack').")]
    [SerializeField] private AudioSource levelMusic;
    [Tooltip("The boss fight soundtrack.")]
    [SerializeField] private AudioClip bossMusic;
    [Range(0f, 1f)]
    [SerializeField] private float bossMusicVolume = 0.6f;
    [Tooltip("Seconds to fade the old music out and the new music in.")]
    [SerializeField] private float fadeTime = 1f;

    [Header("After the boss is defeated (optional)")]
    [Tooltip("Drag the boss here so the music changes when it's defeated. Leave empty to keep the boss music playing.")]
    [SerializeField] private GoombaEnemy boss;
    [Tooltip("Plays once the boss is defeated, like a victory tune. Leave empty to just fade the boss music out.")]
    [SerializeField] private AudioClip victoryMusic;
    [SerializeField] private bool loopVictoryMusic = false;
    [Range(0f, 1f)]
    [SerializeField] private float victoryMusicVolume = 0.6f;

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    private AudioSource bossSource;
    private AudioSource victorySource;
    private bool bossAssigned;
    private bool bossMusicStarted;
    private bool defeatHandled;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        bossAssigned = boss != null;

        bossSource = CreateMusicSource();
        victorySource = CreateMusicSource();
    }

    AudioSource CreateMusicSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D: same volume wherever the camera is
        source.volume = 0f;
        return source;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (bossMusicStarted || bossMusic == null) return;

        GameObject target = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        if (!target.CompareTag(playerTag)) return;

        // Don't start the boss music if the boss is somehow already beaten
        if (BossDefeated()) return;

        bossMusicStarted = true;
        StartCoroutine(Crossfade(levelMusic, bossSource, bossMusic, true, bossMusicVolume));
    }

    void Update()
    {
        if (!bossMusicStarted || defeatHandled || !BossDefeated()) return;

        defeatHandled = true;
        StartCoroutine(Crossfade(bossSource, victorySource, victoryMusic, loopVictoryMusic, victoryMusicVolume));
    }

    bool BossDefeated()
    {
        // The boss object is removed shortly after dying, so a missing boss also counts as defeated
        return bossAssigned && (boss == null || boss.IsDead);
    }

    // Fades "from" out and stops it, while fading "to" in with the new clip (if there is one)
    IEnumerator Crossfade(AudioSource from, AudioSource to, AudioClip clip, bool loop, float targetVolume)
    {
        float fromStartVolume = from != null ? from.volume : 0f;
        bool playNew = to != null && clip != null;

        if (playNew)
        {
            to.clip = clip;
            to.loop = loop;
            to.volume = 0f;
            to.Play();
        }

        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeTime);

            if (from != null) from.volume = Mathf.Lerp(fromStartVolume, 0f, progress);
            if (playNew) to.volume = Mathf.Lerp(0f, targetVolume, progress);

            yield return null;
        }

        if (from != null) from.Stop();
        if (playNew) to.volume = targetVolume;
    }
}
