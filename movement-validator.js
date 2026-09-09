// Put this logic in a trusted PlayFab CloudScript/Function or equivalent.
// Server owns the authoritative state; client movement is only a proposal.
function validateMovement(request) {
  const player = getPlayerState(request.sessionId); // PUT YOUR PLAYER LOOKUP HERE
  if (!player) return { accepted:false, reason:"unknown_session" };
  const dt=Math.max(0.02,Math.min(request.serverSequenceTime-player.lastTime,0.25));
  const dx=request.position.x-player.position.x, dy=request.position.y-player.position.y, dz=request.position.z-player.position.z;
  const distance=Math.sqrt(dx*dx+dy*dy+dz*dz);
  const maxDistance=2.5+player.allowedMovementBonus; // TUNE FOR YOUR GAME
  if(distance>maxDistance){player.suspicion+=15;savePlayerState(player);return {accepted:false,reason:"movement_outlier"};}
  player.position=request.position;player.lastTime=request.serverSequenceTime;savePlayerState(player);
  return {accepted:true,reason:"ok"};
}
