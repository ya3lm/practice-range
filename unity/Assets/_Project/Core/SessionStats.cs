using System;
using System.Collections.Generic;
using UnityEngine;

namespace PracticeRange.Core
{
    /// <summary>Round statistics. Pure data; GameLoop owns the instance.</summary>
    [Serializable]
    public class SessionStats
    {
        public int shots;
        public int hits;
        public int kills;
        public int headshots;
        public float roundSeconds;
        public readonly List<float> reactionTimes = new List<float>();
        public readonly List<float> accuracyTimeline = new List<float>(); // sampled once per second
        public int policyCheckpoint; // which AI level/checkpoint was active

        public float Accuracy => shots == 0 ? 0f : (float)hits / shots;
        public float KillsPerMinute => roundSeconds <= 0 ? 0 : kills / (roundSeconds / 60f);
        public float MeanReactionMs
        {
            get
            {
                if (reactionTimes.Count == 0) return 0f;
                float s = 0f; foreach (var r in reactionTimes) s += r;
                return 1000f * s / reactionTimes.Count;
            }
        }

        public void Reset()
        {
            shots = hits = kills = headshots = 0;
            roundSeconds = 0f;
            reactionTimes.Clear();
            accuracyTimeline.Clear();
        }

        public string ToJson() => JsonUtility.ToJson(new Snapshot(this));

        [Serializable]
        struct Snapshot
        {
            public int shots, hits, kills, headshots;
            public float accuracy, kpm, meanReactionMs, roundSeconds;
            public float[] accuracyTimeline;
            public Snapshot(SessionStats s)
            {
                shots = s.shots; hits = s.hits; kills = s.kills; headshots = s.headshots;
                accuracy = s.Accuracy; kpm = s.KillsPerMinute; meanReactionMs = s.MeanReactionMs;
                roundSeconds = s.roundSeconds; accuracyTimeline = s.accuracyTimeline.ToArray();
            }
        }
    }
}
