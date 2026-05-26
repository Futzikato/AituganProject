using UnityEngine;

namespace Aitugan.Core
{
    /// <summary>
    /// Audio is intentionally minimal in the placeholder build: a single
    /// procedurally-generated dombra-like pluck for text-blip and a low drone
    /// per vignette. No authored audio assets are shipped with this build.
    /// </summary>
    public class AudioBus : MonoBehaviour
    {
        public static AudioBus I { get; private set; }
        AudioSource _sfx;
        AudioSource _music;

        AudioClip _blipT, _blipF, _blipO;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.spatialBlend = 0f;
            _sfx.volume = 0.5f;

            _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.spatialBlend = 0f;
            _music.volume = 0.25f;

            _blipT = MakePluck(220f, 0.07f);
            _blipF = MakePluck(160f, 0.09f);
            _blipO = MakePluck(110f, 0.12f);
        }

        public void Blip(string type)
        {
            AudioClip c = type switch { "F" => _blipF, "O" => _blipO, _ => _blipT };
            _sfx.PlayOneShot(c);
        }

        public void StopMusic() => _music.Stop();

        public void PlayDrone(float baseHz, float seconds = 0f)
        {
            _music.clip = MakeDrone(baseHz, 4f);
            _music.Play();
        }

        static AudioClip MakePluck(float hz, float dur)
        {
            int sr = 44100;
            int n = Mathf.CeilToInt(sr * dur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float env = Mathf.Exp(-t * 22f);
                float vibrato = Mathf.Sin(2f * Mathf.PI * hz * t) +
                                0.4f * Mathf.Sin(2f * Mathf.PI * hz * 2f * t) +
                                0.2f * Mathf.Sin(2f * Mathf.PI * hz * 3f * t);
                s[i] = env * vibrato * 0.4f;
            }
            var clip = AudioClip.Create("pluck", n, 1, sr, false);
            clip.SetData(s, 0);
            return clip;
        }

        static AudioClip MakeDrone(float hz, float dur)
        {
            int sr = 44100;
            int n = Mathf.CeilToInt(sr * dur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float w = Mathf.Sin(2f * Mathf.PI * hz * t) * 0.3f
                        + Mathf.Sin(2f * Mathf.PI * hz * 1.5f * t) * 0.2f
                        + Mathf.Sin(2f * Mathf.PI * hz * 0.5f * t) * 0.15f;
                float env = Mathf.Min(1f, t * 3f) * Mathf.Min(1f, (dur - t) * 3f);
                s[i] = w * env;
            }
            var clip = AudioClip.Create("drone", n, 1, sr, false);
            clip.SetData(s, 0);
            return clip;
        }
    }
}
