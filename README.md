## Intro

A follower plugin for ExileApi (PoeHUD).

OwnedCore link: https://www.ownedcore.com/forums/mmo/path-of-exile/poe-bots-programs/923930-poehud-plugin-follower.html

I want to introduce something I have been working on lately and using since Heist league start.
The purpose of this plugin is to enable your other characters to follow your leader. Useful for purposes such as:
1. Leveling many characters at the same time. Personally I leveled a summoner alone and then using Player Scaling leveled 7 characters to 70+ (5 chars + summoner and separately 2 chars + summoner).
2. Mapping with aurabots and curse bots.
3. Raising difficulty and reward of instances/maps without manually clicking on portals or entrances.

## Features

This Follower consist of two parts.
* Leader
* Follower

Leader commands followers. You can propagate actions such as:

* Use portal
* Use entrance
* Pick targeted item
* Follow leader, stop following leader

Follower listens to the HTTP server and depending on action propagated does what Leader commanded.

## Server / Client

When using Leader profile and Network mode Follower plugin will start a server (HttpListener) on port 4412. By default the prefix will be localhost.

If you need to have the server accessible from local network please keep in mind about Windows' urlacl rules. Depending on your hostname you will need to use `netsh`. I use "+" as hostname and I've run `netsh http add urlacl url=http://+:4412/ user=YOUR_USER`. I can access the server from local network.

If you're behind a NAT I guess [ngrok](https://ngrok.com/) or [localtunnel](https://github.com/localtunnel/localtunnel) are your best bets. With Ngrok you'll stumble upon request limits very fast so personally I use Localtunnel.
With Localtunnel you can use `localhost` as hostname but you will need to specify `--local-host localhost` while running Localtunnel. `--subdomain` is good to specify as well. 

The whole command I was using `lt --local-host localhost --port 4412 --subdomain testi` and then you can test `curl https://testi.loca.lt`

## Usage

Using on Leader side:
1. Activate the plugin
1. Set "leader" profile 
1. Open "Leader Mode Settings" tree
1. Select "network" mode
1. Click "Set myself as leader" button or write your player name yourself
1. Click "Start Server Listening"
1. Click "Propagate working of followers" or click the hotkey (F4 by default)
1. IF you want to change the hostname from "localhost" then open "Advanced leader mode settings" and change to something like "+". Restart PoeHUD (restarting the server is not very reliable right now...)


For controlling entering entrances, portal, or clicking on items on leader side you need to:

1. Open "Follower command settings"
1. Enter slave's name or if the follower is nearby select it from select dropdown
1. Click "Set selected value"
1. Click "Add new slave"

This will add the slave's name to the additional ImGui box. Now you can control that follower.

Using on Follower side:
1. Set the profile as "Follower"
1. Click "Follower mode settings"
1. Set mode as "network"
1. Write server URL. If using Localtunnel then it would be something like "https://testi.loca.lt". If you will use your leader's machine's IP then it will be something like "http://192.168.100.23:4412"
1. Set request delay. If running locally I use 500 ms, with Localtunnel 1000 ms might be a good idea
1. Click "Start network requesting" or the hotkey (F3 by default)


"Propagate working of followers" controls whether followers are working or not.

Additional ImGui window controls:
1. "Locked" or "Unlocked" allows dragging the window"
1. "Restricting resizing" or "Allowing resize" allows resizing of the window
1. "User X: _NAME_": 
	* E -> Enter entrance
	* P -> Enter portal
	* QIPick -> Pick quest item
	* Del -> Delete this user from the list
	* Hovering over object and clicking "Ctrl+X" (e.g. Ctrl+1) will command the follower to click on that object (click on WP, pickup items etc.)
1. All:
	* Entrance -> Command all to enter entrance
	* Portal -> Command all to enter portal
	* PickQuestItem -> Command all to pick quest items

## Troubleshoot

If you have any issues please describe it here or create a new issue in Github. Additionally do NOT expect it to work as perfect as real player.

1. Server does not work
	* Do you have "Start Server Listening" enabled?
	* Do you have the proper hostname? Ex. "+" or "localhost".
	* Have you run that "netsh" command?
	* What error it give in Debug Log?

1. Followers are not following
	* Does the server running? Have you tested with curl?
	* Can you access the server from your follower's machine? Have you tested with curl? Do you get JSON response?
	* Do you have "Propagate working of followers" enabled?

## Source Code

Link: [GitHub - 13413j1j13j5315n13/FollowerV2](https://github.com/13413j1j13j5315n13/FollowerV2)

Queuete's [ExileApi](https://github.com/Queuete/ExileApi) was able to download and compile everything without any issues, tested manually.