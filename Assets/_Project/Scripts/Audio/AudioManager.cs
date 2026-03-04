using UnityEngine;

namespace DontLetThemIn.Audio
{
    public sealed class AudioManager : MonoBehaviour
    {
        public void PlaySfx(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            AudioSource.PlayClipAtPoint(clip, Vector3.zero, 0.7f);
        }
    }
}
