---
layout: page
hash: isolate-spotify-audio-endpoint
order: 74
title: How to isolate Spotify and Spytify to a different audio endpoint device to eliminate noise?
namespace: faq
---

By using the Spytify setting **Audio Device**, you can move Spytify to a different audio endpoint and Spytify will route Spotify as well to this device.

> If you need a better audio endpoint because your sound card has issue with volume control while recording or encodes bad mp3 qualities, you can install a virtual device (like [Virtual Audio Cable that comes with Spytify](#install-better-audio-endpoint-device)) to have recordings closer to the original Spotify quality.

If the Spytify fails to route Spotify to your audio device, you can move it manually like so:

- Press Windows key and type **Sound mixer options**, you should land on the setting page titled _App volume and device preferences_.
- Make sure Spotify is playing and set the app to the desired endpoint using the **Output** select list.
- Restart Spotify. Spotify and Spytify should now be isolated from any undesired sound.

If you can't find the Windows 10 setting, try the section **_Windows 10 Settings 🡂 System 🡂 Sound_**, scroll down to the **_Advanced sound options 🡂 App volume and device preferences_** setting.

<p align="center"><img alt="Spotify and Spytify using a different audio device" src="./assets/images/audio_output_device.gif" /></p>
