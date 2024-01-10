# DiscJockey - an all-in-one custom Boombox solution

## About

### Why settle for random when you can choose what you want to play?

DiscJockey is a batteries-included media player and downloader that gives you full control over the Boombox.
DiscJockey has a built-in audio engine of sorts that allows you to stream music over the network in real time - it doesn't matter who has what track!

DiscJockey has been designed as a drop-in replacement for [Custom Boombox Music](https://thunderstore.io/c/lethal-company/p/Steven/Custom_Boombox_Music/)

## Features
- Audio is streamed in realtime. It doesn't matter who has what songs, everyone will hear the same thing.
- A custom made vanilla-like media player UI for boomboxes, opened with a configurable hotkey. Supports play/stop, sequential/shuffle/repeat playlist algorithms, volume control and more!
- Custom track loading from disk - also searches for and loads other plugins custom tracks!
- Powered by yt-dlp and allows downloading audio in-game from any one
  of [these sites!](https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md)
- **Fully Networked** - every interaction is synced over the network
- A host of options to configure in the config - interface colour, disabled battery drain and more!
- Other miscellaneous QoL features, like being able to use the Boombox while in orbit

## Planned
- Further UI improvements, like a search bar.

## Adding Custom Music

Simply drag any **MP3** or **WAV** file into the `Custom Songs` folder within the `DiscJockey` plugin folder. If your audio has a sample rate lower than 48khz, it'll be resampled at load - the effect on load time is minimal.

Other plugins that contain a `Custom Songs` folder will also be loaded by DiscJockey.

## Expected folder structure
Note: the `Custom Songs`, `Download Cache` and `Downloaders` folders will be created at first launch.
It's **essential** that the `discjockey` file lives in the `Assets` folder within the `Yoshify-DiscJockey` folder!
```
BepInEx/
  plugins/
    Yoshify-DiscJockey/
      Assets/
        discjockey
      Lib/
        DiscJockey.dll
        <other dll dependencies>
```
## Common problems

### Config has duplicated entries
The config changed quite a lot in the latest version. If you're updating from an old version, you'll need to delete the config and recreate it by launching your game.

## Bugs

DiscJockey has been tested extensively within my friend group, but that doesn't mean it's flawless. If you find any
issues, please file an issue on GitHub [here](https://github.com/Yoshify/DiscJockey)

## Credits
- steven4547466. Thank you for getting the community started with [Custom Boombox Music](https://thunderstore.io/c/lethal-company/p/Steven/Custom_Boombox_Music/)
- [@Bluegrams](https://github.com/Bluegrams) for the fantastic [YoutubeDLSharp](https://github.com/Bluegrams/YoutubeDLSharp) library

## Screenshots

![](https://i.imgur.com/o9zWN1m.png)
