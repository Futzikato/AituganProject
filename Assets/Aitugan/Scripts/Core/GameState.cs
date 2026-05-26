using UnityEngine;

namespace Aitugan.Core
{
    /// <summary>
    /// Persistent run state. A single instance is created at boot and lives
    /// through every vignette transition (DontDestroyOnLoad).
    /// </summary>
    public class GameState : MonoBehaviour
    {
        public static GameState I { get; private set; }

        public int currentVignette = 0; // 0 = title, 1..5 = vignettes, 6 = credits
        public bool hasBow = false;
        public bool hasKinzhal = false;
        public bool hasTumar = false;
        public bool tumarUsed = false;
        public bool hasWindArrows = false;
        public bool readMessengerLetter = false;
        public bool killedSleeper = false; // V4: branches V4-05
        public bool shoulderWound = false; // V5 stamina penalty
        public int saumalFlasks = 2;
        public int arrows = 35;
        public int fireArrows = 0;   // not separately tracked in inventory; lit on the fly at firepot
        public int windArrows = 6;   // gained mid-V4
        public int throwingStones = 3; // V3
        public int hp = 3;
        public int hpMax = 3;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ResetForNewRun()
        {
            currentVignette = 0;
            hasBow = hasKinzhal = hasTumar = false;
            tumarUsed = false;
            hasWindArrows = false;
            readMessengerLetter = false;
            killedSleeper = false;
            shoulderWound = false;
            saumalFlasks = 2;
            arrows = 35;
            windArrows = 6;
            throwingStones = 3;
            hp = hpMax = 3;
        }
    }
}
