Thanks for downloading my Advanced Leaderboard! (Revamped by ghxsty)

Revamp by ghxsty

This asset was originally created by NotHacking on discord. So all credit goes to him!

My version of this system, has report reasons, ban reporting and overall an improved system with almost 0 bugs!

How to set up:

On the button red scripts you need to put a sound like a click, or keyboard type etc, for no in game errors.

The Button Red script was NOT created by me. The mute press was improved in the Leaderboard script but not changed in the mute press, same for the kickpress as well. Those two scripts are created by NotHacking, they were revamped in the Leaderboard script though.

Simply drag the prefab into your scene, change the look of the leaderboard however you want (if u want) and have fun!

Everything is self explanatory and already set up.

Ban Webhook - This is the webhook that gets used when a moderator (/staff member) reports someone and they get banned. It will send to the webhook and say "PlayerName was reported by PlayerName (staff) for {reportReason} banned for 48 hours} if you would like you can open the leaderboard script scroll down to the "BanPlayer" void and change the duration and change the webhook to say it as well.

Webhook URL - This is for normal says the same thing, except no banning for normal players and it says the reporters playfab ID.

Make sure to copy and paste this for Client Banning, paste this under one of your
}; in you're PlayFab Cloud Script:

-----

handlers.banPlayer = function(args){
    var duration = args.duration;
    var reason = args.reason;
    server.BanUsers({Bans:[{DurationInHours:duration,PlayFabId:currentPlayerId,Reason:reason}]});
}

-----
