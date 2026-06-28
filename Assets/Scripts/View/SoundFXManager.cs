using UnityEngine;

namespace GreenPrince
{
    public class SoundFXManager : MonoBehaviour
    {
        public static SoundFXManager instance;

        [SerializeField] AudioClip[] m_StepClips;
        [SerializeField] AudioClip m_MapGemClip;
        [SerializeField] AudioClip m_UpgradeGemClip;
        [SerializeField] AudioClip m_LandmarkBeatenClip;

        void Awake()
        {
            if (instance == null)
                instance = this;
        }

        public void PlayStepSound(Transform spawnTransform)
        {
            PlayRandomSoundFXClip(m_StepClips, spawnTransform);
        }

        public void PlayMapGemPickup(Transform spawnTransform)
        {
            PlaySoundFXClip(m_MapGemClip, spawnTransform);
        }

        public void PlayUpgradePurchased(Transform spawnTransform)
        {
            PlaySoundFXClip(m_UpgradeGemClip, spawnTransform);
        }

        public void PlayLandmarkBeaten(Transform spawnTransform)
        {
            PlaySoundFXClip(m_LandmarkBeatenClip, spawnTransform);
        }

        public void PlaySoundFXClip(AudioClip audioClip, Transform spawnTransform, float volume = 1.0f)
        {
            if (audioClip == null || spawnTransform == null)
                return;

            var soundObject = new GameObject("TempAudioSource");
            soundObject.transform.position = spawnTransform.position;
            var audioSource = soundObject.AddComponent<AudioSource>();
            audioSource.clip = audioClip;
            audioSource.volume = volume;
            audioSource.Play();

            Destroy(soundObject, audioClip.length);
        }

        public void PlayRandomSoundFXClip(AudioClip[] audioClips, Transform spawnTransform, float volume = 1.0f)
        {
            if (audioClips == null || audioClips.Length == 0)
                return;

            var randomClip = audioClips[Random.Range(0, audioClips.Length)];
            PlaySoundFXClip(randomClip, spawnTransform, volume);
        }
    }
}
