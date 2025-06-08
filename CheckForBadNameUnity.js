const BadNames = [
    "LEMMING",
    "OWNER"
// replace with what u want
];

handlers.GetBadNames = function(args) {
    return { badNames: BadNames };
}; // returns all the ban names

handlers.CheckForBadName = function(args) {
    const name = args.photonName || args.playFabName;
    const currentPlayerId = args.currentPlayerId;

    if (BadNames.includes(name)) {
        server.BanUsers({
            Bans: [{
                PlayFabId: currentPlayerId,
                DurationInHours: 24,
                Reason: `USE YOUR HEAD NEXT TIME, BAD NAME: ${name}\nID: ${currentPlayerId}`
            }]
        });
        return { result: 2 };
    } else {
        return { result: 0 };
    }
};
