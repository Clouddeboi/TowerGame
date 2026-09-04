namespace Game.Player.Stance
{
    //explicit player stance, PlayerStanceController is the only thing that sets this
    //Attacking/Blocking/Staggered/Dead are set by combat later via SetStance, this
    //controller does not need to know why, only what height/capability rules apply
    public enum PlayerStance
    {
        Standing,
        Crouching,
        Sprinting,
        Attacking,
        Blocking,
        Staggered,
        Dead
    }
}