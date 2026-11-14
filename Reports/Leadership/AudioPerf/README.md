# Audio pool — Editor measurement

Unity 6000.6.9f1, batchmode/nographics. Eight preconfigured SFX sources plus one looping music source; no per-play Instantiate or new AudioSource. PCM clips preload/decompress on load. Music and SFX route to separate exposed mixer gains; zero also mutes sources.

`audio-pool.json`: after 100 warmup plays, 1,000 actual stop/clip/pitch/play operations took 3.036 ms total with 0 GC.Alloc samples. Positive control produced one allocation. The test bypasses cue throttling and checks that all 1,000 calls played, so this is not a measurement of rejected requests.

Default cue throttling (40 ms) and voice priorities prevent an entire crowd from stacking identical impact sounds or replacing a roar with a low-priority effect. Pitch variation uses a local sequence and does not consume gameplay/loot randomness. Domain event subscriptions are tested over disable/enable cycles.

This is a scoped CPU/managed-allocation measurement, not frame performance or a listening test. Windows mix capture, loudness/true peak and listening quality are pending.
