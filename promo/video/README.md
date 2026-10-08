# Launch video

A 47-second motion video for WinClean, built with [Remotion](https://www.remotion.dev). After the
intro, a Windows desktop appears and the WinClean window opens maximised; a pointer walks through the
pages, the window is restored and minimised, and the desktop widget takes over. Every frame is React;
every sound is synthesised by `tools/make-audio.py`, so the folder carries no third-party media.

```
npm install
npm run audio      # writes public/audio/*.wav (needs Python 3 and numpy)
npm run studio     # preview in the browser
npm run render     # out/winclean-launch.mp4, 1920x1080, 30 fps, H.264 + AAC
```

The scene boundaries live in `src/timeline.ts` and the desktop choreography (window states, pointer
path, clicks, captions, camera) in `src/Stage.tsx`; the sound cues in `src/Soundtrack.tsx` are
expressed through those constants so a beat can be moved without re-timing its audio.
`node tools/stills.mjs 75 230 420` renders single frames to `out/stills/` for a quick look.

On a machine without Chrome, point Remotion at a browser with `--browser-executable`.
