## Intro

A follower plugin for ExileApi (PoeHUD).

OwnedCore link: https://www.ownedcore.com/forums/mmo/path-of-exile/poe-bots-programs/923930-poehud-plugin-follower.html

I want to introduce something I have been working on lately and using since Heist league start.
The purpose of this plugin is to enable your other characters to follow your leader. Useful for purposes such as:
1. Leveling many characters at the same time. Personally I leveled a summoner alone and then using Player Scaling leveled 7 characters to 70+ (5 chars + summoner and separately 2 chars + summoner).
2. Mapping with aurabots, curse bots, and other chars.
3. Raising difficulty and reward of instances/maps without manually clicking on portals or entrances.

### Is this safe?

We never know. One person has reported that his aurabot has been banned. The aurabot was used only with this plugin.

GGG can see many things. E.g. that you run other characters in VMs, that you have the same IP, that other characters are only online when your main character is online, that they are always together in a party etc.
There is no "right" way to use this and not get a ban. But keep in mind the rule of thumb: the more it looks like a real player the less the chance to get a ban.

## Features

This Follower consist of two parts.
* Leader
* Follower

Leader commands followers. You can propagate actions such as:

* Use portal
* Use entrance
* Click on the targeted by leader item (potion, item, WP)
* Level up gems
* Follow leader, stop following leader

Follower listens to the HTTP server and depending on action propagated does what Leader commanded.

## File protocol configuration

This is the use case for leader running natively and followers in VM, e.g. Vmware Workstation 16 on the same computer.

On leader side:
1. Set Profile as "leader".
1. Set "Follower modes" as "file".
1. Open "Advanced leader mode settings"
1. Set the full path where the file will be written. E.g. "D:\Games\test_shared\test.txt"
1. Enable "Start File Writing" checkbox
1. Confirm that you have the file created and it contains the JSON output.

On follower side:
1. Share the file's folder to your VMs. E.g. "D:\Games\test_shared" should be shared in every VM.
	* I strongly recommend enabling "Map as a network drive in Windows guests"
1. Set Profile as "follower".
1. Set "Follower modes" as "file".
1. Set "File path" to the **FULL** path to the file.
	* Remember that inside the VM the path will be different than on the leader.
	* Probably the mounted folder on leader side "D:\Games\test_shared" will be mounted to something like "Z:\test_shared"
	* The full path to set is e.g. "Z:\test_shared\test.txt"
1. Double check that inside the VM the full file's path is correct. 
	* You can access the file from Windows Explorer without any issues.
	* It's pointing to the shared folder. 
	* It's not just "Z:\test_shared"
	* It's not the leader's file path
	* **It's the full "Z:\test_shared\test.txt"**
	* Have I stressed enough that leader's path and the path inside VMs will be different?
1. Enable "Start requesting".

That should be it.


## Server / Client protocol configuration

When using Leader profile and Network mode Follower plugin will start a server (HttpListener) on port 4412. By default the prefix will be localhost.

If you need to have the server accessible from local network please keep in mind about Windows' urlacl rules. Depending on your hostname you will need to use `netsh`. I use "+" as hostname and I've run `netsh http add urlacl url=http://+:4412/ user=YOUR_USER`. I can access the server from local network.

If you're behind a NAT I guess [ngrok](https://ngrok.com/) or [localtunnel](https://github.com/localtunnel/localtunnel) are your best bets. With Ngrok you'll stumble upon request limits very fast so personally I use Localtunnel.
With Localtunnel you can use `localhost` as hostname but you will need to specify `--local-host localhost` while running Localtunnel. `--subdomain` is good to specify as well. 

The whole command I was using `lt --local-host localhost --port 4412 --subdomain testi` and then you can test `curl https://testi.loca.lt`

P.S. Please notice that **for 99% use cases you DO NOT (DOOOOO NOOOOOOOOOOTTTTTTT) need Ngrok or Localtunnel**. If something does not work don't try to use Ngrok or Localtunnel thinking that it will help. Ngrok or Localtunnel will make everything more complicated so stay away from them if you don't clearly understand why you would need them.

Configuration on leader side
1. Activate the plugin
1. Set "leader" profile 
1. Open "Leader Mode Settings" tree
1. Select "network" mode
1. Click "Start Server Listening"
1. IF you want to change the hostname from "localhost" then open "Advanced leader mode settings" and change to something like "+". Restart PoeHUD (restarting the server is not very reliable right now...)

Configuring on follower side
1. Set the profile as "Follower"
1. Click "Follower mode settings"
1. Set mode as "network"
1. Write server URL. If using Localtunnel then it would be something like "https://testi.loca.lt". If you will use your leader's machine's IP then it will be something like "http://192.168.100.23:4412"
1. Set request delay. If running locally I use 500 ms, with Localtunnel 1000 ms might be a good idea
1. Click "Start network requesting" or the hotkey (F3 by default)

## Other configuration

Configuring on Leader side:

1. Click "Set myself as leader" button or write your player name yourself
1. Click "Propagate working of followers" or click the hotkey (F4 by default)

For controlling entering entrances, portal, or clicking on items on leader side you need to:

1. Open "Follower command settings"
1. Enter slave's name or if the follower is nearby select it from select dropdown
1. Click "Set selected value"
1. Click "Add new slave"
1. New follower will be added to the additional ImGui "FollowerV2" window (hold Ctrl to display)

This will add the slave's name to the additional ImGui box. Now you can control that follower.

Using on Follower side:
1. Assign "Move" to "T" hotkey!
1. If you want to use "/hideout NAME" typing functionality you MUST have English keyboard layout


"Propagate working of followers" controls whether followers are working or not.

Additional ImGui window controls (hold Ctrl to display):
1. "Locked" or "Unlocked" allows dragging the window"
1. "Restricting resizing" or "Allowing resize" allows resizing of the window
1. "User X: _NAME_": 
	* E -> Enter entrance
	* P -> Enter portal
	* QIPick -> Pick quest item
	* H -> All followers will write "/hideout NAME" into the chatbox. NAME is the follower's name
	* Del -> Delete this user from the list
	* Hovering over object and clicking "Ctrl+X" (e.g. Ctrl+1) will command the follower to click on that object (click on WP, pickup items etc.)
	* Aggr -> Follower is aggressive and will attack if attack skills defined. Follower will NOT attack at all if this is off. 
		* Useful in maps to quickly set between "follow only" and "follow and attack"
		* E.g. follower will cast non-stop Summon Raging Spirits in a Blight map. For relocation you set this off. On a new place you set this back on.
1. All:
	* Entrance -> Command all to enter entrance
	* Portal -> Command all to enter portal
	* PickQuestItem -> Command all to pick quest items
	* Leader's H -> All followers will write "/hideout NAME" where NAME is leader's name

## Troubleshoot

If you have any issues please describe it here or create a new issue in Github. Additionally do NOT expect it to work as perfect as real player.
Also remember to post either here in "Issues" tab or in the forum thread: https://www.ownedcore.com/forums/mmo/path-of-exile/poe-bots-programs/923930-poehud-plugin-follower.html
Do NOT send me private messages for troubleshoot questions as this reduces visibility and other people cannot search and refer later in case they have the same issues.

1. Server does not work
	* Do you have "Start Server Listening" enabled?
	* Do you have the proper hostname? Ex. "+" or "localhost".
	* Have you run that "netsh" command?
	* What error it give in Debug Log?

1. Followers are not following
	* Does the server running? Have you tested with curl?
	* Can you access the server from your follower's machine? Have you tested with curl? Do you get JSON response?
	* Do you have "Propagate working of followers" enabled?

1. I have a gaming laptop but I barely can run more than 1 virtual machine
	* POE is CPU hungry unless you use something to remove effects and particles. Ex. [poeNullParticles](https://github.com/ajaxvs/poeNullParticles)
	* By default virtual machines (e.g. Vmware Workstation 16) did use Intel's GPU instead of the discrete one. To change that you can go "NVIDIA Control Panel" -> 3D Settings -> Manage 3D Settings -> Global Settings -> Change "Preferred graphics processor" to "High-performance NVIDIA processor". Refer to [this picture](./docs/nvidia_control_panel.png).
	* This is not relevant for desktop PCs.
	* Remember to monitor `Performance` tab in `Task Manager`. Is `GPU 1 NVIDIA: ...` in use or the whole load is on `GPU 0 Intel(R) ...`.

1. I have issues with server/client communication
	* Before you even start asking question prepare the following information:
		* What have you done? Have you followed all instructions?
		* Have you configured server and client properly as described in this documentation?
		* What is your hostname in the plugin's settings?
		* What are your server's and client's IP addresses?
		* Have you tried turning off your firewall? On both sides?
		* If your hostname is not `localhost` in the plugin's settings have you run the required `netsh` command?
		* Can you curl your server inside the machine where you run your server? E.g. does this `curl http://localhost:4412` return the JSON response? If your hostname is not `localhost` do the following commands return the JSON responses `curl http://127.0.0.1:4412` and `curl http://SERVER_MACHINE_IP:4412`?
		* Are you behind a NAT?
		* Can your client machine ping the server machine?
		* Can your client machine curl the server? E.g. running this command from CLIENT's machine should return the JSON response `curl http://SERVER_IP:4412`.
		* Do you use `ngrok` or `localtunnel`?
		* Have you double checked that in client's settings you use `http` and NOT `https`? Notice the ending `S`.
	* If you have answers to all these questions please feel free to ask in the forum thread or create a new Issue here in Github.

## Source Code

Link: [GitHub - 13413j1j13j5315n13/FollowerV2](https://github.com/13413j1j13j5315n13/FollowerV2)

Queuete's [ExileApi](https://github.com/Queuete/ExileApi) was able to download and compile everything without any issues, tested manually.

## Recommended plugins to use

[BasicFlaskRoutine](https://github.com/Queuete/BasicFlaskRoutine) for Health and Quicksilver Flasks

## FAQ

1. Can you implement commands, such as entering an entrance or portal, locally?
	* Only if you will tell me how to do it so that it will work at least across the local network and will not be suspicious by GGG. E.g. chat commands are bad idea because GGG can parse them and mark your accounts as suspicious.

1. How can I run Path of Exile as a limited user in a virtual machine from the shared folder?
	* Create a limited user + restrict access to the folder with PoeHUD. Hold Shift and click with right mouse on game's `PathOfExile_x64.exe`. The option `Run as different user` will appear.

1. Changing hideout two and more times in a row makes PoeHud stuck.
	* The issue is known. Just visit any location or town and Follower will start following again.

1. I just configured something somewhere on my leader and a client in VM, I guess I run localtunnel, and when I run some "cmd" and "curl" command I get some response. Please help!
	* To be able to help we first need to know what you've done and how you've configured everything. The more detail you can give the better. Copy questions from Troubleshoot ---> point 4, and try to answer each of them. When you have the answers you can either create a new issue or post in the OwnedCore forum thread.

1. How can I add a new skill for my follower?
	* First, add a new slave under `Follower command settings`
	* A new tree will be available, e.g. `Follower "Test" settings`
	* Add the required skills. You can refer to the following picture
![](docs/Add_skills_pic.png?raw=true)

## Used offsets and functions

1. GameController.LeftPanel
1. GameController.LeftPanel.StartDrawPoint
1. GameController.InGame
1. GameController.EntityListWrapper.Player
1. GameController.EntityListWrapper.Entities
1. GameController.Entities
1. GameController.IsLoading
1. GameController.Window.GetWindowRectangle().TopLeft
1. GameController.Window.IsForeground()
1. GameController.IngameState.Data.CurrentAreaHash
1. GameController.IngameState.IngameUi.GemLvlUpPanel.GemsToLvlUp.Children
1. GameController.IngameState.CurFps
1. GameController.Game.IngameState.Camera
1. GameController.Game.IngameState.ServerData.IsInGame
1. GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>().CurHP

1. Entity fields:
	1. entity.Id
	1. entity.Type
	1. entity.IsTargetable
	1. entity.IsAlive
	1. entity.IsHostile
	1. entity.IsValid
	1. entity.Distance()
	1. entity.GetComponent<Targetable>()
	1. entity.GetComponent<Targetable>().isTargeted
	1. entity.GetComponent<Player>().PlayerName
	1. entity.GetComponent<WorldItem>().ItemEntity

1. GameController.Player
	1. Player
	1. Player.Pos
	1. Player.GetComponent<Actor>().Action
	1. Player.GetComponent<Actor>().Action
