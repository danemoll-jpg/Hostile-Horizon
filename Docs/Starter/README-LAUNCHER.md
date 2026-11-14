# Hostile Horizon — complete download

This ZIP holds the launcher, the game, and the Steam library artwork (portrait, wide, hero, logo, icon). The launcher keeps the game up to date: each time you open it, it checks for a newer version and installs it before the game starts, so you only install once.

The game in this ZIP may be older than the newest release: the first start downloads and installs the newest version (tens of MB), so be online for it.

## Windows

1. Extract the ZIP anywhere you can write to (for example `Documents\Games`). You get a `HostileHorizon` folder.
2. Run `HostileHorizon\HostileHorizonLauncher.exe`.
3. Leave it alone: it checks for updates, downloads and installs a newer version if there is one (the window shows the progress), then starts the game. Without an internet connection, or if the check fails, the installed game starts.

B / Esc skips the update and plays the installed game at once. Any other button or key opens the launcher's menu instead: **Check for updates**, **Update game and play**, **Update soundtrack**, **Restore previous version**. D-pad / arrows choose, A / Enter confirms, B / Esc cancels.

## Steam Deck

1. Switch to **Desktop Mode**.
2. Download the ZIP and extract it anywhere in your home folder (for example `/home/deck/Games`), or on the SD card. You get a `HostileHorizon` folder.
3. In Steam: **Games → Add a Non-Steam Game to My Library → Browse**, pick `HostileHorizon/HostileHorizonLauncher.exe`, then **Add Selected Programs**.
4. Right-click the new entry → **Properties → Compatibility** → tick **Force the use of a specific Steam Play compatibility tool** → **Proton Experimental**.
5. Optional: the library artwork (see **Steam library artwork** below).
6. Back in Gaming Mode, play it from your library. The launcher updates the game by itself, then starts it. The game opens full screen; on the Deck its **Steam Deck** setting (Settings) is on by itself (a shorter view distance and lighter shadows, for the frame rate).

### A HOTAS on the Steam Deck (Thrustmaster T.Flight HOTAS 4)

By default Steam hands the game a virtual controller instead of the HOTAS (the game then sees "a controller, not a HOTAS" and only part of it works). To give the game the HOTAS itself, in Steam (Desktop or Gaming Mode), on the Hostile Horizon launcher's entry:

1. **Properties → Controller**: set it to **Disable Steam Input**.
2. **Properties → General → Launch options**: `PROTON_ENABLE_HIDRAW=0x044F/0xB67C %command%` (Proton then passes the HOTAS through raw, exactly as Windows sees it).
3. Set the switch on the HOTAS base to **PC** (not PS4), and plug the HOTAS in (dock or USB-C adapter) **before** you start the game.
4. Start the game, then **Settings → Controls → HOTAS setup**: the line under the title says how the HOTAS arrives ("a HID joystick, raw (Proton hidraw)" is the goal; "made by Proton's SDL bus" also works). Use the **Test view** there: move everything and watch it.

If it still isn't right: on the same page choose **Record my input (20 s)**, move and press everything, and send the file. The page shows where it was saved; it is inside the shortcut's Proton prefix, something like `/home/deck/.local/share/Steam/steamapps/compatdata/<number>/pfx/drive_c/users/steamuser/AppData/LocalLow/Hostile Horizon/Hostile Horizon/hotas_input_log.txt` (in Desktop Mode, open it with the Dolphin file manager; the `<number>` folder is the shortcut's own).

### Steam library artwork

The artwork files are in the top `HostileHorizon` folder, beside the launcher. An install made before version 0.9.0 gets them with the 0.9.0 update: open the game once through the launcher and they appear there (the launcher's own cover changes to the portrait on its next start). Set them in **Desktop Mode**, in Steam's Library:

| File | What Steam calls it | Where to set it |
|---|---|---|
| `HostileHorizon-Portrait.png` (600 x 900) | library capsule / grid | right-click the game → **Manage → Set custom artwork** |
| `HostileHorizon-Wide.png` (920 x 430) | wide capsule (the "Recent games" shelf) | right-click the game's tile in the **Recent games** shelf at the top of the Library home → **Manage → Set custom artwork** |
| `HostileHorizon-Hero.png` (1920 x 620) | hero / background (the big banner on the game's page) | open the game's page, right-click the empty banner at the top → **Set custom background** |
| `HostileHorizon-Logo.png` (transparent) | logo (drawn over the hero) | on the same page, right-click the banner → **Set custom logo**; then right-click → **Adjust logo position** if you want it moved |
| `HostileHorizon-Icon.png` | shortcut icon | right-click the game → **Properties**, click the icon square |

Back in Gaming Mode the library shows them.

Keep using this same Steam shortcut from now on. Your saves and settings live in that shortcut's Proton prefix; a new shortcut starts with a new, empty prefix.

## Your files

- `versions`: the installed game, plus the previous one so you can go back. Saves are not stored here, and an update or a restore never removes them. A backup copy of the saves is made before each update, in `save-backups`.
- `shared`: the soundtrack, once the game has one.
- `recovery.log`: what the launcher did, if something goes wrong.
- `Launcher-Licenses`: the licences of the launcher's third-party code (JSON for Modern C++, miniz; both MIT).

Close the game before you install an update.
