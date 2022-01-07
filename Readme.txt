### Introduction
Seems like rotor guns are popular now and everyone wants to have them.

Players seem to be a bit unhappy with its existence so I created a small plugin that basically allows you to find potential rotor guns via command.

And it prevents potential rotor guns to add new rotor heads faster then a set interval. 

**If there are no such problems on your server you dont need this plugin**

### Commands
- !findrotorgun
 - Scans all grids for potential rotor guns.

### How it works

If a Physical Grid group has at least 4 (configurable) rotors on different subgrids its a potential gun and therefore limits its ability to spam new rotor heads. 

Rotorgun Detector logger has its own Log-File. called rotorguns-&lt;Year&gt;-&lt;Month&gt;-&lt;Day&gt;.log and it wont output on the console or torch.log. Both console should not be spammed with unimportant stuff as it makes finding problems harder. At the same time it would be easier for you to look one logfile up instead of scrolling through an infinitely long torch.log

Basically the Logfile contains entries about on which grids the Plugin prevented rotorhead placement due to it being a potential rotorgun. So you can look them up pretty easily. 

### Github
[https://github.com/LordTylus/SE-Torch-Plugin-ALE-Rotorgun-Detection](https://github.com/LordTylus/SE-Torch-Plugin-ALE-Rotorgun-Detection)