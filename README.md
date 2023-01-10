# capycarbonara
An OSRS/Discord flipping program, a rewrite of [Floppa-Flipper](https://github.com/japsuu/Floppa-Flipper).

---

## What?

Accesses [the osrs price API](https://oldschool.runescape.wiki/w/RuneScape:Real-time_Prices#Routes), and uses a custom [anomaly detection algorithm](https://github.com/japsuu/capycarbonara/blob/fbe6922e23b522d127011307d4f176f37d2372d3/Flipper/FlipperV2.cs#L192) to determine item price spikes and crashes.
Notifies the user on Discord when a price crash/spike happens.

Has the ability to sync it's update cycle to the API's update cycle, allowing the program to fetch the latest values without querying the API all the time.

[Modifyable config](https://github.com/japsuu/capycarbonara/blob/master/Flipper/config.json), although the old version had [way better one.](https://github.com/japsuu/Floppa-Flipper/blob/master/App.config)

---

## Why?

Allows the user to take advantage of a crash in the market value of a certain item.
The user can buy the items which are currently being dumped to the market with abnormally low price, and profit later by selling them when the market value spikes again.

---

## When?

Currently in the process of migrating a lot of stuff from the old version.
For now Discord integration is hardcoded to only work on certain channels, until I get to migrating the old config >:)

---

### The old version is still available [here.](https://github.com/japsuu/Floppa-Flipper)
